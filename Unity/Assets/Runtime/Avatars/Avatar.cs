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
    /// <summary>
    /// The base class for all Avatars that are intended to work with Ubiq's Avatar system.
    /// Components can either subclass this type, or be instantiated next to it, to support
    /// their custom behaviours.
    /// </summary>
    public class Avatar : MonoBehaviour, INetworkSpawnable
    {
        /// <summary>
        /// The NetworkId set by the Spawner when this Avatar is created.
        /// </summary>
        public NetworkId NetworkId { get; set; }

        /// <summary>
        /// Whether the Avatar instance represents a local or remote player. This flag is nominal only; child components do not have to use it.
        /// </summary>
        [NonSerialized]
        public bool IsLocal;

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
        public AvatarHints hints { get; private set; }

        /// <summary>
        /// Emitted when the properties of the Peer this Avatar belongs to are updated.
        /// </summary>
        public PeerUpdatedEvent OnPeerUpdated = new PeerUpdatedEvent();

        /// <summary>
        /// The Update Rate (in Hz) that Components should use. This is suggested only and some Components may decide they need a higher rate.
        /// </summary>
        public int UpdateRate = 60;


        // These properties should be set by whatever Component controls the avatar.
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Quaternion Rotation { get; set; }

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

            public string uuid => null;

            public NetworkId networkId => throw new NotImplementedException();

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                throw new NotImplementedException();
            }
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

        public void SetHints(AvatarHints hints)
        {
            this.hints = hints;
        }

        private bool hasStarted = false;

        private void Start()
        {
            hasStarted = true;
        }

        private Vector3 previousPosition;
        private Quaternion previousRotation;
    }
}