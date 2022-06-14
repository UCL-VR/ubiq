using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Voip
{
    public struct Stats
    {
        public float volume;
        public int samples;
        public int sampleRate;
    }

    public interface IAudioStats
    {
        Stats lastFrameStats  {get; }
    }

    public interface IOutputVolume
    {
        float Volume { get; set; }
    }
}