using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;

namespace Ubiq.Samples
{
    [NetworkComponentId(typeof(WebRtcRemoteSineWaveSample), 9)]
    public class WebRtcRemoteSineWaveSample : MonoBehaviour, INetworkComponent
    {
        private NetworkContext context;

        // Start is called before the first frame update
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
            public string command;
            public float argument;
        }

        public void SetFrequency(float frequency)
        {
            var message = new Message();
            message.command = "frequency";
            message.argument = frequency;
            context.SendJson<Message>(message);
        }

        public void ToggleSineWave()
        {
            var message = new Message();
            message.command = "toggle";
            context.SendJson<Message>(message);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
        }
    }
}