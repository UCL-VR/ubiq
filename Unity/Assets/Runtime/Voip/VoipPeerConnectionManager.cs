using System;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Ubiq.Logging;
using Ubiq.Extensions;
using System.Threading.Tasks;
using Ubiq.Voip.Implementations;

namespace Ubiq.Voip
{
    /// <summary>
    /// Manages the lifetime of WebRtc Peer Connection objects with respect to changes in the room
    /// </summary>
    public class VoipPeerConnectionManager : MonoBehaviour
    {
        [Serializable]
        private class IceServerDetailsCollection
        {
            public List<IceServerDetails> servers = new List<IceServerDetails>();
        }

        private Dictionary<string, VoipPeerConnection> peerUuidToConnection = new Dictionary<string, VoipPeerConnection>();

        private string prevIceServersString;
        private IceServerDetailsCollection iceServers = new IceServerDetailsCollection();

        public class OnPeerConnectionEvent : ExistingListEvent<VoipPeerConnection> {}

        /// <summary>
        /// Fires when a new (local) PeerConnection is created by an instance of this Component. This may be a new or replacement PeerConnection.
        /// When a new listener is added it is automatically fired for any existing connections that may have been created before it was joined.
        /// </summary>
        /// <remarks>
        /// WebRtcPeerConnection manager is designed to create connections based on the Peers in a RoomClient's Room, so the event includes a
        /// PeerInfo struct, with information about which peer the connection is intended to reach.
        /// </remarks>
        public OnPeerConnectionEvent OnPeerConnection = new OnPeerConnectionEvent();

        private RoomClient client;
        private NetworkId serviceId = new NetworkId("c994-0768-d7b7-171c"); // The unique service Id
        private LogEmitter logger;
        private NetworkScene networkScene;

        private void Awake()
        {
            OnPeerConnection.SetExisting(peerUuidToConnection.Values);
        }

        protected void Start()
        {
            networkScene = NetworkScene.Find(this);
            var id = NetworkId.Create(networkScene.Id, serviceId);
            NetworkScene.Register(this, id);
            logger = new NetworkEventLogger(id, networkScene, this);
            client = GetComponentInParent<RoomClient>();
            client.OnPeerAdded.AddListener(OnPeerAdded);
            client.OnPeerRemoved.AddListener(OnPeerRemoved);
            client.OnJoinedRoom.AddListener(OnJoinedRoom);
            client.OnRoomUpdated.AddListener(OnRoomUpdated);
        }

        protected void OnDestroy()
        {
            if (client)
            {
                client.OnPeerAdded.RemoveListener(OnPeerAdded);
                client.OnPeerRemoved.RemoveListener(OnPeerRemoved);
                client.OnJoinedRoom.RemoveListener(OnJoinedRoom);
                client.OnRoomUpdated.RemoveListener(OnRoomUpdated);
            }
        }

        private void UpdateIceServerCollection (IRoom room)
        {
            // Update ice server list and emit event
            var iceServersString = room["ice-servers"];

            // We're only interested if ice servers has changed
            if (iceServersString != null
                && iceServersString != prevIceServersString)
            {
                JsonUtility.FromJsonOverwrite(iceServersString,iceServers);
                prevIceServersString = iceServersString;
            }
        }

        private void OnRoomUpdated(IRoom room)
        {
            UpdateIceServerCollection(room);
        }

        // It is the responsibility of the new peer (the one joining the room) to begin the process of creating a peer connection,
        // and existing peers to accept that connection.
        // This is because we need to know that the remote peer is established, before beginning the exchange of messages.
        private void OnJoinedRoom(IRoom room)
        {
            UpdateIceServerCollection (room);
        }

        private void OnPeerAdded(IPeer peer)
        {
            if (peer.uuid == client.Me.uuid)
            {
                return; // Don't connect to ones self!
            }

            if (!peerUuidToConnection.ContainsKey(peer.uuid))
            {
                // This is a new Peer. OnPeerAdded will be received by both sides.
                // All else being equal, the client with the highest UUID is
                // responsible for initiating the connection.

                if (String.Compare(client.Me.uuid, peer.uuid) > 0) // If my uuid is greater then theirs. This will be inverted on the other side.
                {
                    var pcid = NetworkId.Unique(); // A single audio channel exists between two peers. Each audio channel has its own Id.

                    logger.Log("CreatePeerConnectionForPeer", pcid, peer.uuid);

                    CreatePeerConnection(pcid, peer.uuid, true);

                    Message m;
                    m.type = "RequestPeerConnection";
                    m.networkId = pcid; // the shared Id is set by this peer, but it must be chosen so as not to conflict with any other shared id on the network
                    m.uuid = client.Me.uuid; // this is so the other end can identify us if we are removed from the room

                    networkScene.SendJson(NetworkId.Create(peer.networkId, serviceId), m); // Send a message to the Peer's VoipPeerConnectionManager

                    logger.Log("RequestPeerConnection", pcid, peer.uuid);
                }
            }
        }

