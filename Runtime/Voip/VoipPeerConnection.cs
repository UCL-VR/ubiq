using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using SIPSorcery.Net;
using Ubiq.Messaging;
using Ubiq.Logging;
using SIPSorceryMedia.Abstractions;

namespace Ubiq.Voip
{
    public class VoipPeerConnection : MonoBehaviour {

        public IAudioSource audioSource { get; private set; }
        public IAudioSink audioSink { get; private set; }
        public NetworkId networkId { get; private set; }
        public string PeerUuid { get; private set; }

        public bool isSetup => setupTask != null && setupTask.IsCompleted;
        public RTCIceConnectionState iceConnectionState { get; private set; } = RTCIceConnectionState.@new;
        public RTCPeerConnectionState peerConnectionState { get; private set; } = RTCPeerConnectionState.@new;

        [Serializable] public class IceConnectionStateEvent : UnityEvent<RTCIceConnectionState> { }
        [Serializable] public class PeerConnectionStateEvent : UnityEvent<RTCPeerConnectionState> { }

        public IceConnectionStateEvent OnIceConnectionStateChanged = new IceConnectionStateEvent();
        public PeerConnectionStateEvent OnPeerConnectionStateChanged = new PeerConnectionStateEvent();

        private NetworkScene networkScene;
        private ConcurrentQueue<Action> mainThreadActions = new ConcurrentQueue<Action>();
        private Queue<Message> messageQueue = new Queue<Message>();
        private Task<RTCPeerConnection> setupTask;
        private LogEmitter logger;

        // Underlying SipSorcery Peer Connection
        private RTCPeerConnection rtcPeerConnection;

        public struct SessionStatistics
        {
            public uint PacketsSent;
            public uint BytesSent;
            public uint PacketsRecieved;
            public uint BytesReceived;
        }

        /// <summary>
        /// Summarises the throughput for different sessions in this connection.
        /// This is returned when the statistics are polled from this peer connection.
        /// </summary>
        public struct Statistics
        {
            public SessionStatistics Audio;
            public SessionStatistics Video;
        }

        private void OnDestroy()
        {
            if (networkScene)
            {
                networkScene.RemoveProcessor(networkId,ProcessMessage);
            }

            Teardown();
        }

        public void Setup (NetworkId objectId, string peerUuid,
            bool polite, IAudioSource source, IAudioSink sink,
            Task<RTCPeerConnection> peerConnectionTask)
        {
            if (setupTask != null)
            {
                // Already setup or setup in progress
                return;
            }

            this.networkId = objectId;
            this.PeerUuid = peerUuid;
            this.audioSource = source;
            this.audioSink = sink;
            this.networkScene = NetworkScene.Find(this);
            this.logger = new NetworkEventLogger(objectId, networkScene, this);

            networkScene.AddProcessor(networkId,ProcessMessage);

            this.setupTask = Task.Run(() => DoSetup(polite,peerConnectionTask));
        }

        private async void Teardown ()
        {
            if (setupTask != null)
            {
                await setupTask.ConfigureAwait(false);
                setupTask.Result.Dispose();
            }
        }

        private async Task<RTCPeerConnection> DoSetup(bool polite, Task<RTCPeerConnection> pcTask)
        {
            var pc = await pcTask.ConfigureAwait(false);

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
                    OnPeerConnectionStateChanged.Invoke(state);
                    logger.Log("PeerConnectionStateChange",state);
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
                if (media == SDPMediaTypesEnum.audio)
                {
                    audioSink.GotAudioRtp(rep, rtpPkt.Header.SyncSource, rtpPkt.Header.SequenceNumber, rtpPkt.Header.Timestamp, rtpPkt.Header.PayloadType, rtpPkt.Header.MarkerBit == 1, rtpPkt.Payload);
                }
            };

