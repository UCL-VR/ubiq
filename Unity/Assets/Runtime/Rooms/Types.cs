using System;
using System.Collections.Generic;
using System.Linq;
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
    /// The RoomInterface class provides encapsulation for the RoomInfo data transfer object. It provides a single
    /// reference that is safe to store, and prevents unsafe writes. All accesses to the RoomClient's current Room
    /// should go through this.
    /// </summary>
    public abstract class RoomInterface
    {
        public string Name { get; protected set; }
        public string UUID { get; protected set; }
        public string JoinCode { get; protected set; }
        public bool Publish { get; protected set; }

        protected SerializableDictionary properties;

        public RoomInterface()
        {
            properties = new SerializableDictionary();
        }

        public string this[string key]
        {
            get => properties[key];
            set => properties[key] = value;
        }

        public IEnumerable<KeyValuePair<string, string>> Properties
        {
            get => properties.Enumerator;
        }

        public RoomInfo GetRoomInfo()
        {
            return new RoomInfo(Name, UUID, JoinCode, Publish, properties);
        }
    }

    /// <summary>
    /// The PeerInterface class provides encapsulation of the PeerInfo data transfer object, similar to the RoomInterface above.
    /// </summary>
    public abstract class PeerInterface
    {
        public string UUID { get; private set; }

        public PeerInterface(String uuid)
        {
            UUID = uuid;
            properties = new SerializableDictionary();
        }

        protected SerializableDictionary properties;
        protected NetworkId networkId;

        public string this[string key]
        {
            get => properties[key];
            set => properties[key] = value;
        }

        public PeerInfo GetPeerInfo()
        {
            return new PeerInfo(UUID, networkId, properties);
        }
    }

    /// <summary>
    /// An immutable description of a remote peer. This type is used by the RoomClient to store information about the other peers
    /// in a room, and as a data transfer object when communicating with the RoomServer.
    /// </summary>
    /// <remarks>
    /// While the object is technically immutable, note that the serialised dictionary is a reference. This means while user code
    /// cannot change the dictionary, the properties of an existing PeerInfo may change, if RoomClient receives an update.
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
            get => properties != null ? properties[key] : null;
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

    /// <summary>
    /// An immutable description of a Room. This type is used by RoomClient to communicate information about a room to other peers, 
    /// and to other objects via events.
    /// This representation of a room cannot be changed; use the RoomClient::Room member if you need to change properties of the
    /// current room.
    /// </summary>
    /// <remarks>
    /// While the object is technically immutable, note that the serialised dictionary is a reference. This means while user code
    /// cannot change the dictionary, the properties of an existing copy of PeerInfo may change, if RoomClient receives an update.
    /// </remarks>
    [Serializable]
    public struct RoomInfo
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
            get => properties != null ? properties[key] : null;
        }

        public IEnumerable<KeyValuePair<string, string>> Properties
        {
            get
            {
                return properties != null ? properties.Enumerator : Enumerable.Empty<KeyValuePair<string, string>>();
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
    public struct JoinRequest
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
        public JoinRequest requestArgs;
    }

    [Serializable]
    public struct LeaveRequest
    {
        public PeerInfo peer;
    }

    /// <summary>
    /// The room and peers members contain the state of the room when the join occured on the server
    /// </summary>
    [Serializable]
    public class SetRoom
    {
        public RoomInfo room;
        public List<PeerInfo> peers;

        public SetRoom()
        {
            room = new RoomInfo();
            peers = new List<PeerInfo>();
        }
    }

    [Serializable]
    public class RoomsRequest
    {
    }

    [Serializable]
    public class RoomsResponse
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

    [Serializable]
    public struct PingResponse
    {
        public string sessionId; 
    }

    public struct PingRequest
    {
        public NetworkId id;
    }
}