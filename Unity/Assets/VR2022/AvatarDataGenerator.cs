using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Avatars
{
    public class AvatarDataGenerator : MonoBehaviour, INetworkComponent
    {
        public int BytesPerMessage = 0;

        private float lastTransmitTime;
        private Avatar avatar;
        private NetworkContext context;

        private void Awake()
        {
            avatar = GetComponentInParent<Avatar>();
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            // We dont need to do anything here
        }

        // Start is called before the first frame update
        void Start()
        {
            context = NetworkScene.Register(this);
        }

        // Update is called once per frame
        void Update()
        {
            if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
            {
                lastTransmitTime = Time.time;
                var message = ReferenceCountedSceneGraphMessage.Rent(BytesPerMessage);
                context.Send(message);
                
            }
        }
    }
}