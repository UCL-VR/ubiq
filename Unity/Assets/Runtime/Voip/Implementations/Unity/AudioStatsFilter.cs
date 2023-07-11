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

        private volatile int channels;

        void OnDestroy()
        {
            statsPushed = null;
        }

        void OnEnable()
        {
            OnAudioConfigurationChanged(false);
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
        }

        void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        }

        void OnAudioConfigurationChanged(bool deviceWasChanged)
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

            Debug.Log(name + " " + channels);
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
        void OnAudioFilterRead(float[] data, int channels)
        {
            this.channels = channels;

            var volumeSum = 0.0f;
            for (int i = 0; i < data.Length; i+=channels)
            {
                volumeSum += Mathf.Abs(data[i]);
            }

            statsQueue.Enqueue(new AudioStats(data.Length/channels,volumeSum,0));
        }
    }
}