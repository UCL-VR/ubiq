using System;
using System.Collections.Generic;
using Ubiq.Networking;
using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Dictionaries;
using Ubiq.XR.Notifications;
using System.Linq;
using Ubiq.Rooms.Messages;

namespace Ubiq.Rooms
{
    /// <summary>
    /// Facilitates joining and working with Rooms via a RoomServer somewhere on the Network.
    /// The Rooms system provides the concept of Peers as Remote Players, with their own sets of
    /// Components and properties.
    /// With the use of a server, RoomClient can join a network with other Peers.
    /// </summary>
    [RequireComponent(typeof(NetworkScene))]
    public class RoomClient : NetworkBehaviour
    {
        [Serializable]
        private class Property
        {
            public string key;
            public string value;
        }

        [Serializable]
        private class PeerArgs
        {
            public string uuid;
            public NetworkId networkId;
            public Property[] properties;
        }

        [Serializable]
        private class PeerAddedArgs : PeerArgs { }

        [Serializable]
        private class PeerRemovedArgs
        {
            public string uuid;
        }

        [Serializable]
        private class RoomPropertySetArgs
        {
            public string[] keys;
            public string[] values;
        }

        [Serializable]
        private class PeerPropertySetArgs
        {
            public string uuid;
            public string[] keys;
            public string[] values;
        }


        public IRoom Room { get => room; }

        /// <summary>
        /// A reference to the Peer that represents this local player. Me will be the same reference through the life of the RoomClient.
        /// It is valid after Awake() (can be used in Start()).
        /// </summary>
        public ILocalPeer Me { get => me; }

        /// <summary>
        /// The Session Id identifies a persistent connection to a RoomServer. If the Session Id returned by the Room Server changes,
        /// it indicates that the RoomClient/RoomServer have become desynchronised and the client must rejoin.
        /// </summary>
        public string SessionId { get; private set; }

        /// <summary>
        /// Emitted when a Peer appears in the Room for the first time.
        /// </summary>
        public PeerEvent OnPeerAdded = new PeerEvent();

        /// <summary>
        /// Emitted when the properties of a Peer change.
        /// </summary>
        public PeerUpdatedEvent OnPeerUpdated = new PeerUpdatedEvent();

        /// <summary>
        /// Emitted when a Peer goes out of scope (e.g. it leaves the room)
        /// </summary>
        public PeerEvent OnPeerRemoved = new PeerEvent();

        /// <summary>
        /// Emitted when this peer has joined a room
        /// </summary>
        /// <remarks>
        /// There is no OnLeftRoom equivalent; leaving a room is the same as joining a new, empty room.
        /// </remarks>
        public RoomEvent OnJoinedRoom = new RoomEvent();

        /// <summary>
        /// Emitted when a Room this peer is a member of has updated its properties
        /// </summary>
        public RoomEvent OnRoomUpdated = new RoomEvent();

        /// <summary>
        /// Emitted when this peer attempts to join a room and is rejected
        /// </summary>
        public RejectedEvent OnJoinRejected = new RejectedEvent();

        /// <summary>
        /// Emitted in response to a discovery request
        /// </summary>
        public RoomsDiscoveredEvent OnRoomsDiscovered = new RoomsDiscoveredEvent();

        /// <summary>
        /// A list of all the Peers in the Room. This does not include the local Peer, Me.
        /// </summary>
        public IEnumerable<IPeer> Peers
        {
            get
            {
                return peers.Values;
            }
        }

        /// <summary>
        /// A list of default servers to connect to on start-up. The network must only have one RoomServer or
        /// undefined behaviour will result.
        /// </summary>
        [SerializeField]
        private ConnectionDefinition[] servers;

        /// <summary>
        /// The version the client defines which message schema it supports. Servers must match that schema
        /// in order to be able to communicate with it.
        /// </summary>
        private const string roomClientVersion = "0.0.4";

        private NetworkId roomServerObjectId = new NetworkId(1);
        private Dictionary<string, Action<string>> blobCallbacks;
        private float pingSent;
        private float pingReceived;
        private float heartbeatReceived => Time.realtimeSinceStartup - pingReceived;
        private float heartbeatSent => Time.realtimeSinceStartup - pingSent;
        public static float HeartbeatTimeout = 5f;
        public static float HeartbeatInterval = 1f;
        private float lastGetRoomsTime;
        private PeerInterfaceFriend me;
        private RoomInterfaceFriend room;

        private List<Action> actions;

        /// <summary>
        /// Contains the current Peers, indexed by UUID
        /// </summary>
        private Dictionary<string, PeerInterfaceFriend> peers;

