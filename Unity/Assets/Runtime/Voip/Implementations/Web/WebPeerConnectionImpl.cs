using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Ubiq.Voip.Implementations.Web
{
    public class WebPeerConnectionImpl : IPeerConnectionImpl
    {
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_New();
        [DllImport("__Internal")]
        public static extern void JS_WebRTC_New_AddIceCandidate(int pc, string uri, string username, string password);
        [DllImport("__Internal")]
        public static extern void JS_WebRTC_New_SetPolite(int pc, bool polite);
        [DllImport("__Internal")]
        public static extern void JS_WebRTC_New_Start(int pc);
        [DllImport("__Internal")]
        public static extern bool JS_WebRTC_ResumeAudioContext();
        [DllImport("__Internal")]
        public static extern void JS_WebRTC_Close(int pc);
        [DllImport("__Internal")]
        public static extern bool JS_WebRTC_IsSetup(int pc);
        [DllImport("__Internal")]
        public static extern bool JS_WebRTC_ProcessSignallingMessage(int pc, int type, string args);
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_GetIceConnectionState(int pc);
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_GetPeerConnectionState(int pc);
        [DllImport("__Internal")]
        public static extern bool JS_WebRTC_SignallingMessages_Has(int pc);
        [DllImport("__Internal")]
        public static extern string JS_WebRTC_SignallingMessages_GetArgs(int pc);
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_SignallingMessages_GetType(int pc);
        [DllImport("__Internal")]
        public static extern void JS_WebRTC_SignallingMessages_Pop(int pc);
        [DllImport("__Internal")]
        public static extern void JS_WebRTC_SetPanner(int pc, float x, float y, float z);
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_GetStatsSamples(int pc);
        [DllImport("__Internal")]
        public static extern float JS_WebRTC_GetStatsVolume(int pc);
        [DllImport("__Internal")]
        public static extern void JS_WebRTC_EndStats(int pc);

        public event MessageEmittedDelegate signallingMessageEmitted;
        public event IceConnectionStateChangedDelegate iceConnectionStateChanged;
        public event PeerConnectionStateChangedDelegate peerConnectionStateChanged;

        private Queue<SignallingMessage> messageQueue = new Queue<SignallingMessage>();

        private MonoBehaviour context;

        private Coroutine updateCoroutine;
        private int peerConnectionId = -1;

        private PeerConnectionState lastPeerConnectionState = PeerConnectionState.@new;
        private IceConnectionState lastIceConnectionState = IceConnectionState.@new;

        public void Dispose()
        {
            if (updateCoroutine != null)
            {
                context.StopCoroutine(updateCoroutine);
                updateCoroutine = null;
            }

            if (peerConnectionId >= 0)
            {
                JS_WebRTC_Close(peerConnectionId);
                peerConnectionId = -1;
            }
        }

        public PlaybackStats GetLastFramePlaybackStats ()
        {
            return new PlaybackStats
            {
                sampleCount = JS_WebRTC_GetStatsSamples(peerConnectionId),
                volumeSum = JS_WebRTC_GetStatsVolume(peerConnectionId),
                sampleRate = 16000
            };
        }

        public void UpdateSpatialization(Vector3 sourcePosition,
            Quaternion sourceRotation, Vector3 listenerPosition,
            Quaternion listenerRotation)
        {
            // For WebRTC, need source position relative to listener
            var originToListener = Matrix4x4.TRS(listenerPosition,listenerRotation,Vector3.one);
            var p = originToListener.inverse.MultiplyPoint3x4(sourcePosition);

            JS_WebRTC_SetPanner(peerConnectionId,p.x,p.y,p.z);
        }

        public void Setup(MonoBehaviour context,
            bool polite, List<IceServerDetails> iceServers)
        {
            if (this.context != null)
            {
                // Already setup or setup in progress
                return;
            }

            this.context = context;
            updateCoroutine = context.StartCoroutine(Update());
            peerConnectionId = JS_WebRTC_New();
            for (int i = 0; i < iceServers.Count; i++)
            {
                JS_WebRTC_New_AddIceCandidate(
                    peerConnectionId,iceServers[i].uri,
                    iceServers[i].username,iceServers[i].password);
            }
            JS_WebRTC_New_SetPolite(peerConnectionId,polite);
            JS_WebRTC_New_Start(peerConnectionId);
        }

        public void ProcessSignallingMessage (SignallingMessage message)
        {
            messageQueue.Enqueue(message);
        }

        // private void DoProcessSignallingMessage(SignallingMessage message)
        // {
        //     JS_WebRTC_ProcessSignallingMessage(peerConnectionId,(int)message.type,message.args);
        //     // var type = message.ParseType();
        //     // switch(type)
        //     // {
        //     //     case Message.Type.Offer: ReceiveOffer(message); break;
        //     //     case Message.Type.IceCandidate: ReceiveIceCandidate(message); break;
        //     // }
        // }

        private void ProcessSignallingMessages()
        {
            var queueCount = messageQueue.Count;
            for (int i = 0; i < queueCount; i++)
            {
                var msg = messageQueue.Dequeue();
                Debug.Log("Process THIS: " + msg.type + " " + msg.args);
                if (!JS_WebRTC_ProcessSignallingMessage(peerConnectionId,(int)msg.type,msg.args))
                {
                    // Peer Connection isn't ready for this message yet - try again later
                    messageQueue.Enqueue(msg);
                }
            }
        }

        private IEnumerator Update()
        {
            while(peerConnectionId < 0 || !JS_WebRTC_IsSetup(peerConnectionId))
            {
                Debug.Log("Updating in top loop...");
                yield return null;
            }

            while(true)
            {
                JS_WebRTC_ResumeAudioContext();

                ProcessSignallingMessages();

                UpdateIceConnectionState();
                UpdatePeerConnectionState();

                SendSignallingMessages();

                JS_WebRTC_EndStats(peerConnectionId);

                // GetAudioSamples();

                yield return null;
            }
        }

        private void UpdateIceConnectionState()
        {
            var state = (IceConnectionState)
                JS_WebRTC_GetIceConnectionState(peerConnectionId);
            if (state != lastIceConnectionState)
            {
                Debug.Log("ICE Connection State Changed: " + state);
                iceConnectionStateChanged(state);
                lastIceConnectionState = state;
            }
        }

        private void UpdatePeerConnectionState()
        {
            var state = (PeerConnectionState)
                JS_WebRTC_GetPeerConnectionState(peerConnectionId);
            if (state != lastPeerConnectionState)
            {
                Debug.Log("Peer Connection State Changed: " + state);
                peerConnectionStateChanged(state);
                lastPeerConnectionState = state;
            }
        }

        private void SendSignallingMessages()
        {
            // Check for new ice candidates provided by the peer connection
            while (JS_WebRTC_SignallingMessages_Has(peerConnectionId))
            {
                // var len = JS_WebRTC_SignallingMessages_GetArgsLengthBytes(peerConnectionIdx);
                // var buf = new byte[len];
                // JS_WebRTC_SignallingMessages_GetArgs(peerConnectionIdx,buf);
                // var type = (Message.Type)JS_WebRTC_SignallingMessages_GetType(peerConnectionIdx);
                // var args = System.Text.Encoding.UTF8.GetString(buf);

                signallingMessageEmitted(new SignallingMessage{
                    type = (SignallingMessage.Type)JS_WebRTC_SignallingMessages_GetType(peerConnectionId),
                    args = JS_WebRTC_SignallingMessages_GetArgs(peerConnectionId)
                });

                JS_WebRTC_SignallingMessages_Pop(peerConnectionId);
            }
        }

        // private void UpdateIceCandidates()
        // {
        //     // Check for new ice candidates provided by the peer connection
        //     while (JS_WebRTC_IceCandidates_Has(peerConnectionIdx))
        //     {
        //         var args = new IceCandidateArgs();
        //         var buf = null as byte[];
        //         var len = 0;

        //         len = JS_WebRTC_IceCandidates_GetCandidateLengthBytes(peerConnectionIdx);
        //         buf = new byte[len];
        //         JS_WebRTC_IceCandidates_GetCandidate(peerConnectionIdx,buf);
        //         args.candidate = System.Text.Encoding.UTF8.GetString(buf);

        //         len = JS_WebRTC_IceCandidates_GetSdpMidLengthBytes(peerConnectionIdx);
        //         buf = new byte[len];
        //         JS_WebRTC_IceCandidates_GetSdpMid(peerConnectionIdx,buf);
        //         args.sdpMid = System.Text.Encoding.UTF8.GetString(buf);

        //         args.sdpMLineIndex =
        //             JS_WebRTC_IceCandidates_GetSdpMLineIndex(peerConnectionIdx);

        //         len = JS_WebRTC_IceCandidates_GetUsernameFragmentLengthBytes(peerConnectionIdx);
        //         buf = new byte[len];
        //         JS_WebRTC_IceCandidates_GetUsernameFragment(peerConnectionIdx,buf);
        //         args.usernameFragment = System.Text.Encoding.UTF8.GetString(buf);

        //         // Send to the remote end of the peer connection
        //         messageEmitted(Message.FromIceCandidate(args));

        //         JS_WebRTC_IceCandidates_Pop(peerConnectionIdx);
        //     }
        // }

        // private void SendOffer(RTCSessionDescriptionInit ssOffer)
        // {
        //     var offer = new SessionDescriptionArgs();
        //     offer.sdp = ssOffer.sdp;
        //     switch(ssOffer.type)
        //     {
        //         case RTCSdpType.answer : offer.type = SessionDescriptionArgs.Type.answer; break;
        //         case RTCSdpType.offer : offer.type = SessionDescriptionArgs.Type.offer; break;
        //         case RTCSdpType.pranswer : offer.type = SessionDescriptionArgs.Type.pranswer; break;
        //         case RTCSdpType.rollback : offer.type = SessionDescriptionArgs.Type.rollback; break;
        //     }
        //     messageEmitted(Message.FromOffer(offer));
        // }

        // private void SendIceCandidate(RTCIceCandidate iceCandidate)
        // {
        //     var args = new IceCandidateArgs
        //     {
        //         candidate = iceCandidate.candidate,
        //         sdpMid = iceCandidate.sdpMid,
        //         sdpMLineIndex = iceCandidate.sdpMLineIndex,
        //         usernameFragment = iceCandidate.usernameFragment
        //     };
        //     messageEmitted(Message.FromIceCandidate(args));
        // }

        // private void ReceiveOffer(Message message)
        // {
        //     if (message.ParseOffer(out var offer))
        //     {
        //         Debug.Log($"Got remote SDP, type {offer.type}");
        //         var result = rtcPeerConnection.setRemoteDescription(ssOffer);
        //         if (result != SetDescriptionResultEnum.OK)
        //         {
        //             Debug.Log($"Failed to set remote description, {result}.");
        //             rtcPeerConnection.Close("Failed to set remote description");
        //         }
        //         else
        //         {
        //             if(rtcPeerConnection.signalingState == RTCSignalingState.have_remote_offer)
        //             {
        //                 var answerSdp = rtcPeerConnection.createAnswer();
        //                 rtcPeerConnection.setLocalDescription(answerSdp);

        //                 Debug.Log($"Sending SDP answer");

        //                 SendOffer(answerSdp);
        //             }
        //         }
        //     }
        // }

        // private void ReceiveIceCandidate(Message message)
        // {
        //     if (message.ParseIceCandidate(out var iceCandidate))
        //     {
        //         Debug.Log($"Got remote Ice Candidate, uri {iceCandidate.candidate}");
        //         JS_WebRTC_AddIceCandidate(iceCandidate.candidate,
        //             iceCandidate.sdpMid,iceCandidate.sdpMLineIndex,
        //             iceCandidate.usernameFragment);
        //     }
        // }
    }
}