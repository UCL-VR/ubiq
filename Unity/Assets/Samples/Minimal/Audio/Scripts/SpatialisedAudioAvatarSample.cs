using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Ubiq.Networking;
using Ubiq.WebRtc;
using UnityEngine;

namespace Ubiq.Samples
{
    public class SpatialisedAudioAvatarSample : MonoBehaviour
    {
        private WebRtcDataChannel channel;
        public float worldScale = 0.1f;

#pragma warning disable 0649
        [Serializable]
        private struct AvatarTransform
        {
            public float x;
            public float y;
        }
#pragma warning restore 0649

        private void Awake()
        {
            channel = GetComponentInChildren<WebRtcDataChannel>();
        }

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            var msg = channel.Receive();
            if(msg != null)
            {
                var t = JsonUtility.FromJson<AvatarTransform>(msg.ToString());
                this.transform.position = new Vector3(t.x * worldScale, transform.position.y, t.y * worldScale * -1f);
            }
        }
    }

}
