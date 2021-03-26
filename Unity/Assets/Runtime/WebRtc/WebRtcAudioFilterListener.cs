using Pixiv.Webrtc;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Ubiq.WebRtc
{
    /// <summary>
    /// An audio sink that listens to the DSP pipeline using the OnFilterRead member
    /// </summary>
    public class WebRtcAudioFilterListener : MonoBehaviour, IManagedAudioSourceInterface, IWebRtcSource
    {
        public MediaSourceInterface.SourceState State => MediaSourceInterface.SourceState.Live;
        public bool Remote => false;
        public string StreamID => id;

        public string id;

        private Dictionary<IntPtr, AudioTrackSinkInterface> _sinks;
        private AudioBuffer buffer;
        private int sampleRate;

        private WebRtcPeerConnection pc;

        private void Awake()
        {
            _sinks = new Dictionary<IntPtr, AudioTrackSinkInterface>();
            sampleRate = AudioSettings.outputSampleRate;
            pc = GetComponentInParent<WebRtcPeerConnection>();
        }

        private void Start()
        {
            pc.AddSource(this);
        }

        public struct AudioBuffer
        {
            public short[] destination; // the buffer provided to AudioTrackSinkInterface
            private int destinationOffset;

            public void Resize(int sampleRate, int channels)
            {
                int destinationLength = (sampleRate * channels / 1000) * 10; // OnData() expects 10ms of raw PCM data
                if (destination == null || destination.Length != destinationLength)
                {
                    destination = new short[destinationLength];
                }
            }

            /// <summary>
            /// Copies x ms of audio data into the buffer from source. x is determined by the length of the destination buffer. Returns whether the destination buffer has been filled.
            /// </summary>
            public bool Consume(float[] source, ref int offset)
            {
                int available = source.Length - offset;
                int required = destination.Length - destinationOffset;
                int length = Mathf.Min(available, required);

                for (int i = 0; i < length; i++)
                {
                    destination[destinationOffset + i] = Sample(source[offset + i]);
                }

                offset += length;
                destinationOffset += length;

                if (destinationOffset >= destination.Length)
                {
                    destinationOffset = 0;
                    return true;
                }
                else
                {
                    return false;
                }
            }

            private static short Sample(float sample)
            {
                var scaled = sample < 0 ? -sample * short.MinValue : sample * short.MaxValue;
                return (short)Mathf.Round(scaled);
            }
        }

        public void AddSink(AudioTrackSinkInterface sink)
        {
            lock (this)
            {
                _sinks.Add(sink.Ptr, sink);
            }
        }

        public void RemoveSink(AudioTrackSinkInterface sink)
        {
            lock (this)
            {
                _sinks.Remove(sink.Ptr);
            }
        }

        private void OnAudioFilterRead(float[] samples, int channels)
        {
            buffer.Resize(sampleRate, channels);

            int samplesOffset = 0;

            while (buffer.Consume(samples, ref samplesOffset)) // consume 10 ms of data and send if 10 ms was availble (if not, finish filling the buffer on the next call)
            {
                var audioData = buffer.destination;
                var handle = GCHandle.Alloc(audioData, GCHandleType.Pinned);
                try
                {
                    lock (this)
                    {
                        foreach (var sink in _sinks.Values)
                        {
                            sink.OnData( // OnData is an extension method in media_stream_interface.cs
                                handle.AddrOfPinnedObject(),
                                16,
                                sampleRate,
                                channels,
                                audioData.Length / channels);
                        }
                    }
                }
                finally
                {
                    handle.Free();
                }
            }

            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = 0f;
            }
        }

        public void RegisterObserver(ObserverInterface observer)
        {
            Debug.Log("Register Observer");
        }

        public void UnregisterObserver(ObserverInterface observer)
        {
            Debug.Log("Unregister Observer");
        }

        public void Close()
        {
        }
    }
}