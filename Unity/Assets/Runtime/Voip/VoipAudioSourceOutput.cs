using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using SIPSorceryMedia.Abstractions;
using UnityEngine;

namespace Ubiq.Voip
{
    public class VoipAudioSourceOutput : MonoBehaviour, IAudioSink
    {
        // IAudioSink implementation starts
        // Thread safe and can be called before Awake() and after OnDestroy()
        public event SourceErrorDelegate OnAudioSinkError = delegate {};
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

        // Jitter buffer implementation
        // Recovers from desync and resets latency after desync
        // Known issues:
        // Does not reduce latency unless missing buffer (could speedup playback)
        // Does not smooth in/out after desync - may 'click'
        // Does not dynamically adjust latency based on round trip time
        // Fixed bitrate of 64kbps, relatively high
        private class RtpBufferer
        {
            public G722AudioDecoder decoder { get; private set; }
            public int latencySamples { get; private set; }
            public int syncSamples { get; private set; }
            public float gain;

            private int absTimeSamples = -1;
            private int lastTimeSamples;
            private int missedRtps = RESYNC_AFTER_RTP_MISS_NUM;
            private long bufferOffset = -1;

            private System.Diagnostics.Stopwatch stopwatch;
            private List<AudioRtp> rtps = new List<AudioRtp>();

            private const int RESYNC_AFTER_RTP_MISS_NUM = 50;

            public RtpBufferer (int latencySamples, int syncSamples,
                float gain = 1.0f)
            {
                this.latencySamples = latencySamples;
                this.syncSamples = syncSamples;
                this.gain = gain;

                decoder = new G722AudioDecoder();

                stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
            }

            public Stats Commit (int currentTimeSamples, AudioClip audioClip)
            {
                var stats = Advance(currentTimeSamples, audioClip);
                Flush(audioClip);
                return stats;
            }

            private void Flush (AudioClip audioClip)
            {
                if (rtps.Count == 0)
                {
                    return;
                }

                // First scan through to check sync
                for (int i = 0; i < rtps.Count; i++)
                {
                    if (missedRtps >= RESYNC_AFTER_RTP_MISS_NUM)
                    {
                        break;
                    }

                    var rtp = rtps[i];
                    var bufferPos = rtp.timestamp + bufferOffset;
                    // When decoded: 1 byte -> 2 pcm
                    var bufferPosEnd = bufferPos + rtp.payload.Length*2;

                    // If this rtp falls outside the in-sync range, note it down
                    // Might still be played if it falls in the buffer range
                    if (bufferPos < absTimeSamples ||
                        bufferPosEnd > absTimeSamples + syncSamples)
                    {
                        missedRtps++;
                    }
                    else
                    {
                        missedRtps = 0;
                    }
                }

                // Dropped a large number of consecutive packets
                // We're probably out of sync - try resync with latest rtp
                if (missedRtps >= RESYNC_AFTER_RTP_MISS_NUM)
                {
                    var mostRecentRtp = rtps[rtps.Count-1];
                    bufferOffset = latencySamples + absTimeSamples - mostRecentRtp.timestamp;
                    missedRtps = 0;

                    rtps.Clear();
                    rtps.Add(mostRecentRtp);
                }

                // Finally, queue up rtps into audioclip
                for (int rtpi = 0; rtpi < rtps.Count; rtpi++)
                {
                    var rtp = rtps[rtpi];
                    var bufferPos = rtp.timestamp + bufferOffset;
                    // When decoded: 1 byte -> 2 pcm
                    var bufferPosEnd = bufferPos + rtp.payload.Length*2;

                    // If this rtp falls outside the total buffer range, discard
                    if (bufferPos < absTimeSamples ||
                        bufferPosEnd > absTimeSamples + audioClip.samples)
                    {
                        continue;
                    }

                    // TODO pool buffers to avoid runtime GC
                    var pcms = decoder.Decode(rtp.payload);
                    var floatPcms = new float[pcms.Length];

                    for (int pcmi = 0; pcmi < pcms.Length; pcmi++)
                    {
                        var floatSample = ((float)pcms[pcmi]) / short.MaxValue;
                        floatPcms[pcmi] = Mathf.Clamp(floatSample*gain,-.999f,.999f);
                    }

                    audioClip.SetData(floatPcms,(int)(bufferPos % audioClip.samples));
                }

                rtps.Clear();
            }

            // Step internal time trackers forward and zero out just-read samples
            private Stats Advance (int timeSamples, AudioClip audioClip)
            {
                if (absTimeSamples < 0)
                {
                    absTimeSamples = timeSamples;
                    lastTimeSamples = timeSamples;
                }
                else
                {
                    var deltaTimeSamples = timeSamples - lastTimeSamples;
                    if (deltaTimeSamples < 0)
                    {
                        deltaTimeSamples += audioClip.samples;
                    }

                    // Zero out so we don't repeat audio on connection drop
                    // Note audio will still repeat if frames do not keep up
                    // TODO pool buffers to avoid runtime GC
                    var volume = 0.0f;
                    if (deltaTimeSamples > 0)
                    {
                        var floatPcms = new float[deltaTimeSamples];

                        // Gather volume for this set of stats
                        audioClip.GetData(floatPcms,lastTimeSamples);
                        for (int i = 0; i < floatPcms.Length; i++)
                        {
                            volume += Mathf.Abs(floatPcms[i]);
                            floatPcms[i] = 0;
                        }

                        // Zero out
                        audioClip.SetData(floatPcms,lastTimeSamples);
                    }

                    // Update time trackers
                    absTimeSamples += deltaTimeSamples;
                    lastTimeSamples = timeSamples;

                    // Calculate stats for the advance
                    return new Stats { volume=volume, samples=deltaTimeSamples};
                }

                return new Stats { volume=0, samples=0};
            }

            public void AddRtp (AudioRtp rtp)
            {
                rtps.Add(rtp);
            }
        }

        public struct Stats
        {
            public float volume;
            public int samples;
        }

        public int sampleRate { get { return 16000; } }
        public int bufferSamples { get { return sampleRate * 2; } }
        public int latencySamples { get { return sampleRate / 5; } }
        public int syncSamples { get { return sampleRate * 1; } }

        public float gain = 1.0f;
        public AudioSource unityAudioSource { get; private set; }
        public Stats lastFrameStats { get; private set; }

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

        private RtpBufferer rtpBufferer;

        private void Awake()
        {
            unityAudioSource = gameObject.AddComponent<AudioSource>();
            unityAudioSource.clip = AudioClip.Create(
                name: "WebRTC AudioClip",
                lengthSamples: bufferSamples,
                channels: 1,
                frequency: sampleRate,
                stream: false);
            unityAudioSource.loop = true;
            unityAudioSource.ignoreListenerPause = true;
            unityAudioSource.Play();

            rtpBufferer = new RtpBufferer(latencySamples,syncSamples);
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
        }

        private void Update()
        {
            // Run queued tasks in main thread
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
                rtpBufferer.AddRtp(rtp);
            }

            var timeSamples = unityAudioSource.timeSamples;
            var clip = unityAudioSource.clip;
            lastFrameStats = rtpBufferer.Commit(timeSamples,clip);
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