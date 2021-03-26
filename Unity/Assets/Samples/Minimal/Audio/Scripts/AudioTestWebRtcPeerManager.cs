using System.Collections;
using System.Collections.Generic;
using Ubiq.WebRtc;
using UnityEngine;

namespace Ubiq.Samples.Minimal.Audio
{
    public class AudioTestWebRtcPeerManager : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<WebRtcPeerConnection>().Id = new Messaging.NetworkId(1);
        }
    }
}
