using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Avatars
{
    public class AvatarDataGenerator : NetworkBehaviour
    {
        public int BytesPerMessage = 0;

        private float lastTransmitTime;
        private Avatar avatar;

        private void Awake()
        {
            avatar = GetComponentInParent<Avatar>();
        }

        private void Update()
        {
            if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
            {
                lastTransmitTime = Time.time;
                var message = ReferenceCountedSceneGraphMessage.Rent(BytesPerMessage);
                Send(message);
            }
        }
    }
}