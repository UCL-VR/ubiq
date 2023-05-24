using UnityEngine;

namespace Ubiq.Voip.Implementations.Dotnet
{
    public interface IVoipSink : SIPSorceryMedia.Abstractions.IAudioSink
    {
        PlaybackStats GetLastFramePlaybackStats();
        void UpdateSpatialization(Vector3 sourcePosition, Quaternion sourceRotation,
            Vector3 listenerPosition, Quaternion listenerRotation);
    }
}