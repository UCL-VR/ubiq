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

        // V1 - handles overflow only
        // private class RtpBufferer //JitterBuffer
        // {
        //     public AudioClip audioClip { get; private set; }
        //     public G722AudioDecoder decoder { get; private set; }

        //     public float gain;
        //     private int latencySamples;

        //     private int absTimeSamples = -1;

        //     private long initTimestamp = long.MinValue;
        //     private int initTimeSamples = -1;
        //     private int timeSamplesOffset;

        //     private int lastTimeSamples;

        //     private System.Diagnostics.Stopwatch stopwatch;

        //     public RtpBufferer (AudioClip audioClip, int latencySamples,
        //         float gain = 1.0f)
        //     {
        //         this.audioClip = audioClip;
        //         this.gain = gain;
        //         this.latencySamples = latencySamples;

        //         decoder = new G722AudioDecoder();

        //         stopwatch = new System.Diagnostics.Stopwatch();
        //         stopwatch.Start();
        //     }

        //     public void SetTimeSamples (int timeSamples)
        //     {
        //         if (absTimeSamples < 0)
        //         {
        //             absTimeSamples = lastTimeSamples = timeSamples;
        //         }
        //         else
        //         {
        //             var deltaTimeSamples = timeSamples - lastTimeSamples;
        //             if (deltaTimeSamples < 0)
        //             {
        //                 deltaTimeSamples += audioClip.samples;
        //             }
        //             Debug.Log("dts: " + deltaTimeSamples + " ts: " + timeSamples + " ats: " + absTimeSamples);
        //             absTimeSamples += deltaTimeSamples;
        //             lastTimeSamples = timeSamples;
        //         }
        //     }

        //     public void AddRtp (AudioRtp rtp)
        //     {
        //         if (initTimestamp == long.MinValue)
        //         {
        //             initTimestamp = (long)rtp.timestamp;
        //             initTimeSamples = absTimeSamples;
        //             // timeSamplesOffset = (timeSamples + latencySamples) - (int)rtp.timestamp;
        //         }

        //         var recvSamples = rtp.timestamp - initTimestamp;
        //         var bufferPos = recvSamples + initTimeSamples + latencySamples;

        //         Debug.Log("buffpos: " + bufferPos + " timeSamples: " + absTimeSamples);

        //         // var recvSamples = (rtp.timestamp - initTimestamp) + latencySamples;
        //         // var recvTimeSamples = rtp.timestamp - timeSamplesOffset;

        //         if (bufferPos < absTimeSamples)
        //         {
        //             // Drop sample - too early
        //             return;
        //         }

        //         // decoded - 1 byte -> 2 pcm
        //         if (bufferPos + rtp.payload.Length*2 > absTimeSamples + audioClip.samples)
        //         {
        //             Debug.Log(stopwatch.ElapsedMilliseconds + " adjusting...");
        //             // Overfilled buffer - probably a desync issue
        //             // Try to salvage by making this our new start point?
        //             initTimestamp = (long) rtp.timestamp + initTimeSamples - absTimeSamples;
        //             AddRtp(rtp);
        //             return;
        //             // recvTimeSamples = (rtp.timestamp - initTimestamp) + latencySamples;
        //         }

        //         // TODO pool buffers to avoid runtime GC
        //         var pcm = decoder.Decode(rtp.payload);
        //         var floatPcm = new float[pcm.Length];

        //         for (int i = 0; i < pcm.Length; i++)
        //         {
        //             var floatSample = ((float)pcm[i]) / short.MaxValue;
        //             floatPcm[i] = Mathf.Clamp(floatSample*gain,-.999f,.999f);
        //         }

        //         // if (advancedSamples < 0)
        //         // {
        //         //     advancedSamples = unityAudioSource.timeSamples + LATENCY_SAMPLES;
        //         // }

        //         audioClip.SetData(floatPcm,(int)(bufferPos % audioClip.samples));

        //         // unityAudioSource.clip.SetData(floatPcm,advancedSamples % unityAudioSource.clip.samples);
        //         // advancedSamples += pcm.Length;
        //     }
        // }

        // // V2 - handles desync
        // private class RtpBufferer
        // {

        //     public AudioClip audioClip { get; private set; }
        //     public G722AudioDecoder decoder { get; private set; }

        //     public float gain;
        //     private int latencySamples;

        //     private int absTimeSamples = -1;

        //     private long initTimestamp = -1;
        //     private int initTimeSamples = -1;
        //     private int timeSamplesOffset;

        //     private int lastTimeSamples;

        //     private int droppedRtps = RESYNC_AFTER_DROPPED_RTP_NUM;

        //     private System.Diagnostics.Stopwatch stopwatch;

        //     private const int RESYNC_AFTER_DROPPED_RTP_NUM = 30;

        //     public RtpBufferer (AudioClip audioClip, int latencySamples,
        //         float gain = 1.0f)
        //     {
        //         this.audioClip = audioClip;
        //         this.gain = gain;
        //         this.latencySamples = latencySamples;

        //         decoder = new G722AudioDecoder();

        //         stopwatch = new System.Diagnostics.Stopwatch();
        //         stopwatch.Start();
        //     }

        //     public void SetTimeSamples (int timeSamples)
        //     {
        //         if (absTimeSamples < 0)
        //         {
        //             absTimeSamples = lastTimeSamples = timeSamples;
        //         }
        //         else
        //         {
        //             var deltaTimeSamples = timeSamples - lastTimeSamples;
        //             if (deltaTimeSamples < 0)
        //             {
        //                 deltaTimeSamples += audioClip.samples;
        //             }
        //             Debug.Log("dts: " + deltaTimeSamples + " ts: " + timeSamples + " ats: " + absTimeSamples);
        //             absTimeSamples += deltaTimeSamples;
        //             lastTimeSamples = timeSamples;
        //         }
        //     }

        //     public void AddRtp (AudioRtp rtp)
        //     {
        //         if (droppedRtps >= RESYNC_AFTER_DROPPED_RTP_NUM)
        //         {
        //             initTimestamp = (long)rtp.timestamp;
        //             initTimeSamples = absTimeSamples;

        //             droppedRtps = 0;
        //             Debug.Log(stopwatch.ElapsedMilliseconds + " adjusting...");
        //             // timeSamplesOffset = (timeSamples + latencySamples) - (int)rtp.timestamp;
        //         }

        //         var recvSamples = rtp.timestamp - initTimestamp;
        //         var bufferPos = recvSamples + initTimeSamples + latencySamples;

        //         // When decoded: 1 byte -> 2 pcm
        //         var bufferPosEnd = bufferPos + rtp.payload.Length*2;

        //         Debug.Log("buffpos: " + bufferPos + " timeSamples: " + absTimeSamples);

        //         // var recvSamples = (rtp.timestamp - initTimestamp) + latencySamples;
        //         // var recvTimeSamples = rtp.timestamp - timeSamplesOffset;

        //         if (bufferPos < absTimeSamples ||
        //             bufferPosEnd > absTimeSamples + audioClip.samples)
        //         {
        //             droppedRtps++;
        //             return;
        //         }

        //         droppedRtps = 0;

        //         // // decoded - 1 byte -> 2 pcm
        //         // if (bufferPos + rtp.payload.Length*2 > absTimeSamples + audioClip.samples)
        //         // {
        //         //     Debug.Log(stopwatch.ElapsedMilliseconds + " adjusting...");
        //         //     // Overfilled buffer - probably a desync issue
        //         //     // Try to salvage by making this our new start point?
        //         //     initTimestamp = long.MinValue;// (long) rtp.timestamp + initTimeSamples - absTimeSamples;
        //         //     AddRtp(rtp);
        //         //     return;
        //         //     // recvTimeSamples = (rtp.timestamp - initTimestamp) + latencySamples;
        //         // }

        //         // TODO pool buffers to avoid runtime GC
        //         var pcm = decoder.Decode(rtp.payload);
        //         var floatPcm = new float[pcm.Length];

        //         for (int i = 0; i < pcm.Length; i++)
        //         {
        //             var floatSample = ((float)pcm[i]) / short.MaxValue;
        //             floatPcm[i] = Mathf.Clamp(floatSample*gain,-.999f,.999f);
        //         }

        //         // if (advancedSamples < 0)
        //         // {
        //         //     advancedSamples = unityAudioSource.timeSamples + LATENCY_SAMPLES;
        //         // }

        //         audioClip.SetData(floatPcm,(int)(bufferPos % audioClip.samples));

        //         // unityAudioSource.clip.SetData(floatPcm,advancedSamples % unityAudioSource.clip.samples);
        //         // advancedSamples += pcm.Length;
        //     }
        // }

        // V3 - Recovers from desync and resets latency after desync
        // Known issues:
        // Does not reduce latency unless missing buffer (could speedup playback)
        // Does not smooth in/out after desync - may 'click'
        // Does not dynamically adjust latency based on round trip time
        // Fixed bitrate of 64kbps, relatively high
        private class RtpBufferer
        {
            public G722AudioDecoder decoder { get; private set; }
            public float gain;

            private int latencySamples;
            private int absTimeSamples = -1;
            private int lastTimeSamples;
            private int droppedRtps = RESYNC_AFTER_DROPPED_RTP_NUM;
            private long bufferOffset = -1;

            private System.Diagnostics.Stopwatch stopwatch;
            private List<AudioRtp> rtps = new List<AudioRtp>();

            private const int RESYNC_AFTER_DROPPED_RTP_NUM = 50;

            public RtpBufferer (int latencySamples, float gain = 1.0f)
            {
                this.latencySamples = latencySamples;
                this.gain = gain;

                decoder = new G722AudioDecoder();

                stopwatch = new System.Diagnostics.Stopwatch();
                stopwatch.Start();
            }

            public void Commit (int currentTimeSamples, AudioClip audioClip)
            {
                UpdateTimeSamples(currentTimeSamples, audioClip.samples);

                if (rtps.Count == 0)
                {
                    return;
                }

                // First scan through to check sync
                for (int i = 0; i < rtps.Count; i++)
                {
                    if (droppedRtps >= RESYNC_AFTER_DROPPED_RTP_NUM)
                    {
                        break;
                    }

                    var rtp = rtps[i];
                    var bufferPos = rtp.timestamp + bufferOffset;
                    // When decoded: 1 byte -> 2 pcm
                    var bufferPosEnd = bufferPos + rtp.payload.Length*2;

                    if (bufferPos < absTimeSamples ||
                        bufferPosEnd > absTimeSamples + audioClip.samples)
                    {
                        droppedRtps++;
                    }
                    else
                    {
                        droppedRtps = 0;
                    }
                }

                // Dropped a large number of consecutive packets
                // We're probably out of sync - try resync with latest rtp
                if (droppedRtps >= RESYNC_AFTER_DROPPED_RTP_NUM)
                {
                    var mostRecentRtp = rtps[rtps.Count-1];
                    bufferOffset = latencySamples + absTimeSamples - mostRecentRtp.timestamp;
                    droppedRtps = 0;

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

                    Debug.Log("buffpos: " + bufferPos + " timeSamples: " + absTimeSamples);

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

            private void UpdateTimeSamples (int timeSamples, int maxTimeSamples)
            {
                if (absTimeSamples < 0)
                {
                    absTimeSamples = lastTimeSamples = timeSamples;
                }
                else
                {
                    var deltaTimeSamples = timeSamples - lastTimeSamples;
                    if (deltaTimeSamples < 0)
                    {
                        deltaTimeSamples += maxTimeSamples;
                    }
                    Debug.Log("dts: " + deltaTimeSamples + " ts: " + timeSamples + " ats: " + absTimeSamples);
                    absTimeSamples += deltaTimeSamples;
                    lastTimeSamples = timeSamples;
                }
            }

            public void AddRtp (AudioRtp rtp)
            {
                rtps.Add(rtp);
            }
        }

        // private class RtpBufferer
        // {
        //     public AudioClip audioClip { get; private set; }
        //     public G722AudioDecoder decoder { get; private set; }

        //     public float gain;
        //     private int latencySamples;

        //     private int absTimeSamples = -1;

        //     private long initTimestamp = long.MinValue;
        //     private int initTimeSamples = -1;
        //     private int timeSamplesOffset;

        //     private int lastTimeSamples;

        //     private List<AudioRtp> rtps = new List<AudioRtp>();

        //     private System.Diagnostics.Stopwatch stopwatch;

        //     public RtpBufferer (AudioClip audioClip, int latencySamples,
        //         float gain = 1.0f)
        //     {
        //         this.audioClip = audioClip;
        //         this.gain = gain;
        //         this.latencySamples = latencySamples;

        //         decoder = new G722AudioDecoder();

        //         stopwatch = new System.Diagnostics.Stopwatch();
        //         stopwatch.Start();
        //     }

        //     public void SetTimeSamples (int timeSamples)
        //     {
        //         if (absTimeSamples < 0)
        //         {
        //             absTimeSamples = lastTimeSamples = timeSamples;
        //         }
        //         else
        //         {
        //             var deltaTimeSamples = timeSamples - lastTimeSamples;
        //             if (deltaTimeSamples < 0)
        //             {
        //                 deltaTimeSamples += audioClip.samples;
        //             }
        //             Debug.Log("dts: " + deltaTimeSamples + " ts: " + timeSamples + " ats: " + absTimeSamples);
        //             absTimeSamples += deltaTimeSamples;
        //             lastTimeSamples = timeSamples;
        //         }
        //     }

        //     public void AddRtp (AudioRtp rtp)
        //     {
        //         if (initTimestamp == long.MinValue)
        //         {
        //             initTimestamp = (long)rtp.timestamp;
        //             initTimeSamples = absTimeSamples;
        //             // timeSamplesOffset = (timeSamples + latencySamples) - (int)rtp.timestamp;
        //         }

        //         var recvSamples = rtp.timestamp - initTimestamp;
        //         var bufferPos = recvSamples + initTimeSamples + latencySamples;

        //         // When decoded: 1 byte -> 2 pcm
        //         var bufferPosEnd = bufferPos + rtp.payload.Length*2;

        //         Debug.Log("buffpos: " + bufferPos + " timeSamples: " + absTimeSamples);

        //         // var recvSamples = (rtp.timestamp - initTimestamp) + latencySamples;
        //         // var recvTimeSamples = rtp.timestamp - timeSamplesOffset;

        //         if (bufferPos < absTimeSamples
        //             || bufferPosEnd > absTimeSamples + audioClip.samples)
        //         {
        //             // Sample doesn't fit in buffer - too early or too late

        //             return;
        //         }

        //         // decoded - 1 byte -> 2 pcm
        //         if (bufferPos + rtp.payload.Length*2 > absTimeSamples + audioClip.samples)
        //         {
        //             Debug.Log(stopwatch.ElapsedMilliseconds + " adjusting...");
        //             // Overfilled buffer - probably a desync issue
        //             // Try to salvage by making this our new start point?
        //             initTimestamp = (long)rtp.timestamp + initTimeSamples - absTimeSamples;
        //             AddRtp(rtp);
        //             return;
        //             // recvTimeSamples = (rtp.timestamp - initTimestamp) + latencySamples;
        //         }

        //         // TODO pool buffers to avoid runtime GC
        //         var pcm = decoder.Decode(rtp.payload);
        //         var floatPcm = new float[pcm.Length];

        //         for (int i = 0; i < pcm.Length; i++)
        //         {
        //             var floatSample = ((float)pcm[i]) / short.MaxValue;
        //             floatPcm[i] = Mathf.Clamp(floatSample*gain,-.999f,.999f);
        //         }

        //         // if (advancedSamples < 0)
        //         // {
        //         //     advancedSamples = unityAudioSource.timeSamples + LATENCY_SAMPLES;
        //         // }

        //         audioClip.SetData(floatPcm,(int)(bufferPos % audioClip.samples));

        //         // unityAudioSource.clip.SetData(floatPcm,advancedSamples % unityAudioSource.clip.samples);
        //         // advancedSamples += pcm.Length;
        //     }
        // }

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
        private const int BUFFER_SAMPLES = SAMPLE_RATE * 1;
        private const int LATENCY_SAMPLES = SAMPLE_RATE / 5;

        public AudioSource unityAudioSource { get; private set; }

        private int advancedSamples = -1;
        private RtpBufferer rtpBufferer;

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
            unityAudioSource.ignoreListenerPause = true;
            unityAudioSource.Play();

            rtpBufferer = new RtpBufferer(unityAudioSource.clip,LATENCY_SAMPLES);
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
            // Debug.Log("u: " + unityAudioSource.timeSamples + " a: " + advancedSamples + " m: " + advancedSamples % unityAudioSource.clip.samples);
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

            rtpBufferer.UpdateTimeSamples(unityAudioSource.timeSamples);

            while (rtps.TryDequeue(out var rtp))
            {
                rtpBufferer.AddRtp(rtp);
                // // TODO pool buffers to avoid runtime GC
                // var pcm = audioDecoder.Decode(rtp.payload);
                // var floatPcm = new float[pcm.Length];

                // var sum = 0.0f;
                // for (int i = 0; i < pcm.Length; i++)
                // {
                //     var floatSample = PcmToFloat(pcm[i]);
                //     floatPcm[i] = Mathf.Clamp(floatSample*gain,-.999f,.999f);
                //     sum += Mathf.Abs(floatPcm[i]);
                // }
                // var avg = sum / pcm.Length;
                // OnVolumeChange?.Invoke(avg);

                // if (advancedSamples < 0)
                // {
                //     advancedSamples = unityAudioSource.timeSamples + LATENCY_SAMPLES;
                // }

                // unityAudioSource.clip.SetData(floatPcm,advancedSamples % unityAudioSource.clip.samples);
                // advancedSamples += pcm.Length;
            }

            rtpBufferer.Commit(unityAudioSource.timeSamples,unityAudioSource.clip);
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