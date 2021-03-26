using System;
using System.Collections.Generic;
using Ubiq.Networking;
using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Dictionaries;
using UnityEngine.Profiling;

namespace Ubiq.Rooms
{
    /// <summary>
    /// Maintains a representation of a shared room. This component exchanges messages with other room managers
    /// and the matchmaking service in order to keep all peers in sync.
    /// The room manager is responsible for forwarding messages to other peers, until p2p connections can be e
    /// </summary>
    [RequireComponent(typeof(NetworkScene))] //not strictly true, but anyone advanced enough to use multiple clients in one scene can remove this...
    [NetworkComponentId(typeof(RoomClient), 1)]
    public class RoomClient : MonoBehaviour, INetworkComponent
    {
        private NetworkContext context;
        private Dictionary<string, PeerInfo> peers;

        private NetworkId RoomServerObjectId = new NetworkId(1);
        private ushort RoomServerComponentId = 1;

        private const string roomClientVersion = "0.0.4";

        public List<RoomInfo> Available;

        /// <summary>
        /// A list of default servers to connect to on start-up
        /// </summary>
        public ConnectionDefinition[] servers;

        private Dictionary<string, Action<string>> blobCallbacks;

        // In C#, interfaces do not have to be as accessible as classes that implement them, so we can use explicit
        // implementations to create the equivalent of C++ friends.
        // https://stackoverflow.com/questions/42961744
        // This interface ensures that only the RoomClient may update a RoomInterface directly.
        private interface IRoomInterfaceFriend
        {
            void Update(RoomInfo args);
            bool NeedsUpdate();
        }

        /// <summary>
        /// The RoomInterface class provides encapsulation for the RoomInfo data transfer object. It provides a single
        /// reference that is safe to store, and prevents unsafe writes. All accesses to the RoomClient's current Room
        /// should go through this.
        /// </summary>
        public class RoomInterface : IRoomInterfaceFriend
        {
            public string Name { get; private set; }
            public string UUID { get; private set; }
            public string JoinCode { get; private set; }
            public bool Publish { get; private set; }

            private SerializableDictionary properties;

            public RoomInterface()
            {
                properties = new SerializableDictionary();
            }

            // The explicit implementation means the member will have the same visibility as the interface that declares it
            void IRoomInterfaceFriend.Update(RoomInfo args)
            {
                Name = args.Name;
                UUID = args.UUID;
                JoinCode = args.Joincode;
                Publish = args.Publish;
                properties = new SerializableDictionary(args.Properties); // todo: merge the dictionaries properly
            }

            // The IsUpdated() method has side effects (in that the flag will be cleared) so only allow calling it from inside RoomClient
            bool IRoomInterfaceFriend.NeedsUpdate()
            {
                if (properties != null)
                {
                    return properties.IsUpdated();
                }
                else
                {
                    return false;
                }
            }

            public string this[string key]
            {
                get => properties[key];
                set => properties[key] = value;
            }

            public IEnumerable<KeyValuePair<string,string>> Properties
            {
                get => properties.Enumerator;
            }

            public RoomInfo GetRoomInfo()
            {
                return new RoomInfo(Name, UUID, JoinCode, Publish, properties);
            }
        }

        private interface IPeerInterfaceFriend
        {
            void SetNetworkContext(NetworkContext context);
            bool NeedsUpdate();
        }

        /// <summary>
        /// The PeerInterface class provides encapsulation of the PeerInfo data transfer object, similar to the RoomInterface above.
        /// </summary>
        public class PeerInterface : IPeerInterfaceFriend
        {
            public string UUID { get; private set; }

            public PeerInterface(String uuid)
            {
                UUID = uuid;
                properties = new SerializableDictionary();
            }

            private SerializableDictionary properties;
            private NetworkId networkId;

            void IPeerInterfaceFriend.SetNetworkContext(NetworkContext context)
            {
                networkId = context.networkObject.Id;
            }

            bool IPeerInterfaceFriend.NeedsUpdate()
            {
                return properties.IsUpdated();
            }

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

        public RoomInterface Room { get; private set; }
        public PeerInterface Me { get; private set; }

