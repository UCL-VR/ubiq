using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.WebRTC;

#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Ubiq.Voip.Implementations.Unity
{
    public class PeerConnectionMicrophone : MonoBehaviour
    {
        public enum State
        {
            Idle,
            Starting,
            Running
        }

        public State state
        {
            get
            {
                if (!audioSource || audioSource.clip == null)
                {
                    return State.Idle;
                }
                if (audioSource.isPlaying)
                {
                    return State.Running;
                }
                return State.Starting;
            }
        }

        public AudioStreamTrack audioStreamTrack { get; private set; }
        public event Action<AudioStats> statsPushed;

        private AudioSource audioSource;
        private List<GameObject> users = new List<GameObject>();
        private bool microphoneAuthorized;
        private AudioStatsFilter statsFilter;

        private void Awake()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
#endif
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

            if (state == State.Idle && users.Count > 0)
            {
                RequireAudioSource();
                audioSource.clip = Microphone.Start("",true,1,AudioSettings.outputSampleRate);
            }

            if (state == State.Starting)
            {
                if (Microphone.GetPosition("") > audioSource.clip.frequency / 8.0f)
                {
                    audioSource.loop = true;
                    audioSource.Play();
                    audioStreamTrack = new AudioStreamTrack(audioSource);
                }
            }

            if (state == State.Running && users.Count == 0)
            {
                audioSource.Stop();
                Microphone.End("");
                Destroy(audioSource.clip);
                audioSource.clip = null;
                audioStreamTrack.Dispose();
                audioStreamTrack = null;
            }
        }

        private void StatsFilter_StatsPushed(AudioStats stats)
        {
            statsPushed?.Invoke(stats);
        }

        private void RequireAudioSource()
        {
            if(!audioSource)
            {
                audioSource = GetComponent<AudioSource>();

                if (!audioSource)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    statsFilter = gameObject.AddComponent<AudioStatsFilter>();
                    statsFilter.hideFlags = HideFlags.HideInInspector;
                    statsFilter.SetStatsPushedCallback(StatsFilter_StatsPushed);
                }
            }
        }

        /// <summary>
        /// Indicate a new user to the microphone, with an optional callback
        /// for audio stats. If run as part of a coroutine, this will complete
        /// when the microphone is ready to be used. If the user has already
        /// been added, the callback will be replaced.
        /// </summary>
        public IEnumerator AddUser(GameObject user)
        {
            if (!users.Contains(user))
            {
                users.Add(user);
            }

            while (state != State.Running)
            {
                yield return null;
            }
        }

        /// <summary>
        /// Remove a user from the microphone. If user count reaches zero, the
        /// microphone will be stopped.
        /// </summary>
        public void RemoveUser(GameObject user)
        {
            users.Remove(user);
        }
    }
}