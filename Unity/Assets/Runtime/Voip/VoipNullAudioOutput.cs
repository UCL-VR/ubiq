using SIPSorceryMedia.Abstractions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

namespace Ubiq.Voip
{
    /// <summary>
    /// An Audio Sink that drops all audio data.
    /// </summary>
    public class VoipNullAudioOutput : MonoBehaviour, IAudioSink, IAudioStats
    {
#pragma warning disable 67
        public event SourceErrorDelegate OnAudioSinkError;
#pragma warning restore 67

        private MediaFormatManager<AudioFormat> audioFormatManager = new MediaFormatManager<AudioFormat>(
            new List<AudioFormat>
            {
                new AudioFormat(SDPWellKnownMediaFormatsEnum.G722)
            }
        );

        private Stats stats;
        public Stats lastFrameStats => stats;

        public Task CloseAudioSink()
        {
            return Task.CompletedTask;
        }

        public List<AudioFormat> GetAudioSinkFormats()
        {
            return audioFormatManager.GetSourceFormats();
        }

        public Task PauseAudioSink()
        {
            return Task.CompletedTask;
        }

        public void RestrictFormats(Func<AudioFormat, bool> filter)
        {
            audioFormatManager.RestrictFormats(filter);
        }

        public Task ResumeAudioSink()
        {
            return Task.CompletedTask;
        }

        public void SetAudioSinkFormat(AudioFormat audioFormat)
        {
            audioFormatManager.SetSelectedFormat(audioFormat);
        }

        public Task StartAudioSink()
        {
            return Task.CompletedTask;
        }

        public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload)
        {
        }
    }
}