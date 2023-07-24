using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Voip
{
    public class VoipSpeechIndicator : MonoBehaviour
    {
        private class SmoothVolumeEstimator
        {
            private VolumeEstimator prior;
            private VolumeEstimator post;
            private float priorTime;
            private float postTime;
            private VoipPeerConnection.AudioStats audioStats;

            public SmoothVolumeEstimator(float delaySeconds, float lengthSeconds)
            {
                prior = new VolumeEstimator(delaySeconds,lengthSeconds);
                post = new VolumeEstimator(delaySeconds,lengthSeconds);
            }

            public void Add(VoipPeerConnection.AudioStats audioStats, float time)
            {
                priorTime = postTime;
                postTime = time;

                prior.PushAudioStats(this.audioStats);
                post.PushAudioStats(audioStats);
                this.audioStats = audioStats;
            }

            public float GetVolume(float time)
            {
                var t = Mathf.InverseLerp(priorTime,postTime,time);
                return Mathf.Lerp(prior.volume,post.volume,t);
            }
        }

        [Tooltip("The indicators to enable and scale with VOIP volume. The lowest index is considered the most recent")]
        public List<Transform> volumeIndicators;
        [Tooltip("The scale to set if the volume is at the VolumeFloor. Scales will be linearly interpolated between this and ScaleAtVolumeCeiling")]
        public Vector3 scaleAtVolumeFloor;
        [Tooltip("The scale to set if the volume is at the VolumeCeiling. Scales will be linearly interpolated between this and ScaleAtVolumeFloor")]
        public Vector3 scaleAtVolumeCeiling;
        [Tooltip("Above this volume, start showing the indicator")]
        public float triggerVolume = 0.005f;
        [Tooltip("Above this volume, keep showing the indicator, if we are already. Can be set lower than TriggerVolume for simple hysteresis")]
        public float persistVolume = 0.002f;
        [Tooltip("Lower bound of volume for size scaling. Most of the time, you'll want this to be the same as PersistVolume")]
        public float volumeFloor = 0.002f;
        [Tooltip("Upper bound of volume for size scaling")]
        public float volumeCeiling = 0.02f;
        [Tooltip("Total length of the sampling window. Samples older than this will be discarded")]
        public float windowSeconds = 0.6f;
        [Tooltip("Proportion that a sub-window should overlap its neighbouring sub-windows, for smoother visuals. If 0, no overlap")]
        public float windowOverlap = 0.3f;
        [Tooltip("The max value of noise added or subtracted. Noise makes the indicators seem more dynamic at high framerates")]
        public float noiseAmplitude = 0.05f;
        [Tooltip("How quickly the noise appears to change. Noise makes the indicators seem more dynamic at high framerates")]
        public float noiseFrequency = 1f;

        private List<VoipPeerConnection.AudioStats> stats = new List<VoipPeerConnection.AudioStats>();
        private List<SmoothVolumeEstimator> volumeEstimators = new List<SmoothVolumeEstimator>();
        private List<float> noises = new List<float>();
        private List<bool> indicatorStates = new List<bool>();
        private float time = 0;

        private const float NOISE_INIT_MULTIPLIER = 100.0f;

        void Update()
        {
            time += Time.unscaledDeltaTime;
            UpdateVolumes();
            UpdateNoise();
            UpdateIndicators();
            UpdatePosition();
        }

        private void UpdateVolumes()
        {
            if (volumeEstimators.Count != volumeIndicators.Count)
            {
                volumeEstimators.Clear();
                var secondsPerWindow = windowSeconds / volumeIndicators.Count;
                for (int i = 0; i < volumeIndicators.Count; i++)
                {
                    var start = (i - windowOverlap) * secondsPerWindow;
                    start = Mathf.Max(0,start);
                    var length = (1 + windowOverlap) * secondsPerWindow;
                    if (start + length > windowSeconds)
                    {
                        length = windowSeconds - start;
                    }
                    volumeEstimators.Add(new SmoothVolumeEstimator(start,length));
                }
            }
        }

        private void RefreshNoises(bool force)
        {
            if (force || noises.Count != volumeIndicators.Count)
            {
                noises.Clear();
                for (int i = 0; i < volumeIndicators.Count; i++)
                {
                    noises.Add(Random.value * noiseFrequency * NOISE_INIT_MULTIPLIER);
                }
            }
        }

        private void UpdateNoise()
        {
            RefreshNoises(force:false);

            for (int i = 0; i < noises.Count; i++)
            {
                noises[i] += noiseFrequency * Time.deltaTime;
            }
        }

        private void UpdateIndicators()
        {
            if (indicatorStates.Count != volumeIndicators.Count)
            {
                indicatorStates.Clear();
                for (int i = 0; i < volumeIndicators.Count; i++)
                {
                    indicatorStates.Add(false);
                }
            }

            for(int i = 0; i < volumeIndicators.Count; i++)
            {
                var vol = volumeEstimators[i].GetVolume(time);
                var thresh = indicatorStates[i] ? persistVolume : triggerVolume;
                indicatorStates[i] = vol > thresh;

                if (indicatorStates[i])
                {
                    var noise = noiseAmplitude * Mathf.PerlinNoise(noises[i],0);
                    var noiseVec = Vector3.one * noise;
                    volumeIndicators[i].gameObject.SetActive(true);
                    volumeIndicators[i].localScale = noiseVec + Vector3.Lerp(
                        scaleAtVolumeFloor,scaleAtVolumeCeiling,
                        Mathf.InverseLerp(volumeFloor,volumeCeiling,vol));
                }
                else
                {
                    volumeIndicators[i].gameObject.SetActive(false);
                }
            }
        }

        private void UpdatePosition()
        {
            var cameraTransform = Camera.main.transform;
            var headTransform = transform.parent;
            var indicatorRootTransform = transform;

            // If no indicator is being shown currently, reset position
            var indicatorVisible = false;
            for (int i = 0; i < volumeIndicators.Count; i++)
            {
                if (volumeIndicators[i].gameObject.activeInHierarchy)
                {
                    indicatorVisible = true;
                    break;
                }
            }

            if (!indicatorVisible)
            {
                indicatorRootTransform.forward = headTransform.forward;
                IndicatorsInvisibleThisFrame();
                return;
            }

            // Rotate s.t. the indicator is always 90 deg from camera
            // Method - always two acceptable orientations, pick the closest
            var headToCamera = cameraTransform.position - headTransform.position;
            var headToCameraDir = headToCamera.normalized;
            var dirA = Vector3.Cross(headToCameraDir,headTransform.up);
            var dirB = Vector3.Cross(headTransform.up,headToCameraDir);

            var simA = Vector3.Dot(dirA,indicatorRootTransform.forward);
            var simB = Vector3.Dot(dirB,indicatorRootTransform.forward);

            var forward = simA > simB ? dirA : dirB;

            // Deal with rare case when avatars share a position
            if (forward.sqrMagnitude <= 0)
            {
                forward = indicatorRootTransform.forward;
            }

            indicatorRootTransform.forward = forward;
        }

        // Called every frame the indicators are invisible
        private void IndicatorsInvisibleThisFrame()
        {
            RefreshNoises(force:true);
            time = 0;
        }

        /// <summary>
        /// Pushes a new set of audio stats to the indicator. Treats the stats
        /// as a continuous stream, where these are the very latest stats.
        /// </summary>
        public void PushAudioStats(VoipPeerConnection.AudioStats stats)
        {
            for (int i = 0; i < volumeEstimators.Count; i++)
            {
                volumeEstimators[i].Add(stats,time);
            }
        }
    }
}