        public class RejectedEvent : UnityEvent<RejectedArgs> { };
        public class PeerEvent : UnityEvent<PeerInfo> { };
        public class RoomsAvailableEvent : UnityEvent<List<RoomInfo>> { };

        /// <summary>
        /// Emitted when a peer has joined or updated its properties
        /// </summary>
        public PeerEvent OnPeer;

        /// <summary>
        /// Emitted when a peer has left the room
        /// </summary>
        public PeerEvent OnPeerRemoved;

        /// <summary>
        /// Emitted when this peer has joined a room
        /// </summary>
        public UnityEvent OnJoinedRoom;

        /// <summary>
        /// Emitted when this peer attempts to join a room and is rejected
        /// </summary>
        public RejectedEvent OnJoinRejected;

        /// <summary>
        /// Emitted when the room this peer is a member of has updated its properties
        /// </summary>
        public UnityEvent OnRoom;

        /// <summary>
        /// Contains the latest list of rooms currently available on the server. Usually emitted in response to a discovery request.
        /// </summary>
        public RoomsAvailableEvent OnRoomsAvailable;

        public IEnumerable<PeerInfo> Peers
        {
            get
            {
                return peers.Values;
            }
        }

        private void Reset()
        {
            servers = new ConnectionDefinition[]
            {
                new ConnectionDefinition()
                {
                    send_to_ip = "",
                    send_to_port = "",
                    type = ConnectionType.tcp_client
                }
            };
        }

        public bool joinedRoom
        {
            get
            {
                return Room != null && Room.UUID != null && Room.UUID != "";
            }
        }

        private void Awake()
        {
            if (OnJoinedRoom == null)
            {
                OnJoinedRoom = new UnityEvent();
            }
            if (OnJoinRejected == null)
            {
                OnJoinRejected = new RejectedEvent();
            }
            if (OnPeer == null)
            {
                OnPeer = new PeerEvent();
            }
            if (OnPeerRemoved == null)
            {
                OnPeerRemoved = new PeerEvent();
            }
            if (OnRoomsAvailable == null)
            {
                OnRoomsAvailable = new RoomsAvailableEvent();
            }

            blobCallbacks = new Dictionary<string, Action<string>>();

            Room = new RoomInterface();
            peers = new Dictionary<string, PeerInfo>();
            Available = new List<RoomInfo>();

            OnJoinedRoom.AddListener(() => Debug.Log("Joined Room " + Room.Name));
            OnRoomsAvailable.AddListener((rooms) => Available = rooms);

            Me = new PeerInterface(Guid.NewGuid().ToString());
        }

        private void Start()
        {
            context = NetworkScene.Register(this);

            (Me as IPeerInterfaceFriend).SetNetworkContext(context);

            foreach (var item in servers)
            {
                Connect(item);
            }
        }

        // The room joining process occurs in two steps: id aquisition and then room join. This is to avoid a race condition whereby
        // another peer may be informed of this peer before this peer has updated its id.

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var container = JsonUtility.FromJson<Message>(message.ToString());
            switch (container.type)
            {
                case "Accepted":
                    {
                        var args = JsonUtility.FromJson<AcceptedArgs>(container.args);
                        (Room as IRoomInterfaceFriend).Update(args.room);
                        peers.Clear();
                        foreach (var item in args.peers)
                        {
                            peers[item.UUID] = item;
                        }
                        OnJoinedRoom.Invoke();
                        OnRoom.Invoke();
                    }
                    break;
                case "Rejected":
                    {
                        var args = JsonUtility.FromJson<RejectedArgs>(container.args);
                        OnJoinRejected.Invoke(args);
                    }
                    break;
                case "UpdateRoom":
                    {
                        var args = JsonUtility.FromJson<RoomInfo>(container.args);
                        (Room as IRoomInterfaceFriend).Update(args);
                        OnRoom.Invoke();
                    }
                    break;
                case "UpdatePeer":
                    {
                        var peer = JsonUtility.FromJson<PeerInfo>(container.args);
                        peers[peer.UUID] = peer;
                        OnPeer.Invoke(peer);
                    }
                    break;
                case "RemovedPeer":
                    {
                        var peer = JsonUtility.FromJson<PeerInfo>(container.args);
                        peers.Remove(peer.UUID);
                        OnPeerRemoved.Invoke(peer);
                    }
                    break;
                case "Rooms":
                    {
                        var available = JsonUtility.FromJson<RoomsResponseArgs>(container.args);
                        if (roomClientVersion != available.version)
                        {
                            Debug.LogError($"Your version {roomClientVersion} of Ubiq doesn't match the server version {available.version}.");
                        }
                        OnRoomsAvailable.Invoke(available.rooms);
                    }
                    break;
                case "Blob":
                    {
                        var blob = JsonUtility.FromJson<Blob>(container.args);
                        var key = blob.GetKey();
                        if(blobCallbacks.ContainsKey(key))
                        {
                            blobCallbacks[key](blob.blob);
                            blobCallbacks.Remove(key);
                        }
                    }
                    break;
            }
        }

