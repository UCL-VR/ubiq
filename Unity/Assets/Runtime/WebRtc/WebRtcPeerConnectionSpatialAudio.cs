using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.WebRtc
{

    /// <summary>
    /// Attached to a PeerConnection object or similar to support spatial audio through volume control.
    /// </summary>
    [RequireComponent(typeof(WebRtcPeerConnection))]
    public class WebRtcPeerConnectionSpatialAudio : MonoBehaviour
    {
        public AnimationCurve falloff;

        private WebRtcPeerConnection peerconnection;

        private void Reset()
        {
            falloff = AnimationCurve.Linear(0, 1, 100, 0);
        }

        private void Awake()
        {
            this.peerconnection = new WebRtcPeerConnection();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            var listener = WebRtcSpatialAudioListener.Active;
            if (listener != null)
            {
                var d = (listener.transform.position - transform.position).magnitude;
                var v = falloff.Evaluate(d);
                peerconnection.volumeModifier = v;
            }
            else
            {
                Debug.LogWarning("No Active WebRtcSpatialAudioListener in the scene");
            }
        }
    }
}