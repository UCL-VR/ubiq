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

        public event Action stopping = delegate {};
        public event Action started = delegate {};
        public event Action ready = delegate {};

        private List<GameObject> users = new List<GameObject>();
        private bool microphoneAuthorized;

        public AudioSource audioSource;
        public AudioStreamTrack audioStreamTrack;

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

        private void RequireAudioSource()
        {
            if(!audioSource)
            {
                audioSource = GetComponent<AudioSource>();

                if (!audioSource)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }

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

        public void RemoveUser(GameObject user)
        {
            users.Remove(user);
        }
    }
}