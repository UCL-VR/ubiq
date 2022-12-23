using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Messaging;
using Ubiq.Logging;
using Ubiq.Voip.Factory;
using Ubiq.Voip.Implementations;

namespace Ubiq.Voip
{
    public class VoipPeerConnection : MonoBehaviour
    {
        // Defined here as well as in Impl for external use
        public enum IceConnectionState
        {
            closed = 0,
            failed = 1,
            disconnected = 2,
            @new = 3,
            checking = 4,
            connected = 5
        }

        // Defined here as well as in Impl for external use
        public enum PeerConnectionState
        {
            closed = 0,
            failed = 1,
            disconnected = 2,
            @new = 3,
            connecting = 4,
            connected = 5
        }

        // Defined here as well as in Impl for external use
        public struct PlaybackStats
        {
            public int samples;
            public float volume;
            public int sampleRate;
        }

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
        public struct TransmissionStats
        {
            public SessionStatistics Audio;
            public SessionStatistics Video;
        }

        public string peerUuid { get; private set; }

        public IceConnectionState iceConnectionState { get; private set; } = IceConnectionState.@new;
        public PeerConnectionState peerConnectionState { get; private set; } = PeerConnectionState.@new;

        [Serializable] public class IceConnectionStateEvent : UnityEvent<IceConnectionState> { }
        [Serializable] public class PeerConnectionStateEvent : UnityEvent<PeerConnectionState> { }

        public IceConnectionStateEvent OnIceConnectionStateChanged = new IceConnectionStateEvent();
        public PeerConnectionStateEvent OnPeerConnectionStateChanged = new PeerConnectionStateEvent();

        private NetworkId networkId;
        private NetworkScene networkScene;
        private LogEmitter logger;
        private IPeerConnectionImpl impl;

        private bool isSetup;

        private void OnDestroy()
        {
            if (networkScene)
            {
                networkScene.RemoveProcessor(networkId,ProcessMessage);
            }

            if (impl != null)
            {
                impl.Dispose();
                impl = null;
            }
        }

        // todo-remotepeer just take relative co-ords
        // public void SetRemotePeerPosition(Vector3 worldPosition, Quaternion worldRotation)
        // {
        //     if (!networkScene)
        //     {
        //         return;
        //     }

        //     var avatarManager = networkScene.GetComponentInChildren<Ubiq.Avatars.AvatarManager>();
        //     if (!avatarManager)
        //     {
        //         return;
        //     }

        //     var localVoipAvatar = avatarManager.LocalAvatar.GetComponent<Ubiq.Avatars.VoipAvatar>();
        //     if (!localVoipAvatar)
        //     {
        //         return;
        //     }

        //     var listener = localVoipAvatar.audioSourcePosition;
        //     var relativePosition = listener.InverseTransformPoint(worldPosition);
        //     var relativeRotation = Quaternion.Inverse(listener.rotation) * worldRotation;

        //     impl.SetRemotePeerRelativePosition(relativePosition,relativeRotation);
        // }

        public void UpdateSpatialization(Vector3 sourcePosition,
            Quaternion sourceRotation, Vector3 listenerPosition,
            Quaternion listenerRotation)
        {
            impl.UpdateSpatialization(sourcePosition,sourceRotation,
                listenerPosition,listenerRotation);
        }

        // todo
//         public VoipAudioSourceOutput.Stats GetLastFrameStats ()
//         {
// #if UNITY_WEBGL && !UNITY_EDITOR
//             if (impl != null)
//             {
//                 return impl.GetStats();
//             }
//             else
//             {
//                 return new VoipAudioSourceOutput.Stats {samples = 0, volume = 0};
//             }
// #else
//             if (audioSink)
//             {
//                 return audioSink.lastFrameStats;
//             }
//             else
//             {
//                 return new VoipAudioSourceOutput.Stats {samples = 0, volume = 0};
//             }
// #endif
//         }

        public void Setup (NetworkId networkId, NetworkScene scene,
            string peerUuid, bool polite, List<IceServerDetails> iceServers)
        {
            if (isSetup)
            {
                return;
            }

            this.networkId = networkId;
            this.peerUuid = peerUuid;
            this.networkScene = scene;
            this.logger = new NetworkEventLogger(networkId, networkScene, this);

            this.impl = PeerConnectionImplFactory.Create();

            impl.signallingMessageEmitted += OnImplMessageEmitted;
            impl.iceConnectionStateChanged += OnImplIceConnectionStateChanged;
            impl.peerConnectionStateChanged += OnImplPeerConnectionStateChanged;

            networkScene.AddProcessor(networkId, ProcessMessage);

            impl.Setup(this,polite,iceServers);
            isSetup = true;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage data)
        {
            if (impl != null)
            {
                var message = data.FromJson<SignallingMessage>();
                impl.ProcessSignallingMessage(message);
            }
        }

        private void OnImplMessageEmitted (SignallingMessage message)
        {
            networkScene.SendJson(networkId,message);
        }

        private void OnImplIceConnectionStateChanged (Ubiq.Voip.Implementations.IceConnectionState state)
        {
            iceConnectionState = (IceConnectionState)state;
            OnIceConnectionStateChanged.Invoke((IceConnectionState)state);
        }

        private void OnImplPeerConnectionStateChanged (Ubiq.Voip.Implementations.PeerConnectionState state)
        {
            peerConnectionState = (PeerConnectionState)state;
            OnPeerConnectionStateChanged.Invoke((PeerConnectionState)state);
        }

        /// <summary>
        /// Poll this PeerConnection for statistics about its bandwidth usage.
        /// </summary>
        /// <remarks>
        /// This information is also available through RTCP Reports. This method allows the statistics to be polled,
        /// rather than wait for a report. If this method is not never called, there is no performance overhead.
        /// </remarks>
        public TransmissionStats GetTransmissionStats()
        {
            TransmissionStats report = new TransmissionStats();
            //todo

            // if (rtcPeerConnection != null)
            // {
            //     if (rtcPeerConnection.AudioRtcpSession != null)
            //     {
            //         report.Audio.PacketsSent = rtcPeerConnection.AudioRtcpSession.PacketsSentCount;
            //         report.Audio.PacketsRecieved = rtcPeerConnection.AudioRtcpSession.PacketsReceivedCount;
            //         report.Audio.BytesSent = rtcPeerConnection.AudioRtcpSession.OctetsSentCount;
            //         report.Audio.BytesReceived = rtcPeerConnection.AudioRtcpSession.OctetsReceivedCount;
            //     }
            //     if (rtcPeerConnection.VideoRtcpSession != null)
            //     {
            //         report.Video.PacketsSent = rtcPeerConnection.VideoRtcpSession.PacketsSentCount;
            //         report.Video.PacketsRecieved = rtcPeerConnection.VideoRtcpSession.PacketsReceivedCount;
            //         report.Video.BytesSent = rtcPeerConnection.VideoRtcpSession.OctetsSentCount;
            //         report.Video.BytesReceived = rtcPeerConnection.VideoRtcpSession.OctetsReceivedCount;
            //     }
            // }
            return report;
        }

        public PlaybackStats GetLastFramePlaybackStats()
        {
            var playbackStats = new PlaybackStats();
            if (impl != null)
            {
                var implStats = impl.GetLastFramePlaybackStats();
                playbackStats.volume = implStats.volumeSum;
                playbackStats.samples = implStats.sampleCount;
                playbackStats.sampleRate = implStats.sampleRate;
            }
            return playbackStats;
        }
    }
}