using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using Ubiq.Voip.Implementations.JsonHelpers;

namespace Ubiq.Voip.Implementations.Web
{
    public class PeerConnectionImpl : IPeerConnectionImpl
    {
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_New();
        [DllImport("__Internal")]
        public static extern void JS_WebRTC_New_AddIceCandidate(int pc,
            string uri, string username, string password);
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
        public static extern void JS_WebRTC_SetPolite(int pc, bool polite);
        [DllImport("__Internal")]
        public static extern bool JS_WebRTC_ProcessSignalingMessage(int pc,
            string candidate, string sdpMid, bool sdpMLineIndexIsNull,
            int sdpMLineIndex, string usernameFragment, string type, string sdp);
        [DllImport("__Internal")]
        public static extern bool JS_WebRTC_HasRemoteDescription(int pc);
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_GetIceConnectionState(int pc);
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_GetPeerConnectionState(int pc);
        [DllImport("__Internal")]
        public static extern bool JS_WebRTC_SignalingMessages_Has(int pc);
        [DllImport("__Internal")]
        public static extern string JS_WebRTC_SignalingMessages_GetCandidate(int pc);
        [DllImport("__Internal")]
        public static extern string JS_WebRTC_SignalingMessages_GetSdpMid(int pc);
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_SignalingMessages_GetSdpMLineIndex(int pc);
        [DllImport("__Internal")]
        public static extern string JS_WebRTC_SignalingMessages_GetUsernameFragment(int pc);
        [DllImport("__Internal")]
        public static extern string JS_WebRTC_SignalingMessages_GetType(int pc);
        [DllImport("__Internal")]
        public static extern string JS_WebRTC_SignalingMessages_GetSdp(int pc);
        [DllImport("__Internal")]
        public static extern void JS_WebRTC_SignalingMessages_Pop(int pc);
        [DllImport("__Internal")]
        public static extern void JS_WebRTC_SetPanner(int pc, float x, float y, float z);
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_GetPlaybackStatsSampleCount(int pc);
        [DllImport("__Internal")]
        public static extern float JS_WebRTC_GetPlaybackStatsVolumeSum(int pc);
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_GetPlaybackStatsSampleRate(int pc);
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_GetRecordStatsSampleCount();
        [DllImport("__Internal")]
        public static extern float JS_WebRTC_GetRecordStatsVolumeSum();
        [DllImport("__Internal")]
        public static extern int JS_WebRTC_GetRecordStatsSampleRate();
        [DllImport("__Internal")]
        public static extern void JS_WebRTC_EndStats(int pc, int frameCount);

        private class Context
        {
            public IPeerConnectionContext context;
            public Action<AudioStats> playbackStatsPushed;
            public Action<AudioStats> recordStatsPushed;
            public Action<IceConnectionState> iceConnectionStateChanged;
            public Action<PeerConnectionState> peerConnectionStateChanged;
            public bool polite;

            public MonoBehaviour behaviour => context.behaviour;
            public GameObject gameObject => context.behaviour.gameObject;
            public Transform transform => context.behaviour.transform;
        }

        private enum Implementation
        {
            Unknown,
            Dotnet,
            Other
        }

        private Queue<SignalingMessage> messageQueue = new Queue<SignalingMessage>();

        private Coroutine updateCoroutine;
        private int peerConnectionId = -1;

        private PeerConnectionState lastPeerConnectionState = PeerConnectionState.@new;
        private IceConnectionState lastIceConnectionState = IceConnectionState.@new;

        private Implementation otherPeerImplementation = Implementation.Unknown;

        // Workaround for chrome issue, buffer candidates if remote desc
        // not yet set https://stackoverflow.com/questions/38198751
        private bool hasRemoteDescription = false;

        private Context ctx;

        public void Dispose()
        {
            if (updateCoroutine != null)
            {
                if (ctx != null && ctx.behaviour)
                {
                    ctx.behaviour.StopCoroutine(updateCoroutine);
                }
                updateCoroutine = null;
            }

            if (peerConnectionId >= 0)
            {
                JS_WebRTC_Close(peerConnectionId);
                peerConnectionId = -1;
            }

            ctx = null;
        }

        // public PlaybackStats GetLastFramePlaybackStats ()
        // {
        //     return new PlaybackStats
        //     {
        //         sampleCount = JS_WebRTC_GetStatsSamples(peerConnectionId),
        //         volumeSum = JS_WebRTC_GetStatsVolume(peerConnectionId),
        //         sampleRate = 16000
        //     };
        // }

        public void UpdateSpatialization(Vector3 sourcePosition,
            Quaternion sourceRotation, Vector3 listenerPosition,
            Quaternion listenerRotation)
        {
            // For WebRTC, need source position relative to listener
            var originToListener = Matrix4x4.TRS(listenerPosition,listenerRotation,Vector3.one);
            var p = originToListener.inverse.MultiplyPoint3x4(sourcePosition);

            JS_WebRTC_SetPanner(peerConnectionId,p.x,p.y,p.z);
        }

