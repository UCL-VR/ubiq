using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Networking;
using Ubiq.Messaging;

namespace Ubiq.Samples {

    [NetworkComponentId(typeof(WebRtcPeerConnectionUvMeterSample), 8)]
    public class WebRtcPeerConnectionUvMeterSample : MonoBehaviour, INetworkComponent
    {
        public VolumeMeterController meter;

        private NetworkContext context;

        void Start()
        {
            context = NetworkScene.Register(this);
        }

        // Update is called once per frame
        void Update()
        {

        }

        public struct Message
        {
            public float volume;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            meter.Volume = message.FromJson<Message>().volume;
       }
    }
}