using System;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Ubiq.Logging;
using UnityEngine.Events;
using SIPSorcery.Net;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace Ubiq.Voip
{
    /// <summary>
    /// Manages the lifetime of WebRtc Peer Connection objects with respect to changes in the room
    /// </summary>
    [NetworkComponentId(typeof(VoipPeerConnectionManager), 50)]
    public class VoipPeerConnectionManager : MonoBehaviour, INetworkComponent
    {
        // SipSorcery peer connections are slow to instantiate as cert
        // generation seems to take a while. This at least allows that to happen
        // on a separate thread.
        private class RTCPeerConnectionSource
        {
            private List<RTCIceServer> iceServers = new List<RTCIceServer>();

            public void ClearIceServers ()
            {
                iceServers.Clear();
            }

            public void AddIceServer (string uri, string username = "",
                string pass = "")
            {
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(pass))
                {
                    iceServers.Add(new RTCIceServer {
                        urls = uri
                    });
                }
                else
                {
                    iceServers.Add(new RTCIceServer {
                        urls = uri,
                        username = username,
                        credential = pass,
                        credentialType = RTCIceCredentialType.password
                    });
                }
            }

            public Task<RTCPeerConnection> Acquire ()
            {
                return Task.Run(() => Create());
            }

            private RTCPeerConnection Create()
            {
                return new RTCPeerConnection(new RTCConfiguration
                {
                    iceServers = new List<RTCIceServer>(iceServers),
                });
            }
        }

        [Serializable]
        private class IceServerDetails
        {
            public string uri;
            public string username;
            public string password;

            public IceServerDetails(string uri, string username, string password)
            {
                this.uri = uri;
                this.username = username;
                this.password = password;
            }
        }

        [Serializable]
        private class IceServerDetailsCollection
        {
            public List<IceServerDetails> servers = new List<IceServerDetails>();
        }

        private VoipMicrophoneInput audioSource;
        private RoomClient client;
        private Dictionary<string, VoipPeerConnection> peerUuidToConnection;
        private NetworkContext context;

        private RTCPeerConnectionSource peerConnectionSource;

        private string prevIceServersString;
        private IceServerDetailsCollection iceServers;

        public class OnPeerConnectionEvent : ExistingListEvent<VoipPeerConnection>
        {
        }

        /// <summary>
        /// Fires when a new (local) PeerConnection is created by an instance of this Component. This may be a new or replacement PeerConnection.
        /// When a new listener is added it is automatically fired for any existing connections that may have been created before it was joined.
        /// </summary>
        /// <remarks>
        /// WebRtcPeerConnection manager is designed to create connections based on the Peers in a RoomClient's Room, so the event includes a
        /// PeerInfo struct, with information about which peer the connection is intended to reach.
        /// </remarks>
        public OnPeerConnectionEvent OnPeerConnection = new OnPeerConnectionEvent();

        private EventLogger logger;

        private void Awake()
        {
            peerConnectionSource = new RTCPeerConnectionSource();
            client = GetComponentInParent<RoomClient>();
            peerUuidToConnection = new Dictionary<string, VoipPeerConnection>();
            OnPeerConnection.SetExisting(peerUuidToConnection.Values);

            audioSource = CreateAudioSource();
            audioSource.StartAudio();
        }

        private void Start()
        {
            context = NetworkScene.Register(this);
            logger = new ContextEventLogger(context);
            client.OnJoinedRoom.AddListener(OnJoinedRoom);
            client.OnPeerRemoved.AddListener(OnPeerRemoved);
            client.OnRoomUpdated.AddListener(OnRoomUpdated);
        }

        private void OnDestroy()
        {
            if (client)
            {
                client.OnJoinedRoom.RemoveListener(OnJoinedRoom);
                client.OnPeerRemoved.RemoveListener(OnPeerRemoved);
                client.OnRoomUpdated.RemoveListener(OnRoomUpdated);
            }
            peerConnectionSource = null;
        }

        private void UpdateIceServerCollection (IRoom room)
        {
            // Update ice server list and emit event
            var iceServersString = room["ice-servers"];

            // We're only interested if ice servers has changed
            if (iceServersString != null
                && iceServersString != prevIceServersString)
            {
                // Allocates, but this shouldn't happen frequently
                iceServers = JsonUtility.FromJson<IceServerDetailsCollection>(iceServersString);

                peerConnectionSource.ClearIceServers();
                for (int i = 0; i < iceServers.servers.Count; i++)
                {
                    var server = iceServers.servers[i];
                    peerConnectionSource.AddIceServer(server.uri,server.username,server.password);
                }

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

            foreach (var peer in client.Peers)
            {
                if (peer.UUID == client.Me.UUID)
                {
                    continue; // Don't connect to ones self!
                }

                if(peerUuidToConnection.ContainsKey(peer.UUID))
                {
                    continue; // This peer existed in the previous room and we already have a connection to it
                }

                var pcid = NetworkScene.GenerateUniqueId(); //A single audio channel exists between two peers. Each audio channel has its own Id.

                logger.Log("CreatePeerConnectionForPeer", pcid, peer.NetworkObjectId);

                var pc = CreatePeerConnection(pcid, peer.UUID, polite: true);

                Message m;
                m.type = "RequestPeerConnection";
                m.objectid = pcid; // the shared Id is set by this peer, but it must be chosen so as not to conflict with any other shared id on the network
                m.uuid = client.Me.UUID; // this is so the other end can identify us if we are removed from the room
                Send(peer.NetworkObjectId, m);
                logger.Log("RequestPeerConnection", pcid, peer.NetworkObjectId);
            }
        }

        private void OnPeerRemoved(IPeer peer)
        {
            if (peerUuidToConnection.TryGetValue(peer.UUID, out var connection))
            {
                // Audiosinks are created per connection
                Destroy(connection.audioSink.gameObject);
                Destroy(connection.gameObject);
                peerUuidToConnection.Remove(peer.UUID);
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = JsonUtility.FromJson<Message>(message.ToString());
            switch (msg.type)
            {
                case "RequestPeerConnection":
                    logger.Log("CreatePeerConnectionForRequest", msg.objectid);
                    var pc = CreatePeerConnection(msg.objectid, msg.uuid, polite: false);
                    break;
            }
        }

        [Serializable]
        public struct Message
        {
            public string type;
            public NetworkId objectid;
            public string uuid;
        }

        private VoipMicrophoneInput CreateAudioSource()
        {
            var audioSource = new GameObject("Voip Microphone Input")
                .AddComponent<VoipMicrophoneInput>();

            audioSource.transform.SetParent(transform);
            return audioSource;
        }

        private VoipPeerConnection CreatePeerConnection(NetworkId objectid,
            string peerUuid, bool polite)
        {
            var audioSink = new GameObject("Voip Audio Output + " + peerUuid)
                .AddComponent<VoipAudioSourceOutput>();

            var pc = new GameObject("Voip Peer Connection " + peerUuid)
                .AddComponent<VoipPeerConnection>();

            pc.transform.SetParent(transform);

            // The audiosink can be made 3d and moved around by event listeners
            // but here make it a child to avoid cluttering scene graph
            audioSink.transform.SetParent(pc.transform);

            pc.Setup(objectid,peerUuid,polite,audioSource,audioSink,peerConnectionSource.Acquire());

            peerUuidToConnection.Add(peerUuid, pc);
            OnPeerConnection.Invoke(pc);

            return pc;
        }

        public void Send(NetworkId sharedId, Message m)
        {
            context.SendJson(sharedId, m);
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
                if (pc.PeerUuid == peerUUID)
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
                return NetworkScene.FindNetworkScene(Component).GetComponentInChildren<VoipPeerConnectionManager>();
            }
            catch
            {
                return null;
            }
        }
    }
}