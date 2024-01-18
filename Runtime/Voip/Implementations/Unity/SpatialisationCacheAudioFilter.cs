using UnityEngine;

namespace Ubiq.Voip.Implementations.Unity
{
    /// <summary>
    /// Part of the workaround for lack of spatialisation in Unity's WebRTC
    /// implementation. This part stores the raw spatialisation information.
    /// Requires an AudioClip filled with 1s
    /// </summary>
    public class SpatialisationCacheFilter : MonoBehaviour
    {
        public float[] cache = new float[4096];

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
                cache[i] = data[i];
            }
        }
    }
}