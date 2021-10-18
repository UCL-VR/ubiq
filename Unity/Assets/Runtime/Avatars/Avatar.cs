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
    /// <summary>
    /// The base class for all Avatars that are intended to work with Ubiq's Avatar system.
    /// Components can either subclass this type, or be instantiated next to it, to support
    /// their custom behaviours.
    /// </summary>
    public class Avatar : MonoBehaviour, INetworkObject
    {
        /// <summary>
        /// The unique identifier of the Prefab for this Avatar; this is the 'base' of the Avatars representation in the world. The other Components
        /// attached to this avatar, and the properties of the Peer, will customise its appearance.
        /// </summary>
        public string PrefabUuid;

        /// <summary>
        /// Whether the Avatar instance represents a local or remote player. This flag is nominal only; child components do not have to use it.
        /// </summary>
        [NonSerialized]
        public bool IsLocal;

        /// <summary>
        /// The Network Id of this Avatar. All the Avatars in a peer group for one player have the same Id. This must be set externally, for example,
        /// by the AvatarManager.
        /// </summary>
        public NetworkId Id { get; set; } = NetworkId.Unique();

        /// <summary>
        /// The Peer that the Avatar represents. Not all Avatar instances necessarily represent live peers - Avatars may be created to implement
        /// customisation interfaces, NPCs & crowds, or playback, for example.
        /// Be mindful that some components, such as those above, may repurpose the UUID member. Do not assume a UUID, even if present, refers to
        /// a valid peer.
        /// </summary>
        /// <remarks>
        /// Peer is set externally (using SetPeer()). This must be done as soon as the Avatar is created.
        /// </remarks>
        public IPeer Peer { get; private set; }

        /// <summary>
        /// Emitted when the properties of the Peer this Avatar belongs to are updated.
        /// </summary>
        public PeerUpdatedEvent OnPeerUpdated = new PeerUpdatedEvent();

        /// <summary>
        /// A dummy PeerInterface for local properties.
        /// </summary>
        public class AvatarPeerInterface : IPeer
        {
            public AvatarPeerInterface()
            {
            }

            public string this[string key]
            {
                get => null;
                set => throw new NotImplementedException();
            }

            public string UUID => null;

            public NetworkId NetworkObjectId => throw new NotImplementedException();
        }

        private void Awake()
        {
            Peer = new AvatarPeerInterface();
        }

        public void SetPeer(IPeer peerInterface)
        {
            Peer = peerInterface;
            OnPeerUpdated.SetExisting(peerInterface);
            if (hasStarted)
            {
                Debug.LogError("Setting the Avatar Peer after Start() is not supported.");
            }
        }

        private bool hasStarted = false;

        private void Start()
        {
            hasStarted = true;
        }

        private void Reset()
        {
            PrefabUuid = name;
        }
    }
}