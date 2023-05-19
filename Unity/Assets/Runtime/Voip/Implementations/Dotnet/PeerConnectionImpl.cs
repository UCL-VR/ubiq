using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using SIPSorcery.Net;
using Ubiq.Voip.Implementations.JsonHelpers;

namespace Ubiq.Voip.Implementations.Dotnet
{
    public class PeerConnectionImpl : IPeerConnectionImpl {

        public event IceConnectionStateChangedDelegate iceConnectionStateChanged;
        public event PeerConnectionStateChangedDelegate peerConnectionStateChanged;

        private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private Queue<string> messageQueue = new Queue<string>();
        private Task<RTCPeerConnection> setupTask;

        private IPeerConnectionContext context;
        private Coroutine updateCoroutine;

        // SipSorcery Peer Connection
        private RTCPeerConnection rtcPeerConnection;

        private IVoipSink sink;
        private IVoipSource source;

        public async void Dispose()
        {
            if (updateCoroutine != null)
            {
                context.behaviour.StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }

            if (setupTask != null)
            {
                await setupTask.ConfigureAwait(false);
                setupTask.Result.Dispose();
            }
        }

        public void Setup(IPeerConnectionContext context,
            bool polite, List<IceServerDetails> iceServers)
        {
            if (setupTask != null)
            {
                // Already setup or setup in progress
                return;
            }

            this.context = context;

            // Copy ice servers before entering multithreaded context
            var iceServersCopy = new List<IceServerDetails>();
            for (int i = 0; i < iceServers.Count; i++)
            {
                iceServersCopy.Add(new IceServerDetails(
                    uri: iceServers[i].uri,
                    username: iceServers[i].username,
                    password: iceServers[i].password));
            }

            RequireSource();
            RequireSink();

            setupTask = Task.Run(() => DoSetup(polite,iceServersCopy));
            updateCoroutine = context.behaviour.StartCoroutine(Update());
        }

        private void RequireSource ()
        {
            if (source != null)
            {
                return;
            }

            var manager = context.behaviour.transform.parent;

            // First, see if an source already exists among siblings
            source = manager.GetComponentInChildren<IVoipSource>();

            // If not, check if a hint exists and use it
            if (source == null)
            {
                var hint = manager.GetComponent<VoipSourceHint>();
                if (hint && hint.prefab)
                {
                    var go = GameObject.Instantiate(hint.prefab);
                    go.transform.parent = manager;
                    source = go.GetComponentInChildren<IVoipSource>();
                }
            }

            // If still nothing, use default
            if (source == null)
            {
                var go = new GameObject("Microphone Dotnet Voip Source");
                go.transform.parent = manager;
                source = go.AddComponent<MicrophoneVoipSource>();
            }
        }

        private void RequireSink ()
        {
            if (sink != null)
            {
                return;
            }

            var manager = context.behaviour.transform.parent;

            // First, check if a hint exists and use it
            var hint = manager.GetComponent<VoipSinkHint>();
            if (hint && hint.prefab)
            {
                var go = GameObject.Instantiate(hint.prefab);
                go.transform.parent = context.behaviour.transform;
                sink = go.GetComponentInChildren<IVoipSink>();
            }

            // If still nothing, use default
            if (sink == null)
            {
                var go = new GameObject("Dotnet Voip Output");
                go.transform.parent = context.behaviour.transform;
                sink = go.AddComponent<AudioSourceVoipSink>();
            }
        }


        public void ProcessSignallingMessage (string json)
        {
            // Buffer messages until the RtcPeerConnection is initialised
            if (setupTask == null || !setupTask.IsCompleted)
            {
                messageQueue.Enqueue(json);
            }
            else
            {
                DoReceiveSignallingMessage(json);
            }
        }

