using SIPSorceryMedia.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Ubiq.Voip;
using UnityEngine;

public class VoipNullAudioInput : MonoBehaviour, IAudioSource
{
    private G722AudioEncoder audioEncoder;
    private MediaFormatManager<AudioFormat> audioFormatManager;

    public event EncodedSampleDelegate OnAudioSourceEncodedSample;
    public event RawAudioSampleDelegate OnAudioSourceRawSample;
    public event SourceErrorDelegate OnAudioSourceError;

    private void Awake()
    {
        audioFormatManager = new MediaFormatManager<AudioFormat>(
            new List<AudioFormat>
            {
                new AudioFormat(SDPWellKnownMediaFormatsEnum.G722)
            }
        );
        audioEncoder = new G722AudioEncoder();
    }

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
        return false;
    }

    public bool IsAudioSourcePaused()
    {
        return true;
    }

    public Task PauseAudio()
    {
        return Task.CompletedTask;
    }

    public void RestrictFormats(Func<AudioFormat, bool> filter)
    {
    }

    public Task ResumeAudio()
    {
        return Task.CompletedTask;
    }

    public void SetAudioSourceFormat(AudioFormat audioFormat)
    {
    }

    public Task StartAudio()
    {
        return Task.CompletedTask;
    }
}
