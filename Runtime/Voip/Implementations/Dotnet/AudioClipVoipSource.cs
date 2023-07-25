using System.Collections.Generic;
using UnityEngine;
using SIPSorceryMedia.Abstractions;
using System.Threading.Tasks;
using System;

namespace Ubiq.Voip.Implementations.Dotnet
{
    /// <summary>
    /// Plays back an Audio clip as an Audio Source
    /// </summary>
    public class AudioClipVoipSource : MonoBehaviour, IVoipSource
    {
        public AudioClip Clip;
        public bool IsPaused;

        private float gain = 1f;

        private G722AudioEncoder audioEncoder;
        private MediaFormatManager<AudioFormat> audioFormatManager;
        private float time;

        private void Awake()
        {
            audioFormatManager = new MediaFormatManager<AudioFormat>(
                new List<AudioFormat>
                {
                    new AudioFormat(SDPWellKnownMediaFormatsEnum.G722)
                }
            );
            audioEncoder = new G722AudioEncoder();
            IsPaused = true;
            time = 0;
        }

        // Start is called before the first frame update
        void Start()
        {
            StartAudio().Start();
        }

        public event Action<AudioStats> statsPushed;
        public event EncodedSampleDelegate OnAudioSourceEncodedSample;
        public event RawAudioSampleDelegate OnAudioSourceRawSample;
#pragma warning disable 67
        public event SourceErrorDelegate OnAudioSourceError;
#pragma warning restore 67

        public Task CloseAudio()
        {
            return Task.CompletedTask;
        }

        public void ExternalAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds, short[] sample)
        {
            if (OnAudioSourceRawSample != null)
            {
                OnAudioSourceRawSample(samplingRate, durationMilliseconds, sample);
            }
        }

        public List<AudioFormat> GetAudioSourceFormats()
        {
            return audioFormatManager.GetSourceFormats();
        }

        public bool HasEncodedAudioSubscribers()
        {
            return OnAudioSourceEncodedSample != null;
        }

        public bool IsAudioSourcePaused()
        {
            return IsPaused;
        }

        public Task PauseAudio()
        {
            return new Task(() => IsPaused = true);
        }

        public Task ResumeAudio()
        {
            return new Task(() => IsPaused = false);
        }

        public void RestrictFormats(Func<AudioFormat, bool> filter)
        {
            audioFormatManager.RestrictFormats(filter);
        }

        public void SetAudioSourceFormat(AudioFormat audioFormat)
        {
            audioFormatManager.SetSelectedFormat(audioFormat);
        }

        public Task StartAudio()
        {
            return ResumeAudio();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (!IsPaused)
            {
                // The G722 codec is expecting 16 KHz
                var numPcmSamples = (int)(Time.fixedDeltaTime * 16000);
                if(numPcmSamples % 2 > 0)   // The codec expects an even number of samples
                {
                    numPcmSamples--;
                }
                var pcmSamples = new short[numPcmSamples];

                var numClipSamples = (int)(Time.fixedDeltaTime * Clip.frequency * Clip.channels);
                if (numClipSamples % 2 > 0) // De-interleaving expects an even number of samples
                {
                    numClipSamples--;
                }
                var clipSamples = new float[numClipSamples];

                // The offset is given in samples per-channel

                Clip.GetData(clipSamples, (int)(Mathf.Repeat(time, Clip.length) * Clip.frequency));

                if (Clip.channels > 1)
                {

                    var channelSamples = new float[clipSamples.Length / Clip.channels];
                    for (int i = 0; i < channelSamples.Length; i++)
                    {
                        channelSamples[i] = clipSamples[i * Clip.channels];
                    }
                    clipSamples = channelSamples;
                }

                // This ratio re-samples the clip from one samples per second count to other

                var samplesRatio = (Clip.frequency / 16000f);
                var volumeSum = 0.0f;

                for (int i = 0; i < pcmSamples.Length; i++)
                {
                    var sampleIndex = (int)(i * samplesRatio);
                    var clipSample = clipSamples[sampleIndex];
                    clipSample = Mathf.Clamp(clipSample * gain, -.999f, .999f);
                    volumeSum += clipSample;
                    pcmSamples[i] = (short)(clipSample * short.MaxValue);
                }

                var encoded = audioEncoder.Encode(pcmSamples);
                if (HasEncodedAudioSubscribers())
                {
                    // G722 has a sample rate of 16000 but a clock rate of 8000
                    var duration = pcmSamples.Length/2;
                    OnAudioSourceEncodedSample((uint)duration, encoded);
                }

                statsPushed?.Invoke(new AudioStats
                (
                    sampleCount:clipSamples.Length,
                    volumeSum:volumeSum,
                    sampleRate:16000
                ));

                time += Time.fixedDeltaTime;
            }
        }
    }
}