        private void DoReceiveSignallingMessage(string json)
        {
            var msg = SignallingMessageHelper.FromJson(json);
            if (msg.type != null) // Session description
            {
                var sd = SessionDescriptionUbiqToPkg(msg);

                Debug.Log($"Got remote SDP, type {sd.type}");
                switch(sd.type)
                {
                    case RTCSdpType.answer:
                    {
                        var result = rtcPeerConnection.setRemoteDescription(sd);
                        if (result != SetDescriptionResultEnum.OK)
                        {
                            Debug.Log($"Failed to set remote description, {result}.");
                            rtcPeerConnection.Close("Failed to set remote description");
                        }
                        break;
                    }
                    case RTCSdpType.offer:
                    {
                        var result = rtcPeerConnection.setRemoteDescription(sd);
                        if (result != SetDescriptionResultEnum.OK)
                        {
                            Debug.Log($"Failed to set remote description, {result}.");
                            rtcPeerConnection.Close("Failed to set remote description");
                        }

                        var answerSdp = rtcPeerConnection.createAnswer();
                        rtcPeerConnection.setLocalDescription(answerSdp);
                        Send(answerSdp);
                        break;
                    }
                }
            }
            else // Ice candidate
            {
                // Convert to sipsorcery format
                var ic = IceCandidateUbiqToPkg(msg);
                Debug.Log($"Got remote Ice Candidate {ic.sdpMid} {ic.sdpMLineIndex} {ic.candidate}");
                rtcPeerConnection.addIceCandidate(ic);
            }
        }

        private async Task<RTCPeerConnection> DoSetup(bool polite,
            List<IceServerDetails> iceServers)
        {
            // Convert to sipsorcery ice server format
            var ssIceServers = new List<RTCIceServer>();
            for (int i = 0; i < iceServers.Count; i++)
            {
                if (string.IsNullOrEmpty(iceServers[i].username) ||
                    string.IsNullOrEmpty(iceServers[i].password))
                {
                    ssIceServers.Add(new RTCIceServer {
                        urls = iceServers[i].uri
                    });
                }
                else
                {
                    ssIceServers.Add(new RTCIceServer {
                        urls = iceServers[i].uri,
                        username = iceServers[i].username,
                        credential = iceServers[i].password,
                        credentialType = RTCIceCredentialType.password
                    });
                }
            }

            var pc = new RTCPeerConnection(new RTCConfiguration
            {
                iceServers = ssIceServers,
            });

            pc.addTrack(new MediaStreamTrack(
                source.GetAudioSourceFormats(),
                MediaStreamStatusEnum.SendRecv));
            pc.OnAudioFormatsNegotiated += (formats) =>
            {
                source.SetAudioSourceFormat(formats[0]);
                sink.SetAudioSinkFormat(formats[0]);
            };

            pc.onconnectionstatechange += (state) =>
            {
                mainThreadActions.Enqueue(() =>
                {
                    switch(state)
                    {
                        case RTCPeerConnectionState.closed : peerConnectionStateChanged(PeerConnectionState.closed); break;
                        case RTCPeerConnectionState.failed : peerConnectionStateChanged(PeerConnectionState.failed); break;
                        case RTCPeerConnectionState.disconnected : peerConnectionStateChanged(PeerConnectionState.disconnected); break;
                        case RTCPeerConnectionState.@new : peerConnectionStateChanged(PeerConnectionState.@new); break;
                        case RTCPeerConnectionState.connecting : peerConnectionStateChanged(PeerConnectionState.connecting); break;
                        case RTCPeerConnectionState.connected : peerConnectionStateChanged(PeerConnectionState.connected); break;
                    }
                    Debug.Log($"Peer connection state change to {state}.");
                });

                if (state == RTCPeerConnectionState.connected)
                {
                    if (pc.HasAudio)
                    {
                        source.OnAudioSourceEncodedSample += pc.SendAudio;
                    }
                }
                else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
                {
                    source.OnAudioSourceEncodedSample -= pc.SendAudio;
                }
            };

            pc.onicecandidate += (iceCandidate) =>
            {
                if (iceCandidate != null && !string.IsNullOrEmpty(iceCandidate.candidate))
                {
                    mainThreadActions.Enqueue(() => Send(iceCandidate));
                }
            };

            pc.OnRtpPacketReceived += (IPEndPoint rep, SDPMediaTypesEnum media, RTPPacket rtpPkt) =>
            {
                if (media == SDPMediaTypesEnum.audio)
                {
                    // todo
                    sink.GotAudioRtp(rep, rtpPkt.Header.SyncSource, rtpPkt.Header.SequenceNumber, rtpPkt.Header.Timestamp, rtpPkt.Header.PayloadType, rtpPkt.Header.MarkerBit == 1, rtpPkt.Payload);
                }
            };

            // Diagnostics.
            pc.OnReceiveReport += (re, media, rr) => mainThreadActions.Enqueue(
                () => Debug.Log($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}"));
            pc.OnSendReport += (media, sr) => mainThreadActions.Enqueue(
                () => Debug.Log($"RTCP Send for {media}\n{sr.GetDebugSummary()}"));
            pc.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) => mainThreadActions.Enqueue(
                () => Debug.Log($"STUN {msg.Header.MessageType} received from {ep}."));
            pc.oniceconnectionstatechange += (state) => mainThreadActions.Enqueue(() =>
            {
                switch(state)
                {
                    case RTCIceConnectionState.closed : iceConnectionStateChanged(IceConnectionState.closed); break;
                    case RTCIceConnectionState.failed : iceConnectionStateChanged(IceConnectionState.failed); break;
                    case RTCIceConnectionState.disconnected : iceConnectionStateChanged(IceConnectionState.disconnected); break;
                    case RTCIceConnectionState.@new : iceConnectionStateChanged(IceConnectionState.@new); break;
                    case RTCIceConnectionState.checking : iceConnectionStateChanged(IceConnectionState.checking); break;
                    case RTCIceConnectionState.connected : iceConnectionStateChanged(IceConnectionState.connected); break;
                }
                Debug.Log($"ICE connection state change to {state}.");
            });

            if (!polite)
            {
                var offer = pc.createOffer();
                await pc.setLocalDescription(offer).ConfigureAwait(false);
                mainThreadActions.Enqueue(() => Send(offer));
            }

            return pc;
        }

