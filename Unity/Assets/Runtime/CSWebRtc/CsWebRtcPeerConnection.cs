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

// // using SIP

using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using SIPSorcery.Net;
using Ubiq.Messaging;

// Debug
using SIPSorcery.Media;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia;
using System.Net;
using System.Threading;

namespace Ubiq.CsWebRtc
{
    [NetworkComponentId(typeof(CsWebRtcPeerConnection), 78)]
    public class CsWebRtcPeerConnection : MonoBehaviour, INetworkComponent, INetworkObject {

        public WebRtcUnityAudioSource audioSource { get; private set; }
        public WebRtcUnityAudioSink audioSink { get; private set; }

        public struct PeerConnectionState
        {
            public string Peer;
            // public string LastMessageReceived;
            // public bool HasRemote;
            // public volatile PeerConnectionInterface.SignalingState SignalingState;
            // public volatile PeerConnectionInterface.IceConnectionState ConnectionState;
            // public volatile PeerConnectionInterface.IceGatheringState IceState;
        }

        public PeerConnectionState State;

        public NetworkId Id { get; set; }

        // public void AddLocalAudioSource(){}

        // SipSorcery Peer Connection
        private RTCPeerConnection rtcPeerConnection;
        private NetworkContext context;
        private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private Queue<Message> messageQueue = new Queue<Message>();

        private Task setupTask;

        // Debug
        private const string STUN_URL = "stun:stun.l.google.com:19302";

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

            this.Id = objectId;
            this.State.Peer = peerUuid;
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
            var config = new RTCConfiguration
            {
                iceServers = new List<RTCIceServer> { new RTCIceServer { urls = STUN_URL } }
            };
            var pc = new RTCPeerConnection(config);

            // debug
            // var audioSource = new AudioExtrasSource(new AudioEncoder(), new AudioSourceOptions { AudioSource = AudioSourcesEnum.Music });
            // var audioEp = new DummyAudioEndPoint();

            audioSource.OnAudioSourceEncodedSample += pc.SendAudio;

            var audioTrack = new MediaStreamTrack(audioSource.GetAudioSourceFormats(), MediaStreamStatusEnum.SendRecv);

            pc.addTrack(audioTrack);

            pc.OnAudioFormatsNegotiated += (formats) => audioSource.SetAudioSourceFormat(formats[0]);

            pc.onconnectionstatechange += async (state) =>
            {
                mainThreadActions.Enqueue(() => Debug.Log($"Peer connection state change to {state}."));

                if (state == RTCPeerConnectionState.connected)
                {
                    await audioSource.StartAudio();
                }
                else if (state == RTCPeerConnectionState.closed || state == RTCPeerConnectionState.failed)
                {
                    await audioSource.CloseAudio();
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
            pc.oniceconnectionstatechange += (state) => mainThreadActions.Enqueue(
                () => Debug.Log($"ICE connection state change to {state}."));

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