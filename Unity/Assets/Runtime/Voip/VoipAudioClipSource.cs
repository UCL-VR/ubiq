using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SIPSorceryMedia.Abstractions;
using System.Threading.Tasks;
using System;

namespace Ubiq.Voip
{
    /// <summary>
    /// Plays back an Audio clip as an Audio Source
    /// </summary>
    public class VoipAudioClipSource : MonoBehaviour, IAudioSource
    {
        public AudioClip Clip;
        public bool IsPaused;

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

                // The codec expects an even number of samples
                if(numPcmSamples % 2 > 0)
                {
                    numPcmSamples--;
                }

                var pcmSamples = new short[numPcmSamples];

                var samplesPerSecond = Clip.frequency * Clip.channels;
                time += Time.fixedDeltaTime;

                var offset = Mathf.Repeat(time, Clip.length);
                var offsetInSamples = (int)(offset * samplesPerSecond) % Clip.samples; // in case quantisation makes the above drift a tiny bit over

                var numFloatSamples = (int)(samplesPerSecond * Time.fixedDeltaTime);
                var floatSamples = new float[numFloatSamples];

                Clip.GetData(floatSamples, offsetInSamples);

                var samplesRatio = (Clip.frequency / 16000) * Clip.channels;

                for (int i = 0; i < pcmSamples.Length; i++)
                {
                    var floatSampleIndex = (int)(i * samplesRatio);
                    var floatSample = floatSamples[floatSampleIndex];
                    floatSample = Mathf.Clamp(floatSample * 1, -.999f, .999f);
                    pcmSamples[i] = (short)(floatSample * short.MaxValue);
                }

                var encoded = audioEncoder.Encode(pcmSamples);
                if (HasEncodedAudioSubscribers())
                {
                    OnAudioSourceEncodedSample((uint)pcmSamples.Length, encoded);
                }
            }
        }
    }
}