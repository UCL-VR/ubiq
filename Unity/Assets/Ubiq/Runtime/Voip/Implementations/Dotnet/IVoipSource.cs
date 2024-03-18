using System;

namespace Ubiq.Voip.Implementations.Dotnet
{
    public interface IVoipSource : SIPSorceryMedia.Abstractions.IAudioSource
    {
        event Action<AudioStats> statsPushed;
    }
}