using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Voip.Implementations.Null
{
    public class PeerConnectionImpl : IPeerConnectionImpl
    {
#pragma warning disable CS0067
        public event IceConnectionStateChangedDelegate iceConnectionStateChanged;
        public event PeerConnectionStateChangedDelegate peerConnectionStateChanged;
#pragma warning restore CS0067
        public void Dispose() {}
        public PlaybackStats GetLastFramePlaybackStats() => new PlaybackStats();
        public void ProcessSignalingMessage(string json) {}
        public void Setup(IPeerConnectionContext context, bool polite, List<IceServerDetails> iceServers)
        {
            // Pretend we are connected to silence/hide warnings
            if (iceConnectionStateChanged != null)
            {
                iceConnectionStateChanged(IceConnectionState.connected);
            }
        }
        public void UpdateSpatialization(Vector3 sourcePosition, Quaternion sourceRotation, Vector3 listenerPosition, Quaternion listenerRotation) {}
    }
}