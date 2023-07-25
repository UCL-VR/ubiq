using System;
using System.Collections.Generic;

namespace Ubiq.Voip
{
    public class VolumeEstimator
    {
        public float volume { get { UpdateEstimate(); return _volume; } }
        public float delaySeconds { get; private set; }
        public float lengthSeconds { get; private set; }

        private List<VoipPeerConnection.AudioStats> stats = new List<VoipPeerConnection.AudioStats>();
        private bool isDirty = false;
        private float _volume;

        public VolumeEstimator(float delaySeconds, float lengthSeconds)
        {
            SetWindow(delaySeconds,lengthSeconds);
        }

        public void SetWindow(float delaySeconds, float lengthSeconds)
        {
            this.delaySeconds = delaySeconds;
            this.lengthSeconds = lengthSeconds;
        }

        /// <summary>
        /// Pushes a new set of audio stats to the estimator. Treats the stats
        /// as a continuous stream, where these are the very latest stats.
        /// </summary>
        public void PushAudioStats(VoipPeerConnection.AudioStats stats)
        {
            if (stats.sampleCount == 0)
            {
                return;
            }

            if (this.stats.Count > 0 && this.stats[0].sampleRate != stats.sampleRate)
            {
                // May happen if the audio device changes, which should be rare
                // so just clear the buffer and start again. Will mean a small
                // interruption in the indicator, but it ensure the entire stats
                // buffer has the same sampleRate. Simplifies things a lot.
                this.stats.Clear();
            }

            this.stats.Add(stats);
            isDirty = true;
        }

        private void UpdateEstimate()
        {
            if (!isDirty)
            {
                return;
            }

            if (stats.Count == 0)
            {
                _volume = 0;
                return;
            }

            var startSample = delaySeconds * stats[0].sampleRate;
            var endSample = (delaySeconds + lengthSeconds) * stats[0].sampleRate;
            var volumeSum = 0.0f;
            var sampleCount = 0.0f;
            var currentSample = 0;
            var idx = stats.Count-1;
            for (; idx >= 0; idx--)
            {
                if (currentSample + stats[idx].sampleCount > startSample)
                {
                    var t = 1.0f;
                    var complete = false;
                    if (currentSample < startSample)
                    {
                        // First stats window
                        t = 1 - ((startSample - currentSample) / stats[idx].sampleCount);
                    }

                    if (currentSample + stats[idx].sampleCount > endSample)
                    {
                        // Last stats window
                        t = (endSample - currentSample) / stats[idx].sampleCount;
                        complete = true;
                    }

                    volumeSum += stats[idx].volumeSum * t;
                    sampleCount += stats[idx].sampleCount * t;

                    if (complete)
                    {
                        break;
                    }
                }
                currentSample += stats[idx].sampleCount;
            }

            stats.RemoveRange(0,idx > 0 ? idx : 0);

            _volume = volumeSum / sampleCount;
            isDirty = false;
        }
    }
}