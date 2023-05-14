using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

namespace Ubiq.Voip.Implementations.Web
{
    public class PeerConnectionImpl : IPeerConnectionImpl
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
        public static extern bool JS_WebRTC_IsStarted(int pc);
        [DllImport("__Internal")]
        public static extern bool JS_WebRTC_ProcessSignallingMessage(int pc, int type, string args);
        [DllImport("__Internal")]
        public static extern bool JS_WebRTC_HasRemoteDescription(int pc);
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

        public event IceConnectionStateChangedDelegate iceConnectionStateChanged;
        public event PeerConnectionStateChangedDelegate peerConnectionStateChanged;

        private Queue<SignallingMessage> messageQueue = new Queue<SignallingMessage>();

        private IPeerConnectionContext context;

        private Coroutine updateCoroutine;
        private int peerConnectionId = -1;

        private PeerConnectionState lastPeerConnectionState = PeerConnectionState.@new;
        private IceConnectionState lastIceConnectionState = IceConnectionState.@new;

        // Workaround for chrome issue, buffer candidates if remote desc
        // not yet set https://stackoverflow.com/questions/38198751
        private bool hasRemoteDescription = false;

        public void Dispose()
        {
            if (updateCoroutine != null)
            {
                context.behaviour.StopCoroutine(updateCoroutine);
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

        public void Setup(IPeerConnectionContext context, bool polite,
            List<IceServerDetails> iceServers)
        {
            if (this.context != null)
            {
                // Already setup or setup in progress
                return;
            }

            this.context = context;
            peerConnectionId = JS_WebRTC_New();
            for (int i = 0; i < iceServers.Count; i++)
            {
                JS_WebRTC_New_AddIceCandidate(
                    peerConnectionId,iceServers[i].uri,
                    iceServers[i].username,iceServers[i].password);
            }
            JS_WebRTC_New_SetPolite(peerConnectionId,polite);
            JS_WebRTC_New_Start(peerConnectionId);

            updateCoroutine = context.behaviour.StartCoroutine(Update());
        }

        public void ProcessSignallingMessage (SignallingMessage message)
        {
            messageQueue.Enqueue(message);
        }

        private void ProcessSignallingMessages()
        {
            var queueCount = messageQueue.Count;
            for (int i = 0; i < queueCount; i++)
            {
                var msg = messageQueue.Dequeue();

                // Workaround for chrome issue, buffer candidates if remote desc
                // not yet set https://stackoverflow.com/questions/38198751
                if (!hasRemoteDescription && msg.type == SignallingMessage.Type.IceCandidate) {
                    // Peer Connection isn't ready for this message yet - try again later
                    messageQueue.Enqueue(msg);
                    continue;
                }

                JS_WebRTC_ProcessSignallingMessage(peerConnectionId,
                    (int)msg.type,msg.args);
            }
        }

        private IEnumerator Update()
        {
            while(true)
            {
                UpdateHasRemoteDescription();
                UpdateIceConnectionState();
                UpdatePeerConnectionState();

                JS_WebRTC_ResumeAudioContext();

                ProcessSignallingMessages();
                SendSignallingMessages();

                JS_WebRTC_EndStats(peerConnectionId);

                yield return null;
            }
        }

        private void UpdateHasRemoteDescription()
        {
            hasRemoteDescription = hasRemoteDescription ||
                JS_WebRTC_HasRemoteDescription(peerConnectionId);
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
                context.Send(new SignallingMessage{
                    type = (SignallingMessage.Type)JS_WebRTC_SignallingMessages_GetType(peerConnectionId),
                    args = JS_WebRTC_SignallingMessages_GetArgs(peerConnectionId)
                });

                JS_WebRTC_SignallingMessages_Pop(peerConnectionId);
            }
        }
    }
}