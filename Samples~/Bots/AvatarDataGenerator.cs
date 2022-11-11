using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Spawning;
using UnityEngine;

namespace Ubiq.Avatars
{
    public class AvatarDataGenerator : MonoBehaviour, INetworkSpawnable
    {
        public int BytesPerMessage = 0;

        private float lastTransmitTime;
        private Avatar avatar;
        private NetworkContext context;

        public NetworkId NetworkId { get; set; }

        private void Awake()
        {
            avatar = GetComponentInParent<Avatar>();
            context = NetworkScene.Register(this);
        }

        private void Update()
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