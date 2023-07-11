using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Voip
{
    public class VoipSpeechIndicator : MonoBehaviour
    {
        public List<Transform> volumeIndicators;

        public Vector3 minIndicatorScale;
        public Vector3 maxIndicatorScale;

        public float minVolume = 0.005f;
        public float minVolumeWhenOn = 0.002f; // Should be lower for hysteresis
        public float maxVolume = 0.02f;

        public float windowSeconds = 0.6f;

        private List<VoipPeerConnection.AudioStats> stats = new List<VoipPeerConnection.AudioStats>();
        private List<float> volumes = new List<float>();
        private List<bool> indicatorStates = new List<bool>();

        void Update()
        {
            UpdateVolumes();
            UpdateIndicators();
            UpdatePosition();
        }

        private void UpdateVolumes()
        {
            if (volumeIndicators.Count == 0 )
            {
                return;
            }

            var secondsPerIndicator = windowSeconds / volumeIndicators.Count;
            if (volumes.Count != volumeIndicators.Count)
            {
                volumes.Clear();
                for (int i = 0; i < volumeIndicators.Count; i++)
                {
                    volumes.Add(0);
                }
            }

            for (int i = 0; i < volumes.Count; i++)
            {
                volumes[i] = 0;
            }

            if (stats.Count == 0)
            {
                return;
            }

            // Walk through the stats structs, calculate volume for each window
            var samplesPerVolume = secondsPerIndicator * stats[0].sampleRate;
            var volumeIdx = 0;
            var sampleHead = 0.0f;
            var volumeSum = 0.0f;
            var endSamples = samplesPerVolume;
            for (int i = stats.Count-1; i >= 0; i--)
            {
                var prevSampleHead = sampleHead;
                while (sampleHead + stats[i].sampleCount > endSamples)
                {
                    // Volume window ends in this audio stats struct
                    // Multiple volume windows may end in this stats struct
                    var samplesToUse = endSamples - sampleHead;
                    var proportion = samplesToUse / stats[i].sampleCount;
                    volumeSum += stats[i].volumeSum * proportion;
                    volumes[volumeIdx] = volumeSum / samplesPerVolume;

                    volumeIdx++;
                    volumeSum = 0;
                    sampleHead = endSamples;
                    endSamples += samplesPerVolume;

                    // We've fully filled all volume windows. Now remove any
                    // stats structs old enough to feature in no volume windows
                    if (volumeIdx >= volumes.Count)
                    {
                        stats.RemoveRange(0,i);
                        break;
                    }
                }

                if (volumeIdx >= volumes.Count)
                {
                    break;
                }

                // Volume window stretches into next audio stats
                var endSampleHead = prevSampleHead + stats[i].sampleCount;
                var remainingSamples = endSampleHead - sampleHead;
                volumeSum += stats[i].volumeSum * (remainingSamples / stats[i].sampleCount);
                sampleHead = endSampleHead;
            }

            // Zero out any unfilled volumes. Means we need a full audio stat
            // window or a volume will be zero. Should always be the case after
            // the first second or so.
            for (int i = volumeIdx; i < volumes.Count; i++)
            {
                volumes[i] = 0;
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
                var thresh = indicatorStates[i] ? minVolumeWhenOn : minVolume;
                indicatorStates[i] = volumes[i] > thresh;

                if (indicatorStates[i])
                {
                    volumeIndicators[i].gameObject.SetActive(true);
                    var range = maxVolume - minVolumeWhenOn;
                    var t = (volumes[i] - minVolumeWhenOn) / range;
                    volumeIndicators[i].localScale = Vector3.Lerp(
                        minIndicatorScale,maxIndicatorScale,t);
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

        /// <summary>
        /// Pushes a new set of audio stats to the indicator. Treats the stats
        /// as a continuous stream, where these are the very latest stats.
        /// </summary>
        public void PushAudioStats(VoipPeerConnection.AudioStats stats)
        {
            if (stats.sampleCount == 0)
            {
                return;
            }

            if (this.stats.Count > 0 && this.stats[0].sampleRate != stats.sampleRate)
            {
                // May happen if the audio device changes, which should be rare
                // so just clear the buffer and start again. Will mean a small
                // interruption in the indicator, but it ensure the entire stats
                // buffer has the same sampleRate. Simplifies things a lot.
                this.stats.Clear();
            }

            this.stats.Add(stats);
        }
    }
}