        public void Setup(IPeerConnectionContext context,
            bool polite,
            List<IceServerDetails> iceServers,
            Action<AudioStats> playbackStatsPushed,
            Action<AudioStats> recordStatsPushed,
            Action<IceConnectionState> iceConnectionStateChanged,
            Action<PeerConnectionState> peerConnectionStateChanged)
        {
            if (ctx != null)
            {
                // Already setup
                return;
            }

            ctx = new Context();
            ctx.context = context;
            ctx.polite = polite;
            ctx.playbackStatsPushed = playbackStatsPushed;
            ctx.recordStatsPushed = recordStatsPushed;
            ctx.iceConnectionStateChanged = iceConnectionStateChanged;
            ctx.peerConnectionStateChanged = peerConnectionStateChanged;

            peerConnectionId = JS_WebRTC_New();
            for (int i = 0; i < iceServers.Count; i++)
            {
                JS_WebRTC_New_AddIceCandidate(
                    peerConnectionId,iceServers[i].uri,
                    iceServers[i].username,iceServers[i].password);
            }
            JS_WebRTC_New_SetPolite(peerConnectionId,polite);
            JS_WebRTC_New_Start(peerConnectionId);

            updateCoroutine = ctx.behaviour.StartCoroutine(Update());
        }

        public void ProcessSignalingMessage (string json)
        {
            messageQueue.Enqueue(JsonHelpers.SignalingMessageHelper.FromJson(json));
        }

        private void ProcessSignalingMessages()
        {
            var queueCount = messageQueue.Count;
            for (int i = 0; i < queueCount; i++)
            {
                var msg = messageQueue.Dequeue();

                // Id the other implementation as Dotnet requires special treatment
                if (otherPeerImplementation == Implementation.Unknown)
                {
                    otherPeerImplementation = msg.implementation == "dotnet"
                        ? Implementation.Dotnet
                        : Implementation.Other;

                    if (otherPeerImplementation == Implementation.Dotnet)
                    {
                        // If just one of the two peers is dotnet, the
                        // non-dotnet peer always takes on the role of polite
                        // peer as the dotnet implementaton isn't smart enough
                        // to handle rollback
                        JS_WebRTC_SetPolite(peerConnectionId,true);
                    }
                }

                // Workaround for chrome issue, buffer candidates if remote desc
                // not yet set https://stackoverflow.com/questions/38198751
                if (!hasRemoteDescription && msg.candidate != null) {
                    // Peer Connection isn't ready for this message yet - try again later
                    messageQueue.Enqueue(msg);
                    continue;
                }

                JS_WebRTC_ProcessSignalingMessage(peerConnectionId,msg.candidate,
                    msg.sdpMid,msg.sdpMLineIndex == null, msg.sdpMLineIndex ?? 0,
                    msg.usernameFragment,msg.type,msg.sdp);
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

                ProcessSignalingMessages();
                SendSignalingMessages();

                JS_WebRTC_EndStats(peerConnectionId,Time.frameCount);

                PushPlaybackStats();
                PushRecordStats();

                yield return null;
            }
        }

        private void PushPlaybackStats()
        {
            ctx.playbackStatsPushed?.Invoke(new AudioStats
            (
                sampleCount: JS_WebRTC_GetPlaybackStatsSampleCount(peerConnectionId),
                volumeSum: JS_WebRTC_GetPlaybackStatsVolumeSum(peerConnectionId),
                sampleRate: JS_WebRTC_GetPlaybackStatsSampleRate(peerConnectionId)
            ));
        }

        private void PushRecordStats()
        {
            ctx.recordStatsPushed?.Invoke(new AudioStats
            (
                sampleCount: JS_WebRTC_GetRecordStatsSampleCount(),
                volumeSum: JS_WebRTC_GetRecordStatsVolumeSum(),
                sampleRate: JS_WebRTC_GetRecordStatsSampleRate()
            ));
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
                ctx.iceConnectionStateChanged?.Invoke(state);
                lastIceConnectionState = state;
            }
        }

        private void UpdatePeerConnectionState()
        {
            var state = (PeerConnectionState)
                JS_WebRTC_GetPeerConnectionState(peerConnectionId);
            if (state != lastPeerConnectionState)
            {
                ctx.peerConnectionStateChanged?.Invoke(state);
                lastPeerConnectionState = state;
            }
        }

        private void SendSignalingMessages()
        {
            // Check for new ice candidates provided by the peer connection
            while (JS_WebRTC_SignalingMessages_Has(peerConnectionId))
            {
                var sdp = JS_WebRTC_SignalingMessages_GetSdp(peerConnectionId);
                if (sdp != null)
                {
                    ctx.context.Send(SdpMessage.ToJson(new SdpMessage
                    (
                        type:JS_WebRTC_SignalingMessages_GetType(peerConnectionId),
                        sdp:sdp
                    )));
                }
                else
                {
                    var sdpMLineIndex = JS_WebRTC_SignalingMessages_GetSdpMLineIndex(peerConnectionId);
                    ctx.context.Send(IceCandidateMessage.ToJson(new IceCandidateMessage
                    (
                        candidate:JS_WebRTC_SignalingMessages_GetCandidate(peerConnectionId),
                        sdpMid:JS_WebRTC_SignalingMessages_GetSdpMid(peerConnectionId),
                        sdpMLineIndex:sdpMLineIndex < 0 ? null : (ushort?)sdpMLineIndex,
                        usernameFragment:JS_WebRTC_SignalingMessages_GetUsernameFragment(peerConnectionId)
                    )));
                }

                JS_WebRTC_SignalingMessages_Pop(peerConnectionId);
            }
        }
    }
}