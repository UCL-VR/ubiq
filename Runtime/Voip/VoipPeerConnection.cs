using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Messaging;
using Ubiq.Voip.Implementations;

namespace Ubiq.Voip
{
    public class VoipPeerConnection : MonoBehaviour
    {
        private class PeerConnectionContext : IPeerConnectionContext
        {
            MonoBehaviour IPeerConnectionContext.behaviour => (MonoBehaviour)peerConnection;
            void IPeerConnectionContext.Send(string json) => peerConnection.SendFromImpl(json);

            private VoipPeerConnection peerConnection;

            public PeerConnectionContext (VoipPeerConnection peerConnection)
            {
                this.peerConnection = peerConnection;
            }
        }

        // Defined here as well as in IPeerConnectionImpl for external use
        public enum IceConnectionState
        {
            closed = 0,
            failed = 1,
            disconnected = 2,
            @new = 3,
            checking = 4,
            connected = 5,
            completed = 6
        }

        // Defined here as well as in IPeerConnectionImpl for external use
        public enum PeerConnectionState
        {
            closed = 0,
            failed = 1,
            disconnected = 2,
            @new = 3,
            connecting = 4,
            connected = 5
        }

        // Defined here as well as in IPeerConnectionImpl for external use
        [Serializable]
        public struct AudioStats : IEquatable<AudioStats>
        {
            [field: SerializeField] public int sampleCount { get; private set; }
            [field: SerializeField] public float volumeSum { get; private set; }
            [field: SerializeField] public int sampleRate { get; private set; }

            public AudioStats(int sampleCount, float volumeSum, int sampleRate)
            {
                this.sampleCount = sampleCount;
                this.volumeSum = volumeSum;
                this.sampleRate = sampleRate;
            }

            public override bool Equals(object obj)
            {
                return obj is AudioStats stats && Equals(stats);
            }

            public bool Equals(AudioStats other)
            {
                return sampleCount == other.sampleCount &&
                       volumeSum == other.volumeSum &&
                       sampleRate == other.sampleRate;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(sampleCount, volumeSum, sampleRate);
            }

            public static bool operator ==(AudioStats left, AudioStats right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(AudioStats left, AudioStats right)
            {
                return !(left == right);
            }
        }

        public string PeerUuid { get; private set; }

        public bool Polite { get; private set; }

        public IceConnectionState iceConnectionState { get; private set; } = IceConnectionState.@new;
        public PeerConnectionState peerConnectionState { get; private set; } = PeerConnectionState.@new;

        [Serializable] public class IceConnectionStateEvent : UnityEvent<IceConnectionState> { }
        [Serializable] public class PeerConnectionStateEvent : UnityEvent<PeerConnectionState> { }

        public IceConnectionStateEvent OnIceConnectionStateChanged = new IceConnectionStateEvent();
        public PeerConnectionStateEvent OnPeerConnectionStateChanged = new PeerConnectionStateEvent();

        // C# events rather than Unity events as these will be potentially be
        // called multiple times every frame, and Unity events are slow by
        // comparison (https://www.jacksondunstan.com/articles/3335)
        public event Action<AudioStats> playbackStatsPushed;
        public event Action<AudioStats> recordStatsPushed;

        private NetworkId networkId;
        private NetworkScene networkScene;
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

        public void UpdateSpatialization(Vector3 sourcePosition,
            Quaternion sourceRotation, Vector3 listenerPosition,
            Quaternion listenerRotation)
        {
            impl.UpdateSpatialization(sourcePosition,sourceRotation,
                listenerPosition,listenerRotation);
        }

        public void Setup (NetworkId networkId, NetworkScene scene,
            string peerUuid, bool polite, List<IceServerDetails> iceServers)
        {
            if (isSetup)
            {
                return;
            }

            this.Polite = polite;
            this.networkId = networkId;
            this.PeerUuid = peerUuid;
            this.networkScene = scene;

            this.impl = PeerConnectionImplFactory.Create();

            networkScene.AddProcessor(networkId, ProcessMessage);

            impl.Setup(new PeerConnectionContext(this),polite,iceServers,
                Impl_PlaybackStatsPushed,Impl_RecordStatsPushed,
                Impl_IceConnectionStateChanged,Impl_PeerConnectionStateChanged);
            isSetup = true;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage data)
        {
            if (impl != null)
            {
                impl.ProcessSignalingMessage(data.ToString());
            }
        }

        private void SendFromImpl(string json)
        {
            networkScene.Send(networkId,json);
        }

        private static AudioStats ConvertStats(Implementations.AudioStats stats)
        {
            return new AudioStats(stats.sampleCount,stats.volumeSum,stats.sampleRate);
        }

        private void Impl_PlaybackStatsPushed (Implementations.AudioStats stats)
        {
            playbackStatsPushed?.Invoke(ConvertStats(stats));
        }

        private void Impl_RecordStatsPushed (Implementations.AudioStats stats)
        {
            recordStatsPushed?.Invoke(ConvertStats(stats));
        }

        private void Impl_IceConnectionStateChanged (Implementations.IceConnectionState state)
        {
            iceConnectionState = (IceConnectionState)state;
            OnIceConnectionStateChanged.Invoke((IceConnectionState)state);
        }

        private void Impl_PeerConnectionStateChanged (Implementations.PeerConnectionState state)
        {
            peerConnectionState = (PeerConnectionState)state;
            OnPeerConnectionStateChanged.Invoke((PeerConnectionState)state);
        }
    }
}