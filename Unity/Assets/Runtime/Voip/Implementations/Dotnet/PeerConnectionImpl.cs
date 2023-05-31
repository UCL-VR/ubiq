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
    public class PeerConnectionImpl : IPeerConnectionImpl
    {
        private class Event
        {
            // Ideally we'd let the implementation guide us on when to negotiate
            // with the OnNegotiationNeeded event, but SipSorcery seems to never
            // generate these events, so comment out for now and workaround it
            public enum Type
            {
                // NegotiationNeeded,
                SignalingMessage
            }

            public readonly Type type;
            public readonly string json;

            public Event(string json) : this(Type.SignalingMessage,json) { }
            public Event(Type type, string json = null, RTCIceCandidate iceCandidate = null)
            {
                this.type = type;
                this.json = json;
            }
        }

        private enum Implementation
        {
            Unknown,
            Dotnet,
            Other
        }

        public event IceConnectionStateChangedDelegate iceConnectionStateChanged;
        public event PeerConnectionStateChangedDelegate peerConnectionStateChanged;

        private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private ConcurrentQueue<Event> events = new ConcurrentQueue<Event>();
        private Queue<string> messageQueue = new Queue<string>();
        private Task<RTCPeerConnection> setupTask;

        private IPeerConnectionContext context;
        private bool polite;
        private Coroutine updateCoroutine;

        // SipSorcery Peer Connection
        private RTCPeerConnection peerConnection;

        private IVoipSink sink;
        private IVoipSource source;

        private List<Coroutine> coroutines = new List<Coroutine>();

        private Implementation otherPeerImplementation = Implementation.Unknown;
        private List<RTCIceCandidate> bufferedIceCandidates = new List<RTCIceCandidate>();
        private bool hasSentLocalSdp;

        public async void Dispose()
        {
            if (context.behaviour)
            {
                foreach(var coroutine in coroutines)
                {
                    if (context.behaviour)
                    {
                        context.behaviour.StopCoroutine(coroutine);
                    }
                }
            }
            coroutines.Clear();

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
            this.polite = polite;

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

            coroutines.Add(context.behaviour.StartCoroutine(DoUpdate()));
            // coroutines.Add(context.behaviour.StartCoroutine(StartMicrophoneTrack()));
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


        public void ProcessSignalingMessage (string json)
        {
            events.Enqueue(new Event(json));
        }

        private RTCPeerConnection DoSetup(bool polite,
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

            // Ideally we'd let the implementation guide us on when to negotiate
            // with the OnNegotiationNeeded event, but SipSorcery seems to never
            // generate these events, so comment out for now and workaround it
            // pc.onnegotiationneeded += () =>
            // {
            //     events.Enqueue(new Event(Event.Type.NegotiationNeeded));
            // };

            pc.onicecandidate += (iceCandidate) =>
            {
                mainThreadActions.Enqueue(() => Send(iceCandidate));
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
                () => Debug.Log($"STUN {msg.Header.MessageType} received from {ep}:{msg.ToString()}"));
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

            pc.addTrack(new MediaStreamTrack(source.GetAudioSourceFormats()));

            return pc;
        }

        private IEnumerator DoUpdate()
        {
            while(setupTask == null || !setupTask.IsCompleted)
            {
                yield return null;
            }

            peerConnection = setupTask.Result;

            // Send id message as this implementation needs special workarounds
            Send(implementation:"dotnet");

            var time = Time.realtimeSinceStartup;
            while(true)
            {
                while (mainThreadActions.TryDequeue(out Action action))
                {
                    action();
                }

                while (events.TryDequeue(out Event ev))
                {
                    yield return HandleSignalingEvent(ev);
                }

                if (Time.realtimeSinceStartup > time + 5)
                {
                    Debug.Log($"{peerConnection.connectionState} | {peerConnection.signalingState} | {peerConnection.iceConnectionState} | {peerConnection.iceGatheringState} ");
                    time = Time.realtimeSinceStartup;
                }
                yield return null;
            }
        }

        private bool ignoreOffer;

        // Manage all signaling, sending and receiving offers.
        // Attempt to implement 'Perfect Negotiation', but with some changes
        // as we fully finish consuming each signaling message before starting
        // on the next.
        // https://w3c.github.io/webrtc-pc/#example-18
        private IEnumerator HandleSignalingEvent(Event e)
        {
            // e.type == Signaling message
            var msg = SignalingMessageHelper.FromJson(e.json);

            // Id the other implementation as Dotnet requires special treatment
            if (otherPeerImplementation == Implementation.Unknown)
            {
                otherPeerImplementation = msg.implementation == "dotnet"
                    ? Implementation.Dotnet
                    : Implementation.Other;

                if (otherPeerImplementation == Implementation.Dotnet && !polite)
                {
                    yield return SetLocalDescription(peerConnection);
                    Send(peerConnection.localDescription);
                    yield break;
                }
                else
                {
                    // If just one of the two peers is dotnet, the
                    // non-dotnet peer always takes on the role of polite
                    // peer as the dotnet implementaton isn't smart enough
                    // to handle rollback
                    polite = false;
                }
            }

            if (msg.type != null)
            {
                ignoreOffer = !polite
                    && msg.type == "offer"
                    && !(peerConnection.signalingState == RTCSignalingState.stable
                        || peerConnection.signalingState == RTCSignalingState.closed);
                if (ignoreOffer)
                {
                    yield break;
                }

                var desc = SessionDescriptionUbiqToPkg(msg);
                var result = peerConnection.setRemoteDescription(desc);
                if (msg.type == "offer")
                {
                    yield return SetLocalDescription(peerConnection);
                    Send(peerConnection.localDescription);
                }
                yield break;
            }

            if (msg.candidate != null && !string.IsNullOrWhiteSpace(msg.candidate))
            {
                if (!ignoreOffer)
                {
                    peerConnection.addIceCandidate(IceCandidateUbiqToPkg(msg));
                }
                yield break;
            }
        }

        private void Send(string implementation)
        {
            context.Send(SignalingMessageHelper.ToJson(new SignalingMessage{
                implementation = implementation
            }));
        }

        private void Send(RTCIceCandidate ic)
        {
            if (hasSentLocalSdp)
            {
                InternalSend(context,ic);
            }
            else
            {
                bufferedIceCandidates.Add(ic);
            }
        }

        private void Send(RTCSessionDescription sd)
        {
            InternalSend(context,sd);
            hasSentLocalSdp = true;
            while (bufferedIceCandidates.Count > 0)
            {
                var ic = bufferedIceCandidates[0];
                bufferedIceCandidates.RemoveAt(0);
                InternalSend(context,ic);
            }
        }

        private static IEnumerator SetLocalDescription(RTCPeerConnection pc)
        {
            // Should just be able to call setLocalDescription() here
            // and have the implementation generate an offer or an answer
            // as appropriate but SipSorcery doesn't implement the spec
            // completely. Instead need to call createOffer() or
            // createAnswer() depending on SignalingState, and then
            // setLocalDescription().
            // https://w3c.github.io/webrtc-pc/#dom-peerconnection-setlocaldescription
            RTCSessionDescriptionInit sd = null;
            if (pc.signalingState == RTCSignalingState.closed
                || pc.signalingState == RTCSignalingState.stable
                || pc.signalingState == RTCSignalingState.have_local_offer
                || pc.signalingState == RTCSignalingState.have_remote_pranswer)
            {
                sd = pc.createOffer();
            }
            else
            {
                sd = pc.createAnswer();
            }
            var op = Task.Run(() => pc.setLocalDescription(sd));
            yield return new WaitUntil(() => op.IsCompleted);
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

        private static RTCIceCandidateInit IceCandidateUbiqToPkg(SignalingMessage msg)
        {
            return new RTCIceCandidateInit()
            {
                candidate = msg.candidate,
                sdpMid = msg.sdpMid,
                sdpMLineIndex = msg.sdpMLineIndex ?? 0,
                usernameFragment = msg.usernameFragment
            };
        }

        private static RTCSessionDescriptionInit SessionDescriptionUbiqToPkg(SignalingMessage msg)
        {
            return new RTCSessionDescriptionInit{
                sdp = msg.sdp,
                type = StringToSdpType(msg.type)
            };
        }

        private static void InternalSend(IPeerConnectionContext context, RTCSessionDescription sd)
        {
            context.Send(SignalingMessageHelper.ToJson(new SignalingMessage{
                sdp = sd.sdp.RawString(),
                type = SdpTypeToString(sd.type)
            }));
        }

        private static void InternalSend(IPeerConnectionContext context, RTCIceCandidate ic)
        {
            context.Send(SignalingMessageHelper.ToJson(new SignalingMessage{
                candidate = $"candidate:{ic.candidate}",
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