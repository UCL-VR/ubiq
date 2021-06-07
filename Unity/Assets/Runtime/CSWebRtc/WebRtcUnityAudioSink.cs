using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using SIPSorceryMedia.Abstractions;
using UnityEngine;

namespace Ubiq.CsWebRtc
{
    public class WebRtcUnityAudioSink : MonoBehaviour, IAudioSink
    {
        // IAudioSink implementation starts
        // Thread safe and can be called before Awake() and after OnDestroy()
        public event SourceErrorDelegate OnAudioSinkError;
        public List<AudioFormat> GetAudioSinkFormats() => audioFormatManager?.GetSourceFormats();
        public void SetAudioSinkFormat(AudioFormat audioFormat) => audioFormatManager?.SetSelectedFormat(audioFormat);
        public void RestrictFormats(Func<AudioFormat, bool> filter) => audioFormatManager?.RestrictFormats(filter);
        public Task StartAudioSink() => QueueTaskOnMainThread(GetStartAudioTask);
        public Task PauseAudioSink() => QueueTaskOnMainThread(GetPauseAudioTask);
        public Task ResumeAudioSink() => QueueTaskOnMainThread(GetResumeAudioTask);
        public Task CloseAudioSink() => QueueTaskOnMainThread(GetCloseAudioTask);
        public void GotAudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload) => rtps?.Enqueue(new AudioRtp(remoteEndPoint,ssrc,seqnum,timestamp,payloadID,marker,payload));
        // IAudioSink implementation ends

        // TODO SIPSorcery is internally allocating large byte arrays for
        // payloads send to GotAudioRtp. Will need to work this out at some
        // point in the future how to fix this, as we'll have huge runtime
        // allocations for the received payload byte arrays. Seems likely
        // we'll need to maintain a no-runtime-alloc fork of SIPSorcery :(
        private class AudioRtp
        {
            public readonly IPEndPoint remoteEndPoint;
            public readonly uint ssrc;
            public readonly uint seqnum;
            public readonly uint timestamp;
            public readonly int payloadID;
            public readonly bool marker;
            public readonly byte[] payload;

            public AudioRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum,
                uint timestamp, int payloadID, bool marker, byte[] payload)
            {
                this.remoteEndPoint = remoteEndPoint;
                this.ssrc = ssrc;
                this.seqnum = seqnum;
                this.timestamp = timestamp;
                this.payloadID = payloadID;
                this.marker = marker;
                this.payload = payload;
            }

            public override string ToString()
            {
                return "remoteEndPoint: " + remoteEndPoint
                    + " ssrc: " + ssrc
                    + " seqnum: " + seqnum
                    + " timestamp: " + timestamp
                    + " payloadID: " + payloadID
                    + " marker: " + marker
                    + " payloadLen " + payload.Length;
            }
        }

        public float gain = 1.0f;

        // debug
        public event Action<float> OnVolumeChange;

        private G722AudioDecoder audioDecoder = new G722AudioDecoder();
        private MediaFormatManager<AudioFormat> audioFormatManager = new MediaFormatManager<AudioFormat>(
            new List<AudioFormat>
            {
                new AudioFormat(SDPWellKnownMediaFormatsEnum.G722)
            }
        );

        private ConcurrentQueue<AudioRtp> rtps = new ConcurrentQueue<AudioRtp>();
        private Queue<Task> mainThreadTasks = new Queue<Task>();
        private TaskCompletionSource<bool> allTaskTcs = new TaskCompletionSource<bool>();
        private readonly object taskLock = new object();

        private const int SAMPLE_RATE = 16000;
        private const int BUFFER_SAMPLES = SAMPLE_RATE * 10;
        private const int LATENCY_SAMPLES = SAMPLE_RATE / 5;

        public AudioSource unityAudioSource { get; private set; }

        private int advancedSamples = -1;

        private void Awake()
        {
            unityAudioSource = gameObject.AddComponent<AudioSource>();
            unityAudioSource.clip = AudioClip.Create(
                name: "WebRTC AudioClip",
                lengthSamples: BUFFER_SAMPLES,
                channels: 1,
                frequency: SAMPLE_RATE,
                stream: false);
            unityAudioSource.loop = true;
            unityAudioSource.Play();
        }

        private void OnDestroy()
        {
            // Mark all tasks completed when this MonoBehaviour is destroyed
            allTaskTcs.TrySetResult(true);

            lock (taskLock)
            {
                mainThreadTasks = null;
            }

            rtps = null;
            // microphoneListener.End();
        }

        private void Update()
        {
            Debug.Log("u: " + unityAudioSource.timeSamples + " a: " + advancedSamples + " m: " + advancedSamples % unityAudioSource.clip.samples);
            // Run queued tasks synchronously
            while (true)
            {
                var task = null as Task;
                lock (taskLock)
                {
                    if (mainThreadTasks.Count == 0)
                    {
                        break;
                    }

                    task = mainThreadTasks.Dequeue();
                }

                task.RunSynchronously();
            }

            while (rtps.TryDequeue(out var rtp))
            {
                // TODO pool buffers to avoid runtime GC
                var pcm = audioDecoder.Decode(rtp.payload);
                var floatPcm = new float[pcm.Length];

                var sum = 0.0f;
                for (int i = 0; i < pcm.Length; i++)
                {
                    var floatSample = PcmToFloat(pcm[i]);
                    floatPcm[i] = Mathf.Clamp(floatSample*gain,-.999f,.999f);
                    sum += Mathf.Abs(floatPcm[i]);
                }
                var avg = sum / pcm.Length;
                OnVolumeChange?.Invoke(avg);

                if (advancedSamples < 0)
                {
                    advancedSamples = unityAudioSource.timeSamples + LATENCY_SAMPLES;
                }

                unityAudioSource.clip.SetData(floatPcm,advancedSamples % unityAudioSource.clip.samples);
                advancedSamples += pcm.Length;
            }
        }

        private float PcmToFloat (short pcm)
        {
            return ((float)pcm) / short.MaxValue;
        }

        // Thread-safe task queue - tasks eventually get executed synchronously on
        // the main thread in Update.
        private Task QueueTaskOnMainThread (Func<Task> taskGetter)
        {
            lock (taskLock)
            {
                if (allTaskTcs.Task.IsCompleted)
                {
                    return Task.CompletedTask;
                }
                else
                {
                    var task = taskGetter();
                    mainThreadTasks.Enqueue(task);

                    // Queue is discarded when object is destroyed, so this task
                    // may never be run. Returning a WhenAny means those awaiting
                    // these tasks won't wait forever, as the all-task is completed
                    return Task.WhenAny(allTaskTcs.Task,task);
                }
            }
        }

        private Task GetStartAudioTask()
        {
            return new Task(() => { });
        }
        private Task GetResumeAudioTask()
        {
            return new Task(() => { });
        }
        private Task GetPauseAudioTask()
        {
            return new Task(() => { });
        }
        private Task GetCloseAudioTask()
        {
            return new Task(() => { });
        }
    }
}