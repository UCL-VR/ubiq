using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Avatars
{
    public class BotDataGenerator : NetworkBehaviour
    {
        public int BytesPerMessage = 500;
        public int UpdateRate = 60;
        private float lastTransmitTime;

        private void Update()
        {
            if ((Time.time - lastTransmitTime) > (1f / UpdateRate))
            {
                lastTransmitTime = Time.time;
                var message = ReferenceCountedSceneGraphMessage.Rent(BytesPerMessage);
                Send(message);
                // Debug.Log($"Sending message of size {BytesPerMessage} bytes");
            }
        }
    }
}