using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Dictionaries;
using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq.Rooms.Messages
{
    [Serializable]
    internal struct Message
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
    /// An immutable description of a remote peer that is used as a Data Transfer Object by the RoomClient & RoomServer.
    /// </summary>
    [Serializable]
    public struct PeerInfo
    {
        public string uuid;
        public NetworkId networkId;
        public SerializableDictionary properties;

        public PeerInfo(string uuid, NetworkId networkId, SerializableDictionary properties)
        {
            this.uuid = uuid;
            this.networkId = networkId;
            this.properties = properties;
        }
    }

    /// <summary>
    /// An immutable description of a Room. This type is used by RoomClient as a Data Transfer Object. It can also pass it via
    /// events in the guise of an IRoom.
    /// </summary>
    [Serializable]
    public struct RoomInfo : IRoom
    {
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

        public string Name
        {
            get => name;
        }

        public string UUID
        {
            get => uuid;
        }

        public string JoinCode
        {
            get => joincode;
        }

        public bool Publish
        {
            get => publish;
        }

        public string this[string key]
        {
            get => properties != null ? properties[key] : null;
            set => Debug.LogError("Cannot set properties on a read-only room");
        }

        public RoomInfo(string name, string uuid, string joincode, bool publish, SerializableDictionary properties)
        {
            this.name = name;
            this.uuid = uuid;
            this.joincode = joincode;
            this.publish = publish;
            this.properties = properties;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return properties.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return properties.GetEnumerator();
        }
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
    public struct JoinRequest
    {
        public string uuid;
        public string joincode;
        public string name;
        public bool publish;
        public PeerInfo peer;
    }

    [Serializable]
    public struct RejectedArgs
    {
        public string reason;
        public JoinRequest joinArgs;
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
    public class DiscoverRoomsRequest
    {
        public string joincode;
    }

    [Serializable]
    public class DiscoverRoomsResponse
    {
        public string version;
        public List<RoomInfo> rooms;
        public DiscoverRoomsRequest request;
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

namespace Ubiq.Rooms
{
    public interface IRoom : IEnumerable<KeyValuePair<string,string>>
    {
        string Name { get; }
        string UUID { get; }
        string JoinCode { get;  }
        bool Publish { get; }
        string this[string key] { get; set; }
    }

    /// <summary>
    /// The interface to a Peer. The instance will persist as long as the Peer is in scope; it is safe to
    /// store, pass around and use as a key.
    /// </summary>
    /// <remarks>
    /// Only the properties of the local Peer can be set. All other peers are read-only.
    /// </remarks>
    public interface IPeer
    {
        string UUID { get; }

        string this[string key] { get; set; }

        /// <summary>
        /// The ObjectId of the NetworkScene that hosts the RoomClient of this Peer
        /// </summary>
        NetworkId NetworkObjectId { get; }
    }

    public class RejectedEvent : UnityEvent<Rejection>
    {
    };

    public class RoomsDiscoveredEvent : UnityEvent<List<IRoom>,RoomsDiscoveredRequest>
    {
    };

    public class PeerEvent : UnityEvent<IPeer>
    {
    };

    public class PeerUpdatedEvent : ExistingEvent<IPeer>
    {
    };

    public class RoomEvent : UnityEvent<IRoom>
    {
    };

    public struct RoomsDiscoveredRequest
    {
        public string joincode;
    }

    public struct Rejection
    {
        public string reason;
        public string uuid;
        public string joincode;
        public string name;
        public bool publish;
    }
}