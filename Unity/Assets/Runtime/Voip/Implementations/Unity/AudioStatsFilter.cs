using System;
using UnityEngine;
using System.Collections.Concurrent;

namespace Ubiq.Voip.Implementations.Unity
{
    /// <summary>
    /// Gather audio stats from the attached AudioSource
    /// </summary>
    public class AudioStatsFilter : MonoBehaviour
    {
        private ConcurrentQueue<AudioStats> statsQueue = new ConcurrentQueue<AudioStats>();
        private int sampleRate;
        private Action<AudioStats> statsPushed;

        private void OnDestroy()
        {
            statsPushed = null;
        }

        private void OnEnable()
        {
            OnAudioConfigurationChanged(false);
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        private void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        }

        private void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            sampleRate = AudioSettings.outputSampleRate;
        }

        private void Update()
        {
            while (statsQueue.TryDequeue(out var stats))
            {
                statsPushed?.Invoke(new AudioStats(
                    stats.sampleCount,stats.volumeSum,sampleRate
                ));
            }
        }

        public void SetStatsPushedCallback(Action<AudioStats> statsPushed)
        {
            this.statsPushed = statsPushed;
        }

        /// <summary>
        /// </summary>
        /// <note>
        /// Called on the audio thread, not the main thread.
        /// </note>
        /// <param name="data"></param>
        /// <param name="channels"></param>
        private void OnAudioFilterRead(float[] data, int channels)
        {
            var volumeSum = 0.0f;
            for (int i = 0; i < data.Length; i+=channels)
            {
                volumeSum += Mathf.Abs(data[i]);
            }

            var length = data.Length/channels;
            volumeSum = volumeSum/channels;

            statsQueue.Enqueue(new AudioStats(length,volumeSum,0));
        }
    }
}