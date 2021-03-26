using System;
using System.Collections.Generic;
using Ubiq.Dictionaries;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Rooms
{
    [Serializable]
    public struct Message
    {
        public string type;
        public string args;

        public Message(string type, object args)
        {
            this.type = type;
            this.args = JsonUtility.ToJson(args);
        }
    }

    /// <summary>
    /// An immutable description of a remote peer. This type is used by the RoomClient to store information about the other peers
    /// in a room, and as a data transfer object when communicating with the RoomServer.
    /// </summary>
    /// <remarks>
    /// While the object is technically immutable, note that the serialised dictionary is a reference. This means while user code
    /// cannot change the dictionary, the properties of an existing copy of PeerInfo may change, if RoomClient receives an update.
    /// </remarks>
    [Serializable]
    public struct PeerInfo
    {
        /// <summary>
        /// The self generated UUID of this client
        /// </summary>
        public string UUID
        {
            get
            {
                return uuid;
            }
        }

        /// <summary>
        /// A list of key-value pairs that can be set by other components at the remote-peer's end
        /// </summary>
        public string this[string key]
        {
            get => properties[key];
        }

        /// <summary>
        /// The object id of the network object/scene that hosts the RoomClient. This is almost always the NetworkScene/root.
        /// </summary>
        /// <remarks>
        /// It is possible to have multiple RoomClients in a scene, but this is an advanced use case that is by design not supported out of the box.
        /// </remarks>
        public NetworkId NetworkObjectId
        {
            get
            {
                return networkId;
            }
        }

        [SerializeField]
        private string uuid;
        [SerializeField]
        private NetworkId networkId;
        [SerializeField]
        private SerializableDictionary properties;

        public PeerInfo(string uuid, NetworkId networkId, SerializableDictionary properties)
        {
            this.uuid = uuid;
            this.networkId = networkId;
            this.properties = properties;
        }
    }

    [Serializable]
    public struct RoomInfo //todo: make this a struct (means anything that uses the dictionary must check if its initialised)
    {
        public string Name
        {
            get => name;
        }

        public string UUID
        {
            get => uuid;
        }

        /// <summary>
        /// A list of key-value pairs that can be set by other components at the remote-peer's end
        /// </summary>
        public string this[string key]
        {
            get => properties[key];
        }

        public IEnumerable<KeyValuePair<string, string>> Properties
        {
            get
            {
                return properties.Enumerator;
            }
        }

        public string Joincode
        {
            get => joincode;
        }

        public bool Publish
        {
            get => publish;
        }

        [SerializeField]
        private string name;
        [SerializeField]
        private string uuid;
        [SerializeField]
        private string joincode;
        [SerializeField]
        private bool publish;
        [SerializeField]
        private SerializableDictionary properties;

        public RoomInfo(string name, string uuid, string joincode, bool publish, SerializableDictionary properties)
        {
            this.name = name;
            this.uuid = uuid;
            this.joincode = joincode;
            this.publish = publish;
            this.properties = properties;
        }
    }

    [Serializable]
    public struct JoinArgs
    {
        public string joincode;
        public string name;
        public bool publish;
        public PeerInfo peer;
    }

    [Serializable]
    public struct RejectedArgs
    {
        public string reason;
        public JoinArgs requestArgs;
    }

    /// <summary>
    /// Joined Args represents the state of the room at the moment this peer joined
    /// </summary>
    [Serializable]
    public class AcceptedArgs
    {
        public RoomInfo room;
        public List<PeerInfo> peers;

        public AcceptedArgs()
        {
            room = new RoomInfo();
            peers = new List<PeerInfo>();
        }
    }

    [Serializable]
    public class RoomsRequestArgs
    {
    }

    [Serializable]
    public class RoomsResponseArgs
    {
        public string version;
        public List<RoomInfo> rooms;
    }

    [Serializable]
    public class Blob
    {
        public string room;
        public string uuid;
        public string blob;

        public string GetKey()
        {
            return $"{room}:{uuid}";
        }
    }
}