using UnityEngine;

namespace Ubiq.Voip.Implementations.Unity
{
    /// <summary>
    /// Part of the workaround for lack of spatialisation in Unity's WebRTC
    /// implementation. This part multiplies the cache with the output of the
    /// WebRTC audio filter.
    /// </summary>
    public class SpatialisationRestoreFilter : MonoBehaviour
    {
        private SpatialisationCacheFilter cacheAudioFilter;

        void OnEnable()
        {
            cacheAudioFilter = GetComponent<SpatialisationCacheFilter>();
        }

        /// <summary>
        /// </summary>
        /// <note>
        /// Called on the audio thread, not the main thread.
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