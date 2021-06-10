// // using Pixiv.Cricket;
// // using Pixiv.Webrtc;
// using System;
// using System.Collections;
// using System.Collections.Concurrent;
// using System.Collections.Generic;
// using Ubiq.Logging;
// using Ubiq.Messaging;
// using UnityEngine;
// using UnityEngine.Events;


using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using Ubiq.Messaging;

namespace Ubiq.CsWebRtc
{
    [NetworkComponentId(typeof(CsWebRtcPeerConnection), 78)]
    public class CsWebRtcPeerConnection : MonoBehaviour, INetworkComponent, INetworkObject {

        public WebRtcUnityAudioSource audioSource { get; private set; }
        public WebRtcUnityAudioSink audioSink { get; private set; }
        public NetworkId Id { get; private set; }
        public string PeerUuid { get; private set; }

        public RTCIceConnectionState iceConnectionState { get; private set; }
        public RTCPeerConnectionState peerConnectionState { get; private set; }

        [Serializable] public class IceConnectionStateEvent : UnityEvent<RTCIceConnectionState> { }
        [Serializable] public class PeerConnectionStateEvent : UnityEvent<RTCPeerConnectionState> { }

        public IceConnectionStateEvent onIceConnectionStateChanged;
        public PeerConnectionStateEvent onPeerConnectionStateChanged;

        private NetworkContext context;
        private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private Queue<Message> messageQueue = new Queue<Message>();
        private Task setupTask;

        // SipSorcery Peer Connection
        private RTCPeerConnection rtcPeerConnection;

        // Debug
        private const string STUN_URL = "stun:stun.l.google.com:19302";
        private const string TURN_URL = "turn:20.84.122.207";
        private const string TURN_USER = "ubiqtestuser";
        private const string TURN_PASS = "1rZ$aU9C^cdbstHb";

        private void OnDestroy()
        {
            Teardown();
        }

        public void Setup (NetworkId objectId, string peerUuid,
            bool polite, WebRtcUnityAudioSource source, WebRtcUnityAudioSink sink)
        {
            if (setupTask != null)
            {
                // Already setup
                return;
            }

            if (onIceConnectionStateChanged == null)
            {
                onIceConnectionStateChanged = new IceConnectionStateEvent();
            }
            if (onPeerConnectionStateChanged == null)
            {
                onPeerConnectionStateChanged = new PeerConnectionStateEvent();
            }

            this.Id = objectId;
            this.PeerUuid = peerUuid;
            this.audioSource = source;
            this.audioSink = sink;
            this.context = NetworkScene.Register(this);

            this.setupTask = Task.Run(() => DoSetup(polite));
        }

        private async void Teardown ()
        {
            if (setupTask != null)
            {
                await setupTask;
                rtcPeerConnection.Dispose();
            }
        }

        private async Task DoSetup(bool polite)
        {
            var pc = await CreatePeerConnection(audioSource,audioSink,mainThreadActions);
            if (!polite)
            {
                var offer = pc.createOffer();
                await pc.setLocalDescription(offer);
                mainThreadActions.Enqueue(() => Send("Offer",offer.toJSON()));
            }

            rtcPeerConnection = pc;
        }

