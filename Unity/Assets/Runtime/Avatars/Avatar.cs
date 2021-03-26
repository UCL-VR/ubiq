using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using UnityEngine.Events;
using Ubiq.Rooms;
using Ubiq.Dictionaries;

namespace Ubiq.Avatars
{
    public class Avatar : MonoBehaviour, INetworkObject
    {
        public string uuid;

        public NetworkId Id { get; set; } = NetworkId.Unique();

        [Serializable]
        public class AvatarEvent : UnityEvent<Avatar> { }

        public AvatarEvent OnUpdated;
        public SerializableDictionary Properties;

        private void Awake()
        {
            if (OnUpdated == null)
            {
                OnUpdated = new AvatarEvent();
            }
        }

        /// <summary>
        /// Indicates the avatar was instantiated to represent a player on this computer. This flag is informational only. Child components do not have to use it.
        /// </summary>
        public bool local;

        private void Reset()
        {
            local = false; // better not to transmit by accident than to transmit by accident!
            uuid = Guid.NewGuid().ToString();
        }

        public void Merge(SerializableDictionary properties)
        {
            if(Properties.Update(properties))
            {
                OnUpdated.Invoke(this);
            }
        }
    }
}