        private void SendToServer(string type, object argument)
        {
            context.Send(RoomServerObjectId, RoomServerComponentId, JsonUtility.ToJson(new Message(type, argument)));
        }

        /// <summary>
        /// Creates a new room with the current settings on the server, and joins it.
        /// </summary>
        public void JoinNew(string name, bool publish)
        {
            SendToServer("Join", new JoinArgs()
            {
                joincode = "", // Empty joincode means request new room
                name = name,
                publish = publish,
                peer = Me.GetPeerInfo()
            });
            (Me as IPeerInterfaceFriend).NeedsUpdate(); // This will clear the updated needed flag
        }

        public void Join(string joincode)
        {
            SendToServer("Join", new JoinArgs()
            {
                joincode = joincode,
                peer = Me.GetPeerInfo()
            });
            (Me as IPeerInterfaceFriend).NeedsUpdate();
        }

        public void Connect(ConnectionDefinition connection)
        {
            context.scene.AddConnection(Connections.Resolve(connection));
        }

        private void Update()
        {
            if((Me as IPeerInterfaceFriend).NeedsUpdate())
            {
                SendToServer("UpdatePeer", Me.GetPeerInfo());
            }
            if((Room as IRoomInterfaceFriend).NeedsUpdate())
            {
                SendToServer("UpdateRoom", Room.GetRoomInfo());
            }
        }

        public void DiscoverRooms()
        {
            SendToServer("RequestRooms", new RoomsRequestArgs());
        }

        /// <summary>
        /// Retrieves the value of a blob from a room. When the server responds a call will be made to callback.
        /// Only one callback is made per server response, regardless of how many times GetBlob was called between.
        /// If a blob does not exist, callback will be called with an empty string. (In this case, callback may issue
        /// another GetBlob call, to poll the server, if it is known that a value will eventually be set.)
        /// Blobs are by convention immutable, so it is safe to cache the result, once a valid result is returned.
        /// </summary>
        public void GetBlob(string room, string uuid, Action<string> callback)
        {
            var blob = new Blob()
            {
                room = room,
                uuid = uuid
            };
            var key = blob.GetKey();
            if(!blobCallbacks.ContainsKey(key))
            {
                blobCallbacks.Add(key, callback);
            }
            SendToServer("GetBlob", blob);
        }

        private void SetBlob(string room, string uuid, string blob) // private because this could encourage re-using uuids, which is not allowed because blobs are meant to be immutable
        {
            if (blob.Length > 0)
            {
                SendToServer("SetBlob", new Blob()
                {
                    room = room,
                    uuid = uuid,
                    blob = blob
                });
            }
        }

        /// <summary>
        /// Sets a persistent variable that exists for as long as the room does. This variable is not sent with
        /// Room Updates, but rather only when requested, making this method suitable for larger data.
        /// If the room does not exist, the data is discarded.
        /// </summary>
        public string SetBlob(string room, string blob)
        {
            var uuid = Guid.NewGuid().ToString();
            SetBlob(room, uuid, blob);
            return uuid;
        }
    }
}