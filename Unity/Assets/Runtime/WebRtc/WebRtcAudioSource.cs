using Pixiv.Webrtc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace Ubiq.WebRtc
{
    /// <summary>
    /// Acts as an Audio Source in Unity (i.e. an audio reciever for WebRTC) (see also, WebRTCAudioListener)
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class WebRtcAudioSource : MonoBehaviour, IAudioTrackSinkInterface, IWebRtcSink
    {
        private RingBufferAudioTrackSink sink;
        private short[] source;
        private int systemSampleRate;

        public string id;

        public IntPtr Ptr => sink.Ptr;
        public string StreamID => id;

        WebRtcPeerConnection pc;

        private void Awake()
        {
            pc = GetComponentInParent<WebRtcPeerConnection>();
        }

        private void Start()
        {
            sink = new RingBufferAudioTrackSink(100000);
            source = new short[0];
            systemSampleRate = AudioSettings.outputSampleRate;
            pc.AddSink(this);
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (sink.Available > 0)
            {
                var info = sink.Info;

                if (info.bits_per_sample != 16)
                {
                    Debug.LogError("Unsupported Audio Sample Resolution " + info.bits_per_sample.ToString());
                }

                if (info.sample_rate != systemSampleRate)
                {
                    Debug.LogError("Unsupported Audio Sample Frequency " + info.sample_rate.ToString());
                }

                var bytes_per_sample = (info.bits_per_sample / 8);
                var samples_per_ms = (info.sample_rate / 1000) * info.number_of_channels;
                var bytes_per_ms = samples_per_ms * bytes_per_sample;

                var ms_available = sink.Available / bytes_per_ms;
                var ms_to_read = Math.Ceiling((data.Length / channels) / (systemSampleRate / 1000f));
                if (ms_available < ms_to_read)
                {
                    return; // need to buffer more data before we can feed the unity dsp
                }

                var slices_to_read = data.Length / channels;
                int array_size = slices_to_read * info.number_of_channels;
                if (source.Length < array_size)
                {
                    Array.Resize(ref source, array_size);
                }
                Marshal.Copy(sink.Data, source, 0, array_size);
                var element_size = sizeof(short);
                sink.Advance(array_size * element_size);

                for (int i = 0; i < slices_to_read; i++)
                {
                    for (int destination_channel = 0; destination_channel < channels; destination_channel++)
                    {
                        var source_channel = destination_channel % info.number_of_channels;
                        data[(i * channels) + destination_channel] = Map(source[(i * info.number_of_channels) + source_channel]);
                    }
                }
            }
        }

        private static float Map(short value)
        {
            return ((float)value) / (float)short.MaxValue;
        }
    }
}