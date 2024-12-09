using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Spawning;
using UnityEngine;

namespace Ubiq.Avatars
{
    public class AvatarDataGenerator : MonoBehaviour
    {
        public int BytesPerMessage = 0;

        private float lastTransmitTime;
        private Avatar avatar;
        private NetworkScene networkScene;
        
        private void Start()
        {
            avatar = GetComponentInParent<Avatar>();
            networkScene = NetworkScene.Find(this);
        }

        private void Update()
        {
            if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
            {
                lastTransmitTime = Time.time;
                var message = ReferenceCountedSceneGraphMessage.Rent(BytesPerMessage);
                networkScene.Send(NetworkId.Unique(), message);
            }
        }
    }
}