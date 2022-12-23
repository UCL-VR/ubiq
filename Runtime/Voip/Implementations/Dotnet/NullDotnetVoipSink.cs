using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using Ubiq.Voip.Implementations;

namespace Ubiq.Voip.Implementations.Dotnet
{
    /// <summary>
    /// An Audio Sink that drops all audio data.
    /// </summary>
    public class NullDotnetVoipSink : MonoBehaviour, IDotnetVoipSink
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

        public PlaybackStats GetLastFramePlaybackStats()
        {
            return new PlaybackStats();
        }

        public void UpdateSpatialization(Vector3 sourcePosition, Quaternion sourceRotation, Vector3 listenerPosition, Quaternion listenerRotation)
        {
        }
    }
}