        private IEnumerator Update()
        {
            while(setupTask == null || !setupTask.IsCompleted)
            {
                yield return null;
            }

            rtcPeerConnection = setupTask.Result;

            // Process buffered messages now that the peer connection is initialised
            while (messageQueue.Count > 0)
            {
                DoReceiveSignallingMessage(messageQueue.Dequeue());
            }

            while(true)
            {
                while (mainThreadActions.TryDequeue(out Action action))
                {
                    action();
                }

                yield return null;
            }
        }

        private static string SdpTypeToString(RTCSdpType type)
        {
            switch(type)
            {
                case RTCSdpType.answer : return "answer";
                case RTCSdpType.offer : return "offer";
                case RTCSdpType.pranswer : return "pranswer";
                case RTCSdpType.rollback : return "rollback";
                default : return null;
            }
        }

        private static RTCSdpType StringToSdpType(string type)
        {
            switch(type)
            {
                case "answer" : return RTCSdpType.answer;
                case "offer" : return RTCSdpType.offer;
                case "pranswer" : return RTCSdpType.pranswer;
                case "rollback" : return RTCSdpType.rollback;
                default : return RTCSdpType.offer;
            }
        }

        private static RTCIceCandidateInit IceCandidateUbiqToPkg(SignallingMessage msg)
        {
            return new RTCIceCandidateInit()
            {
                candidate = msg.candidate,
                sdpMid = msg.sdpMid,
                sdpMLineIndex = msg.sdpMLineIndex ?? 0
            };
        }

        private static RTCSessionDescriptionInit SessionDescriptionUbiqToPkg(SignallingMessage msg)
        {
            return new RTCSessionDescriptionInit{
                sdp = msg.sdp,
                type = StringToSdpType(msg.type)
            };
        }

        private void Send(RTCSessionDescriptionInit sd)
        {
            context.Send(SignallingMessageHelper.ToJson(new SignallingMessage{
                sdp = sd.sdp,
                type = SdpTypeToString(sd.type)
            }));
        }

        private void Send(RTCIceCandidate ic)
        {
            context.Send(SignallingMessageHelper.ToJson(new SignallingMessage{
                candidate = ic.candidate,
                sdpMid = ic.sdpMid,
                sdpMLineIndex = (ushort?)ic.sdpMLineIndex,
                usernameFragment = ic.usernameFragment
            }));
        }

        public void UpdateSpatialization(Vector3 sourcePosition, Quaternion sourceRotation, Vector3 listenerPosition, Quaternion listenerRotation)
        {
            if (sink != null)
            {
                sink.UpdateSpatialization(sourcePosition,sourceRotation,
                    listenerPosition,listenerRotation);
            }
        }

        public PlaybackStats GetLastFramePlaybackStats ()
        {
            if (sink != null)
            {
                return sink.GetLastFramePlaybackStats();
            }
            else
            {
                return new PlaybackStats();
            }
        }
    }
}