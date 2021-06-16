using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Ubiq.Logging;
using Ubiq.Avatars;
using UnityEngine.Events;
using SIPSorcery.Net;
using System.Threading.Tasks;
using System.Threading;
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
        // generation seems to take a while.
        // Bandaid solution is to keep some connections in memory ready to go
        public class RTCPeerConnectionSource : IDisposable
        {
            // Debug - should come from server
            private const string STUN_URL = "stun:stun.l.google.com:19302";
            private const string TURN_URL = "turn:20.84.122.207";
            private const string TURN_USER = "ubiqtestuser";
            private const string TURN_PASS = "1rZ$aU9C^cdbstHb";

            private ConcurrentBag<Task<RTCPeerConnection>> pcTasks;

            public RTCPeerConnectionSource (int taskCount = 1)
            {
                pcTasks = new ConcurrentBag<Task<RTCPeerConnection>>();
                for (int i = 0; i < taskCount; i++)
                {
                    pcTasks.Add(Task.Run(() => Create()));
                }
            }

            public Task<RTCPeerConnection> Acquire ()
            {
                if (pcTasks.TryTake(out var pcTask))
                {
                    pcTasks.Add(Task.Run(() => Create()));
                    return pcTask;
                }

                return Task.Run(() => Create());
            }

            private RTCPeerConnection Create()
            {
                return new RTCPeerConnection(new RTCConfiguration
                {
                    iceServers = new List<RTCIceServer>
                    {
                        new RTCIceServer { urls = STUN_URL },
                        new RTCIceServer
                        {
                            urls = TURN_URL,
                            username = TURN_USER,
                            credential = TURN_PASS,
                            credentialType = RTCIceCredentialType.password
                        }
                    }
                });
            }

            public async void Dispose()
            {
                await Task.WhenAll(pcTasks).ConfigureAwait(false);

                while (pcTasks.TryTake(out var pc))
                {
                    pc.Dispose();
                }
                pcTasks = null;
            }
        }

        private VoipMicrophoneInput audioSource;
        private RoomClient client;
        private Dictionary<string, VoipPeerConnection> peerUuidToConnection;
        private NetworkContext context;

        private RTCPeerConnectionSource peerConnectionSource;

        /// <summary>
        /// Fires when a new (local) PeerConnection is created by an instance of this Component. This may be a new or replacement PeerConnection.
        /// </summary>
        /// <remarks>
        /// WebRtcPeerConnection manager is designed to create connections based on the Peers in a RoomClient's Room, so the event includes a
        /// PeerInfo struct, with information about which peer the connection is intended to reach.
        /// </remarks>
        public OnPeerConnectionEvent OnPeerConnection = new OnPeerConnectionEvent();
        public class OnPeerConnectionEvent : UnityEvent<VoipPeerConnection> {
            private VoipPeerConnectionManager owner;

            public new void AddListener(UnityAction<VoipPeerConnection> call)
            {
                base.AddListener(call);
                if (owner) {
                    foreach (var item in owner.peerUuidToConnection.Values)
                    {
                        call(item);
                    }
                }
            }

            public void SetOwner(VoipPeerConnectionManager owner)
            {
                this.owner = owner;
                foreach (var item in owner.peerUuidToConnection.Values)
                {
                    Invoke(item);
                }
            }
        }

        private EventLogger logger;

        private void Awake()
        {
            peerConnectionSource = new RTCPeerConnectionSource();
            client = GetComponentInParent<RoomClient>();
            peerUuidToConnection = new Dictionary<string, VoipPeerConnection>();
            OnPeerConnection.SetOwner(this);

            audioSource = CreateAudioSource();
            audioSource.StartAudio();
        }

        private void Start()
        {
            context = NetworkScene.Register(this);
            logger = new ContextEventLogger(context);
            client.OnJoinedRoom.AddListener(OnJoinedRoom);
            client.OnLeftRoom.AddListener(OnLeftRoom);
            client.OnPeerRemoved.AddListener(OnPeerRemoved);
        }

        // Cleanup all peers
        private void OnLeftRoom(RoomInfo room)
        {
            foreach(var pc in peerUuidToConnection.Values) {
                Destroy(pc.audioSource.gameObject);
                Destroy(pc.gameObject);
            }

            peerUuidToConnection.Clear();
        }

        private void OnDestroy()
        {
            peerConnectionSource.Dispose();
            peerConnectionSource = null;
        }

        // It is the responsibility of the new peer (the one joining the room) to begin the process of creating a peer connection,
        // and existing peers to accept that connection.
        // This is because we need to know that the remote peer is established, before beginning the exchange of messages.
        private void OnJoinedRoom(RoomInfo room)
        {
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

        private void OnPeerRemoved(PeerInfo peer)
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
            var audioSource = new GameObject("WebRTC Microphone Input")
                .AddComponent<VoipMicrophoneInput>();

            audioSource.transform.SetParent(transform);
            return audioSource;
        }

        private VoipPeerConnection CreatePeerConnection(NetworkId objectid,
            string peerUuid, bool polite)
        {
            var name = objectid.ToString();

            var audioSink = new GameObject("WebRTC Audio Output + " + name)
                .AddComponent<VoipAudioSourceOutput>();

            var pc = new GameObject("WebRTC Peer Connection " + name)
                .AddComponent<VoipPeerConnection>();

            pc.transform.SetParent(transform);

            // The audiosink can be made 3d and moved around by event listeners
            // but for now, make it a child to avoid cluttering scene graph
            audioSink.transform.SetParent(pc.transform);

            pc.Setup(objectid,peerUuid,polite,audioSource,audioSink,peerConnectionSource.Acquire());

            peerUuidToConnection.Add(peerUuid, pc);
            OnPeerConnection.Invoke(pc);

            return pc;
        }

        // private void OnPeerConnectionStateChanged (SIPSorcery.Net.RTCPeerConnectionState _)
        // {
        //     var useAudioSource = false;
        //     foreach (var connection in peerUuidToConnection.Values)
        //     {
        //         if (connection.peerConnectionState == SIPSorcery.Net.RTCPeerConnectionState.connected)
        //         {
        //             useAudioSource = true;
        //             break;
        //         }
        //     }

        //     if (useAudioSource)
        //     {
        //         audioSource.StartAudio();
        //     }
        //     else
        //     {
        //         audioSource.CloseAudio();
        //     }
        // }
        public void Send(NetworkId sharedId, Message m)
        {
            context.SendJson(sharedId, m);
        }
    }
}