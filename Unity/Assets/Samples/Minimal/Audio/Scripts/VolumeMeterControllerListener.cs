using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Voip;

namespace Ubiq.Samples
{
    // Debug
    public class VolumeMeterControllerListener : MonoBehaviour
    {
        public float gain = 1.0f;
        public VoipPeerConnectionManager peerConnectionManager;

        private Image image;
        private Material material;
        private List<float> volumeSamples = new List<float>();
        public float volume = 0.0f;

        private void Awake()
        {
            image = GetComponent<Image>();
            material = image.material; // take a copy
        }

        private void OnEnable()
        {
            peerConnectionManager.OnPeerConnection.AddListener(OnPeerConnection);
        }

        private void OnDisable()
        {
            peerConnectionManager.OnPeerConnection.RemoveListener(OnPeerConnection);
        }

        private void OnPeerConnection (VoipPeerConnection pc)
        {
            pc.audioSink.OnVolumeChange += OnVolumeChange;
        }

        private void OnVolumeChange (float vol)
        {
            volumeSamples.Add(vol);
        }

        // Update is called once per frame
        void Update()
        {
            if (volumeSamples.Count > 0)
            {
                var sum = 0.0f;
                for (int i = 0; i < volumeSamples.Count; i++)
                {
                    sum += volumeSamples[i];
                }
                var avg = sum / volumeSamples.Count;
                volume = avg;
                volumeSamples.Clear();
            }

            material.SetFloat("_Volume", volume * gain);
        }
    }
}