            // Diagnostics.
            pc.OnReceiveReport += (re, media, rr) => mainThreadActions.Enqueue(
                () => logger.Log("RTCPReceive",media,rr.ReceiverReport));
            pc.OnSendReport += (media, sr) => mainThreadActions.Enqueue(
                () => logger.Log("RTCPSend",media,sr.SenderReport));
            pc.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) => mainThreadActions.Enqueue(
                () => logger.Log("STUN",ep,msg.Header.MessageType));
            pc.oniceconnectionstatechange += (state) => mainThreadActions.Enqueue(() =>
            {
                iceConnectionState = state;
                OnIceConnectionStateChanged.Invoke(state);
                logger.Log("IceConnectionStateChange",state);
            });

            if (!polite)
            {
                var offer = pc.createOffer();
                await pc.setLocalDescription(offer).ConfigureAwait(false);
                mainThreadActions.Enqueue(() => Send("Offer",offer.toJSON()));
            }

            return pc;
        }

        private void Update()
        {
            if (!setupTask.IsCompleted)
            {
                return;
            }

            rtcPeerConnection = setupTask.Result;

            // If RtcPeerConnection is initialised, process buffered actions
            while (mainThreadActions.TryDequeue(out Action action))
            {
                action();
            }

            // If RtcPeerConnection is initialised, process buffered messages
            while (messageQueue.Count > 0)
            {
                DoProcessMessage(messageQueue.Dequeue());
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
            if (rtcPeerConnection == null)
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
                        logger.Log("RemoteSDP",offer.type);

                        var result = rtcPeerConnection.setRemoteDescription(offer);
                        if (result != SetDescriptionResultEnum.OK)
                        {
                            logger.Log("SetRemoteDescriptionFailed",result);
                            rtcPeerConnection.Close("Failed to set remote description");
                        }
                        else
                        {
                            if(rtcPeerConnection.signalingState == RTCSignalingState.have_remote_offer)
                            {
                                var answerSdp = rtcPeerConnection.createAnswer();
                                rtcPeerConnection.setLocalDescription(answerSdp);

                                logger.Log("SendingSDPAnswer");

                                Send("Offer", answerSdp.toJSON());
                            }
                        }
                    }
                    break;
                case "IceCandidate":
                    if (RTCIceCandidateInit.TryParse(message.args,out RTCIceCandidateInit candidate))
                    {
                        logger.Log("IceCandidate",candidate.candidate);
                        rtcPeerConnection.addIceCandidate(candidate);
                    }
                    break;
            }
        }

        private void Send(string type, string args)
        {
            if (networkScene)
            {
                networkScene.SendJson(networkId, new Message() { type = type, args = args});
            }
        }

        /// <summary>
        /// Poll this PeerConnection for statistics about its bandwidth usage.
        /// </summary>
        /// <remarks>
        /// This information is also available through RTCP Reports. This method allows the statistics to be polled,
        /// rather than wait for a report. If this method is not never called, there is no performance overhead.
        /// </remarks>
        public Statistics GetStatistics()
        {
            Statistics report = new Statistics();
            if (rtcPeerConnection != null)
            {
                if (rtcPeerConnection.AudioRtcpSession != null)
                {
                    report.Audio.PacketsSent = rtcPeerConnection.AudioRtcpSession.PacketsSentCount;
                    report.Audio.PacketsRecieved = rtcPeerConnection.AudioRtcpSession.PacketsReceivedCount;
                    report.Audio.BytesSent = rtcPeerConnection.AudioRtcpSession.OctetsSentCount;
                    report.Audio.BytesReceived = rtcPeerConnection.AudioRtcpSession.OctetsReceivedCount;
                }
                if (rtcPeerConnection.VideoRtcpSession != null)
                {
                    report.Video.PacketsSent = rtcPeerConnection.VideoRtcpSession.PacketsSentCount;
                    report.Video.PacketsRecieved = rtcPeerConnection.VideoRtcpSession.PacketsReceivedCount;
                    report.Video.BytesSent = rtcPeerConnection.VideoRtcpSession.OctetsSentCount;
                    report.Video.BytesReceived = rtcPeerConnection.VideoRtcpSession.OctetsReceivedCount;
                }
            }
            return report;
        }
    }
}