        public class TimeoutNotification : Notification
        {
            private RoomClient client;

            public TimeoutNotification(RoomClient client)
            {
                this.client = client;
            }

            public override string Message
            {
                get
                {
                    return $"No Connection ({ client.heartbeatReceived.ToString("0") } seconds ago)";
                }
            }
        }

        private TimeoutNotification notification;

        private class PropertyLog
        {
            private List<string> keys = new List<string>();
            private List<string> values = new List<string>();

            public void Add (string key, string value)
            {
                keys.Add(key);
                values.Add(value);
            }

            public bool TryPop (out string key, out string value)
            {
                if (Count > 0)
                {
                    key = keys[keys.Count-1];
                    value = values[values.Count-1];
                    keys.RemoveAt(keys.Count-1);
                    values.RemoveAt(values.Count-1);
                    return true;
                }
                key = string.Empty;
                value = string.Empty;
                return false;
            }

            public int Count => keys.Count;
        }

        private class RoomInterfaceFriend : IRoom
        {
            public string Name { get; protected set; }
            public string UUID { get; protected set; }
            public string JoinCode { get; protected set; }
            public bool Publish { get; protected set; }

            private PropertyLog log = new PropertyLog();
            public int PropertyLogCount => log.Count;

            private PropertyCollection properties = new PropertyCollection();

            public string this[string key]
            {
                get => properties[key];
                set
                {
                    // Record that the request was made but do not update
                    // properties until we hear back from the server
                    log.Add(key,value);
                }
            }

            public bool TryPopPropertyLog (out string key, out string value)
            {
                return log.TryPop(out key, out value);
            }

            public void ForceSetProperty(string key, string value)
            {
                properties[key] = value;
            }

            // // The IsUpdated() method has side effects (in that the flag will be cleared) so only allow calling it from inside RoomClient
            // public bool NeedsUpdate()
            // {
            //     if (properties != null)
            //     {
            //         return properties.IsUpdated();
            //     }
            //     else
            //     {
            //         return false;
            //     }
            // }

            // public bool Update(RoomInfo args)
            // {
            //     Name = args.Name;
            //     UUID = args.UUID;
            //     JoinCode = args.JoinCode;
            //     Publish = args.Publish;
            //     properties = new SerializableDictionary(args); // todo: merge the dictionaries properly
            //     return true;
            // }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return properties.GetEnumerator();
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return properties.GetEnumerator();
            }

            // public RoomInfo GetRoomInfo()
            // {
            //     return new RoomInfo(Name, UUID, JoinCode, Publish, properties);
            // }
        }

        /// <summary>
        /// Internal peer interface, used for all types of peers. When peer
        /// objects leave this class they are cast to the correct subclass.
        /// </summary>
        private interface IInternalPeer : ILocalPeer {}

        private class PeerInterfaceFriend : IInternalPeer
        {
            public NetworkId networkId { get; set; }
            public string uuid { get; set; }

            public event Action<string,string> propertySet = delegate {};
            private PropertyLog log = new PropertyLog();

            private PropertyCollection properties = new PropertyCollection();

            public PeerInterfaceFriend(string uuid)
            {
                this.uuid = uuid;
            }

            public PeerInterfaceFriend(PeerInfo info)
            {
                uuid = info.uuid;
                // properties = info.properties;
                networkId = info.networkId;
            }

            public string this[string key]
            {
                get => properties[key];
                set
                {
                    properties[key] = value;
                    propertySet(key,value);
                    log.Add(key,value);
                }
            }

            public int PropertyLogCount => log.Count;

            public bool TryPopPropertyLog (out string key, out string value)
            {
                return log.TryPop(out key, out value);
            }


            public void SetPropertySuppressEvent(string key, string value)
            {
                properties[key] = value;
            }

            // public bool NeedsUpdate()
            // {
            //     return properties.IsUpdated();
            // }

            // public PeerInfo GetPeerInfo()
            // {
            //     return new PeerInfo(uuid, networkId, properties);
            // }

            // public bool Update(PeerInfo info)
            // {
            //     return properties.Update(info.properties);
            // }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return properties.GetEnumerator();
            }