        private void OnPeerRemoved(IPeer peer)
        {
            if (peerUuidToConnection.TryGetValue(peer.uuid, out var connection))
            {
                Destroy(connection.gameObject);
                peerUuidToConnection.Remove(peer.uuid);
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = JsonUtility.FromJson<Message>(message.ToString());
            switch (msg.type)
            {
                case "RequestPeerConnection":
                    logger.Log("CreatePeerConnectionForRequest", msg.networkId);
                    CreatePeerConnection(msg.networkId, msg.uuid, polite: false);
                    break;
            }
        }

        [Serializable]
        public struct Message
        {
            public string type;
            public NetworkId networkId;
            public string uuid;
        }

        private VoipPeerConnection CreatePeerConnection(NetworkId networkId,
            string peerUuid, bool polite)
        {
            var pc = new GameObject("Voip Peer Connection " + peerUuid).AddComponent<VoipPeerConnection>();
            pc.transform.SetParent(transform);

            pc.Setup(networkId,networkScene,peerUuid,polite,iceServers.servers);

            peerUuidToConnection.Add(peerUuid, pc);
            OnPeerConnection.Invoke(pc);

            return pc;
        }

        /// <summary>
        /// Gets the PeerConnection for a remote peer with the given UUID. This method will invoke then as
        /// many times as a new VoipPeerConnection is created, which may be more than once if the connection
        /// is re-established. It may also never invoke if a connection is never created.
        /// </summary>
        /// <param name="PeerUUID"></param>
        /// <returns></returns>
        public void GetPeerConnectionAsync(string peerUUID, Action<VoipPeerConnection> then)
        {
            OnPeerConnection.AddListener((pc) =>
            {
                if (pc.peerUuid == peerUUID)
                {
                    then.Invoke(pc);
                }
            }, true);
        }

        /// <summary>
        /// Gets the PeerConnection for a remote peer with the given UUID on the closest VoipPeerConnectionManager to
        /// component. This method will invoke then as many times as a new VoipPeerConnection is created, which may be
        /// more than once if the connection is re-established. It may also never invoke if a connection is never created,
        /// or there is no VoipPeerConnectionManger in the scene.
        /// </summary>
        public static void GetPeerConnectionAsync(MonoBehaviour component, string peerUUID, Action<VoipPeerConnection> then)
        {
            var manager = Find(component);
            if(manager)
            {
                manager.GetPeerConnectionAsync(peerUUID, then);
            }
        }

        /// <summary>
        /// Gets the PeerConnection for a remote peer with the given UUID.
        /// </summary>
        /// <returns>The peer connection object, or null if it has not been established</returns>
        public VoipPeerConnection GetPeerConnection(string peerUUID)
        {
            if(peerUuidToConnection.TryGetValue(peerUUID, out var pc))
            {
                return pc;
            }
            return null;
        }

        /// <summary>
        /// Gets the PeerConnection for a remote peer with the given UUID on the closest VoipPeerConnectionManager to
        /// component.
        /// </summary>
        /// <returns>The peer connection object, or null if it has not been established</returns>
        public static VoipPeerConnection GetPeerConnection(MonoBehaviour component, string peerUUID)
        {
            var manager = Find(component);
            if(manager && manager.peerUuidToConnection.TryGetValue(peerUUID, out var pc))
            {
                return pc;
            }
            return null;
        }

        /// <summary>
        /// Find the VoipConnectionManager for forest the Component is a member of. May return null if there is no Voip manager for the scene.
        /// </summary>
        public static VoipPeerConnectionManager Find(MonoBehaviour Component)
        {
            try
            {
                return NetworkScene.Find(Component).GetComponentInChildren<VoipPeerConnectionManager>();
            }
            catch
            {
                return null;
            }
        }
    }
}