        private Task<RTCPeerConnection> CreatePeerConnection(
            IAudioSource audioSource, IAudioSink audioSink,
            ConcurrentQueue<Action> mainThreadActions )
        {
            var pc = new RTCPeerConnection(new RTCConfiguration
            {
                iceServers = new List<RTCIceServer>
                {
                    new RTCIceServer { urls = STUN_URL },
                    new RTCIceServer
                    {
                        urls = TURN_URL,
                        username = TURN_USER,
                        credential = TURN_PASS,
                        credentialType = RTCIceCredentialType.password
                    }
                }
            });

            pc.addTrack(new MediaStreamTrack(
                audioSource.GetAudioSourceFormats(),
                MediaStreamStatusEnum.SendRecv));
            pc.OnAudioFormatsNegotiated += (formats) =>
                audioSource.SetAudioSourceFormat(formats[0]);
            // audioSink.SetAudioSinkFormat(formats[0]);

            audioSource.OnAudioSourceEncodedSample += pc.SendAudio;

            pc.onconnectionstatechange += (state) =>
            {
                mainThreadActions.Enqueue(() =>
                {
                    peerConnectionState = state;
                    onPeerConnectionStateChanged.Invoke(state);
                    Debug.Log($"Peer connection state change to {state}.");
                });

                if (state == RTCPeerConnectionState.connected)
                {
                    // audioSource.OnAudioSourceEncodedSample += pc.SendAudio;
                }
                else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
                {
                    // audioSource.OnAudioSourceEncodedSample -= pc.SendAudio;
                }
            };
            pc.onicecandidate += (iceCandidate) =>
            {
                if (pc.signalingState == RTCSignalingState.have_remote_offer ||
                    pc.signalingState == RTCSignalingState.stable)
                {
                    mainThreadActions.Enqueue(() => Send("IceCandidate",iceCandidate.toJSON()));
                }
            };
            pc.OnRtpPacketReceived += (IPEndPoint rep, SDPMediaTypesEnum media, RTPPacket rtpPkt) =>
            {
                //logger.LogDebug($"RTP {media} pkt received, SSRC {rtpPkt.Header.SyncSource}.");
                if (media == SDPMediaTypesEnum.audio)
                {
                    audioSink.GotAudioRtp(rep, rtpPkt.Header.SyncSource, rtpPkt.Header.SequenceNumber, rtpPkt.Header.Timestamp, rtpPkt.Header.PayloadType, rtpPkt.Header.MarkerBit == 1, rtpPkt.Payload);
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
                iceConnectionState = state;
                onIceConnectionStateChanged.Invoke(state);
                Debug.Log($"ICE connection state change to {state}.");
            });

            return Task.FromResult(pc);
        }

        private void Update()
        {
            while (mainThreadActions.TryDequeue(out Action action))
            {
                action();
            }

            // If RtcPeerConnection is initialised, process buffered messages
            if (setupTask.IsCompleted)
            {
                while (messageQueue.Count > 0)
                {
                    DoProcessMessage(messageQueue.Dequeue());
                }
            }
        }

        [Serializable]
        private struct Message
        {
            public string type;
            public string args;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage data)
        {
            var message = data.FromJson<Message>();

            // Buffer messages until the RtcPeerConnection is initialised
            if (setupTask == null || !setupTask.IsCompleted)
            {
                messageQueue.Enqueue(message);
            }
            else
            {
                DoProcessMessage(message);
            }
        }

        private void DoProcessMessage(Message message)
        {
            switch(message.type)
            {
                case "Offer":
                    // var offer = JsonUtility.FromJson<RTCSessionDescriptionInit>(message.args);
                    if (RTCSessionDescriptionInit.TryParse(message.args,out RTCSessionDescriptionInit offer))
                    {
                        Debug.Log($"Got remote SDP, type {offer.type}");

                        var result = rtcPeerConnection.setRemoteDescription(offer);
                        if (result != SetDescriptionResultEnum.OK)
                        {
                            Debug.Log($"Failed to set remote description, {result}.");
                            rtcPeerConnection.Close("Failed to set remote description");
                        }
                        else
                        {
                            if(rtcPeerConnection.signalingState == RTCSignalingState.have_remote_offer)
                            {
                                var answerSdp = rtcPeerConnection.createAnswer();
                                rtcPeerConnection.setLocalDescription(answerSdp);

                                Debug.Log($"Sending SDP answer");
                                //logger.LogDebug(answerSdp.sdp);

                                Send("Offer", answerSdp.toJSON());
                            }
                        }
                    }
                    break;
                case "IceCandidate":
                    // var candidate = JsonUtility.FromJson<RTCIceCandidateInit>(message.args);
                    //
                    if (RTCIceCandidateInit.TryParse(message.args,out RTCIceCandidateInit candidate))
                    {
                        Debug.Log($"Got remote Ice Candidate, uri {candidate.candidate}");
                        rtcPeerConnection.addIceCandidate(candidate);
                    }
                    break;
            }
        }

        private void Send(string type, string args)
        {
            context.SendJson(new Message()
            {
                type = type,
                args = args
            });
        }
    }
}