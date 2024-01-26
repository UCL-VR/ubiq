using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Voip.Implementations
{
    public class NullPeerConnectionImpl : IPeerConnectionImpl
    {
        public void Dispose() {}
        public void ProcessSignalingMessage(string json) {}
        public void Setup(IPeerConnectionContext context, bool polite, List<IceServerDetails> iceServers, Action<AudioStats> playbackStatsPushed, Action<AudioStats> recordStatsPushed, Action<IceConnectionState> iceConnectionStateChanged, Action<PeerConnectionState> peerConnectionStateChanged)
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