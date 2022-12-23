using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Voip.Implementations.Null
{
    public class NullPeerConnectionImpl : IPeerConnectionImpl
    {
#pragma warning disable CS0067
        public event MessageEmittedDelegate signallingMessageEmitted;
        public event IceConnectionStateChangedDelegate iceConnectionStateChanged;
        public event PeerConnectionStateChangedDelegate peerConnectionStateChanged;
#pragma warning restore CS0067
        public void Dispose() {}
        public PlaybackStats GetLastFramePlaybackStats() => new PlaybackStats();
        public void ProcessSignallingMessage(SignallingMessage message) {}
        public void Setup(MonoBehaviour context, bool polite, List<IceServerDetails> iceServers) {
            // Pretend we are connected to silence/hide warnings
            if (iceConnectionStateChanged != null)
            {
                iceConnectionStateChanged(IceConnectionState.connected);
            }
        }
        public void UpdateSpatialization(Vector3 sourcePosition, Quaternion sourceRotation, Vector3 listenerPosition, Quaternion listenerRotation) {}
    }
}