            public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
            {
                return properties.GetEnumerator();
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

        public bool JoinedRoom
        {
            get
            {
                return Room != null && Room.UUID != null && Room.UUID != ""; // Null check because this may be called in the Editor
            }
        }

        private void Awake()
        {
            blobCallbacks = new Dictionary<string, Action<string>>();
            room = new RoomInterfaceFriend();
            peers = new Dictionary<string, PeerInterfaceFriend>();
            actions = new List<Action>();

            OnJoinedRoom.AddListener((room) => Debug.Log("Joined Room " + room.Name));

            me = new PeerInterfaceFriend(Guid.NewGuid().ToString());
            OnPeerUpdated.SetExisting(me);
        }

        protected override void Started()
        {
            me.networkId = networkId;

            foreach (var item in servers)
            {
                Connect(item);
            }
        }

        protected override void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var container = JsonUtility.FromJson<Message>(message.ToString());
            switch (container.type)
            {
                case "Rejected":
                    {
                        var args = JsonUtility.FromJson<RejectedArgs>(container.args);
                        OnJoinRejected.Invoke(new Rejection()
                        {
                            reason = args.reason,
                            uuid = args.joinArgs.uuid,
                            joincode = args.joinArgs.joincode,
                            name = args.joinArgs.name,
                            publish = args.joinArgs.publish
                        });
                    }
                    break;
                case "SetRoom":
                    {
                        var args = JsonUtility.FromJson<SetRoom>(container.args);
                        room.Update(args.room);

                        var newPeerUuids = args.peers.Select(x => x.uuid);
                        var peersToRemove = new List<string>();
                        foreach (var peer in peers.Keys)
                        {
                            if (!newPeerUuids.Contains(peer))
                            {
                                peersToRemove.Add(peer);
                            }
                        }

                        foreach (var uuid in peersToRemove)
                        {
                            var peer = peers[uuid];
                            peers.Remove(uuid);
                            OnPeerRemoved.Invoke(peer);
                        }

                        foreach (var item in args.peers)
                        {
                            if(item.uuid == me.uuid)
                            {
                                continue;
                            }

                            UpdatePeer(item);
                        }

                        OnJoinedRoom.Invoke(room);
                        OnRoomUpdated.Invoke(room);
                    }
                    break;
                case "Rooms":
                    {
                        var response = JsonUtility.FromJson<DiscoverRoomsResponse>(container.args);
                        if (roomClientVersion != response.version)
                        {
                            Debug.LogError($"Your version {roomClientVersion} of Ubiq doesn't match the server version {response.version}.");
                        }
                        var rooms = new List<IRoom>();
                        for (int i = 0; i < response.rooms.Count; i++)
                        {
                            rooms.Add((IRoom)response.rooms[i]);
                        }
                        var request = new RoomsDiscoveredRequest();
                        request.joincode = response.request.joincode;

                        OnRoomsDiscovered.Invoke(rooms,request);
                    }
                    break;
                case "PeerAdded":
                    {
                        var args = JsonUtility.FromJson<PeerAddedArgs>(container.args);
                        if (!peers.ContainsKey(args.peer.uuid))
                        {
                            var peer = new PeerInterfaceFriend(args.peer);
                            peers.Add(peer.uuid, peer);
                            OnPeerAdded.Invoke(peer);
                        }
                    }
                    break;
                case "PeerRemoved":
                    {
                        var args = JsonUtility.FromJson<PeerRemovedArgs>(container.args);
                        if (peers.TryGetValue(args.uuid,out var peer))
                        {
                            peers.Remove(args.uuid);
                            OnPeerRemoved.Invoke(peer);
                        }
                    }
                    break;
                case "RoomPropertySet":
                    {
                        var args = JsonUtility.FromJson<RoomPropertySetArgs>(container.args);
                        for (int i = 0; i < args.keys.Count; i++)
                        {
                            room.ForceSetProperty(args.keys[i],args.values[i]);
                        }
                        OnRoomUpdated.Invoke(room);
                    }
                    break;
                case "PeerPropertySet":
                    {
                        var args = JsonUtility.FromJson<PeerPropertySetArgs>(container.args);
                        if (peers.TryGetValue(args.peerUuid,out var peer))
                        {
                            for (int i = 0; i < args.keys.Count; i++)
                            {
                                peer.SetPropertySuppressEvent(args.keys[i],args.values[i]);
                            }
                        }

                        OnPeerUpdated.Invoke(peer);
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
                case "Ping":
                    {
                        pingReceived = Time.realtimeSinceStartup;
                        PlayerNotifications.Delete(ref notification);
                        var response = JsonUtility.FromJson<PingResponse>(container.args);
                        OnPingResponse(response);
                    }
                    break;
            }
        }

        private void UpdatePeer(PeerInfo item)
        {
            PeerInterfaceFriend peer;

            if (item.uuid == me.uuid)
            {
                // We've already sent the event before updating the server
                return;
            }
            else if (peers.TryGetValue(item.uuid, out peer))
            {
                if (peer.Update(item))
                {
                    OnPeerUpdated.Invoke(peer);
                }
            }
            else
            {
                peer = new PeerInterfaceFriend(item);
                peers.Add(item.uuid, peer);
                OnPeerAdded.Invoke(peer);
            }
        }

        private void SendToServer(string type, object argument)
        {
            networkScene.SendJson(roomServerObjectId, new Message(type, argument));
        }

        /// <summary>
        /// Create a new room with the specified name and visibility
        /// </summary>
        /// <param name="name">The name of the new room</param>
        /// <param name="bool">Whether others should be able to browse for this room</param>
        public void Join(string name, bool publish)
        {
            actions.Add(() =>
            {
                SendToServer("Join", new JoinRequest()
                {
                    name = name,
                    publish = publish,
                    peer = me.GetPeerInfo()
                });
                me.NeedsUpdate(); // This will clear the updated needed flag
            });
        }

        /// <summary>
        /// Joins an existing room using a join code.
        /// </summary>
        public void Join(string joincode)
        {
            actions.Add(() =>
            {
                SendToServer("Join", new JoinRequest()
                {
                    joincode = joincode,
                    peer = me.GetPeerInfo()
                });
                me.NeedsUpdate();
            });
        }

        /// <summary>
        /// Joins an existing room using a join code.
        /// </summary>
        public void Join(Guid guid)
        {
            actions.Add(() =>
            {
                SendToServer("Join", new JoinRequest()
                {
                    uuid = guid.ToString(),
                    peer = me.GetPeerInfo()
                });
                me.NeedsUpdate();
            });
        }

        /// <summary>
        /// Creates a new connection on the Network Scene
        /// </summary>
        /// <remarks>
        /// RoomClient is one of a few components able to create new connections. Usually it will be user code that makes such connections.
        /// </remarks>
        public void Connect(ConnectionDefinition connection)
        {
            networkScene.AddConnection(Connections.Resolve(connection));
        }

        private SetPropertiesRequest setPropertiesRequest = new SetPropertiesRequest();

        private void Update()
        {
            actions.ForEach(a => a());
            actions.Clear();

            if (me.PropertyLogCount > 0)
            {
                setPropertiesRequest.keys.Clear();
                setPropertiesRequest.values.Clear();
                while (me.TryPopPropertyLog(out var key, out var value))
                {
                    setPropertiesRequest.keys.Add(key);
                    setPropertiesRequest.values.Add(value);
                }

                SendToServer("SetPeerProperty", setPropertiesRequest);
            }

            if (room.PropertyLogCount > 0)
            {
                setPropertiesRequest.keys.Clear();
                setPropertiesRequest.values.Clear();
                while (room.TryPopPropertyLog(out var key, out var value))
                {
                    setPropertiesRequest.keys.Add(key);
                    setPropertiesRequest.values.Add(value);
                }

                SendToServer("SetRoomProperty", setPropertiesRequest);
            }

            if (heartbeatSent > HeartbeatInterval)
            {
                Ping();
            }

            if (heartbeatReceived > HeartbeatTimeout)
            {
                if (notification == null)
                {
                    notification = PlayerNotifications.Show(new TimeoutNotification(this));
                }
            }
        }

        /// <summary>
        /// </summary>
        public void DiscoverRooms(string joincode = "")
        {
            var request = new DiscoverRoomsRequest();
            request.networkId = networkId;
            request.joincode = joincode;
            SendToServer("DiscoverRooms", request);
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
            var request = new GetBlobRequest()
            {
                networkId = networkId,
                blob = blob
            };
            SendToServer("GetBlob", request);
        }

        private void SetBlob(string room, string uuid, string blob) // private because this could encourage re-using uuids, which is not allowed because blobs are meant to be immutable
        {
            if (blob.Length > 0)
            {
                SendToServer("SetBlob", new SetBlobRequest()
                {
                    blob = new Blob()
                    {
                        room = room,
                        uuid = uuid,
                        blob = blob
                    }
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

        public void Ping()
        {
            pingSent = Time.realtimeSinceStartup;
            SendToServer("Ping", new PingRequest() { networkId = networkId });
        }

        private void OnPingResponse(PingResponse ping)
        {
            if(SessionId != ping.sessionId && SessionId != null)
            {
                Leave(); // The RoomClient has re-established connectivity with the RoomServer, but under a different state. So, leave the room and let the user code re-establish any state.
            }

            SessionId = ping.sessionId;
        }
    }
}