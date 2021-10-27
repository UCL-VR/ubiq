using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SIPSorceryMedia.Abstractions;
using UnityEngine;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Ubiq.Voip
{
    // Uses the Unity Microphone API to provide audio to any number of peer connections
    // TODO Need to work out how best to "switch off" the mic when no-one else
    // is present. Could use Microphone.End(), or could just skip through
    // samples without encoding/sending. At the moment, mic is never switched
    // off. To complicate matters, C# event thread safety is an issue.
    // TODO What happens when the audio device is changed while listening?
    public class VoipMicrophoneInput : MonoBehaviour, IAudioSource
    {
        // IAudioSource implementation starts
        // Thread safe and can be called before Awake() and after OnDestroy()
        public event EncodedSampleDelegate OnAudioSourceEncodedSample = delegate {};
        public event RawAudioSampleDelegate OnAudioSourceRawSample = delegate {};
        public event SourceErrorDelegate OnAudioSourceError = delegate {};
        public bool IsAudioSourcePaused() => isPaused;
        public bool HasEncodedAudioSubscribers() => OnAudioSourceEncodedSample != null;
        public void RestrictFormats(Func<AudioFormat, bool> filter) => audioFormatManager?.RestrictFormats(filter);
        public List<AudioFormat> GetAudioSourceFormats() => audioFormatManager?.GetSourceFormats();
        public void SetAudioSourceFormat(AudioFormat audioFormat) => audioFormatManager?.SetSelectedFormat(audioFormat);
        public Task StartAudio() => QueueTaskOnMainThread(GetStartAudioTask);
        public Task PauseAudio() => QueueTaskOnMainThread(GetPauseAudioTask);
        public Task ResumeAudio() => QueueTaskOnMainThread(GetResumeAudioTask);
        public Task CloseAudio() => QueueTaskOnMainThread(GetCloseAudioTask);
        public void ExternalAudioSourceRawSample(AudioSamplingRatesEnum samplingRate, uint durationMilliseconds, short[] sample) => OnAudioSourceRawSample.Invoke(samplingRate,durationMilliseconds,sample);
        // IAudioSource implementation ends

        private class MicrophoneListener
        {
            public string deviceName { get; private set; }
            public AudioClip audioClip { get; private set; }
            public float gain;

            private int absReadPos;
            private int absMicPos;
            private int lastMicPos;

            public float[] samples { get; private set; }

            public bool IsRecording () => audioClip != null;

            private void UpdateMicrophonePosition ()
            {
                if (!IsRecording())
                {
                    return;
                }

                var micPos = Microphone.GetPosition(deviceName);
                var deltaMicPos = micPos - lastMicPos;
                if (deltaMicPos < 0)
                {
                    deltaMicPos += audioClip.samples;
                }
                absMicPos += deltaMicPos;
                lastMicPos = micPos;

                if (absMicPos > audioClip.samples && absReadPos > audioClip.samples)
                {
                    absMicPos %= audioClip.samples;
                    absReadPos %= audioClip.samples;
                }
            }

            public bool HasSamples ()
            {
                if (!IsRecording())
                {
                    return false;
                }

                UpdateMicrophonePosition();
                return absReadPos + samples.Length < absMicPos;
            }

            public bool Advance()
            {
                if (!HasSamples())
                {
                    return false;
                }

                audioClip.GetData(samples,absReadPos % audioClip.samples);
                absReadPos += samples.Length;
                return true;
            }

            public void Start (string deviceName, int micBuffLengthSeconds,
                int frequency, int outBuffLengthSamples)
            {
                if (IsRecording())
                {
                    return;
                }

                this.deviceName = deviceName;

                if (samples == null || samples.Length != outBuffLengthSamples)
                {
                    samples = new float[outBuffLengthSamples];
                }

                audioClip = Microphone.Start(deviceName,loop:true,
                    micBuffLengthSeconds,frequency);
                absMicPos = absReadPos = lastMicPos = Microphone.GetPosition(deviceName);
            }

            public void End ()
            {
                if (audioClip)
                {
                    Microphone.End(deviceName);
                    Destroy(audioClip);
                    audioClip = null;
                    deviceName = null;
                }
            }
        }

        public float gain = 1.0f;

        private MicrophoneListener microphoneListener = new MicrophoneListener();
        private G722AudioEncoder audioEncoder = new G722AudioEncoder();
        private MediaFormatManager<AudioFormat> audioFormatManager = new MediaFormatManager<AudioFormat>(
            new List<AudioFormat>
            {
                new AudioFormat(SDPWellKnownMediaFormatsEnum.G722)
            }
        );
        private bool isPaused;

        private Queue<Task> mainThreadTasks = new Queue<Task>();
        private TaskCompletionSource<bool> allTaskTcs = new TaskCompletionSource<bool>();
        private readonly object taskLock = new object();

#if UNITY_ANDROID && !UNITY_EDITOR
        private bool microphoneAuthorized = false;
#endif

        private void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#endif
        }

        private void OnDestroy()
        {
            // Mark all tasks completed when this MonoBehaviour is destroyed
            allTaskTcs.TrySetResult(true);

            lock (taskLock)
            {
                mainThreadTasks = null;
            }

            microphoneListener.End();
        }

        private void Update()
        {

#if UNITY_ANDROID && !UNITY_EDITOR
            // Wait for microphone permissions before processing any audio
            if (!microphoneAuthorized)
            {
                microphoneAuthorized = Permission.HasUserAuthorizedPermission(Permission.Microphone);

                if (!microphoneAuthorized)
                {
                    return;
                }
            }
#endif

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

            // Send samples if we have them
            while (microphoneListener.Advance())
            {
                // TODO pool buffers to avoid runtime GC
                var pcmSamples = new short[microphoneListener.samples.Length];
                for (int i = 0; i < microphoneListener.samples.Length; i++)
                {
                    var floatSample = microphoneListener.samples[i];
                    floatSample = Mathf.Clamp(floatSample*gain,-.999f,.999f);
                    pcmSamples[i] = (short)(floatSample * short.MaxValue);
                }

                var encoded = audioEncoder.Encode(pcmSamples);
                OnAudioSourceEncodedSample.Invoke((uint)pcmSamples.Length,encoded);
            }
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
            return new Task(() =>
            {
                // Null means use the default recording device
                microphoneListener.Start(null,10,16000,512);

                // Microphone.GetDeviceCaps(null, out var minFreq, out var maxFreq);
                // Debug.Log("caps: min: " + minFreq + " max: " + maxFreq);
                // Debug.Log("audioclipfreq: " + microphoneListener.audioClip.frequency);
            });
        }
        private Task GetResumeAudioTask()
        {
            return new Task(() => { isPaused = false; });
        }
        private Task GetPauseAudioTask()
        {
            return new Task(() => { isPaused = true; });
        }
        private Task GetCloseAudioTask()
        {
            return new Task(() =>
            {
                microphoneListener.End();
            });
        }
    }
}