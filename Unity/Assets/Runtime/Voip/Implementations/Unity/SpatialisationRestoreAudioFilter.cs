using UnityEngine;

namespace Ubiq.Voip.Implementations.Unity
{
    /// <summary>
    ///
    /// </summary>
    public class SpatialisationRestoreAudioFilter : MonoBehaviour
    {
        private SpatialisationCacheAudioFilter cacheAudioFilter;
        private int m_sampleRate;

        void OnEnable()
        {
            OnAudioConfigurationChanged(false);
            AudioSettings.OnAudioConfigurationChanged += OnAudioConfigurationChanged;
            cacheAudioFilter = GetComponent<SpatialisationCacheAudioFilter>();
        }

        void OnDisable()
        {
            AudioSettings.OnAudioConfigurationChanged -= OnAudioConfigurationChanged;
        }

        void OnAudioConfigurationChanged(bool deviceWasChanged)
        {
            m_sampleRate = AudioSettings.outputSampleRate;
        }

        /// <summary>
        /// </summary>
        /// <note>
        /// Call on the audio thread, not main thread.
        /// </note>
        /// <param name="data"></param>
        /// <param name="channels"></param>
        void OnAudioFilterRead(float[] data, int channels)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] *= cacheAudioFilter.cache[i];
            }
        }
    }
}