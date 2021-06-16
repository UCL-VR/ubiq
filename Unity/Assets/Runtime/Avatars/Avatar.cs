using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using UnityEngine.Events;
using Ubiq.Rooms;
using Ubiq.Dictionaries;
using Ubiq.Spawning;

namespace Ubiq.Avatars
{
    public class Avatar : MonoBehaviour, INetworkObject, ISpawnable
    {
        public string uuid;

        public NetworkId Id { get; set; } = NetworkId.Unique();

        [Serializable]
        public class AvatarEvent : UnityEvent<Avatar> { }

        /// <summary>
        /// The AvatarManager that created this Avatar. Each Avatar must have an AvatarManager, as this is the object that will keep the Dictionaries up to date.
        /// </summary>
        public AvatarManager AvatarManager { get; set; }

        public AvatarEvent OnUpdated;
        public SerializableDictionary Properties { get; protected set; }

        private void Awake()
        {
            Properties = new SerializableDictionary();
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
        // when avatars are spawned (e.g. for replaying purposes) local should be set to false
        public void OnSpawned(bool local)
        {
            // technically, it would be local too, but we don't want user-controlled replay avatars
            this.local = false; 
        }
    }
}