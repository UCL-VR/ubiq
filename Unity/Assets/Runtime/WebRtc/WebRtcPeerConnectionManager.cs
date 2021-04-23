using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Ubiq.Logging;

namespace Ubiq.WebRtc
{
    /// <summary>
    /// Manages the lifetime of WebRtc Peer Connection objects with respect to changes in the room
    /// </summary>
    [NetworkComponentId(typeof(WebRtcPeerConnectionManager), 3)]
    public class WebRtcPeerConnectionManager : MonoBehaviour, INetworkComponent
    {
        private RoomClient client;
        private Dictionary<string, WebRtcPeerConnection> peerUUIDToConnection;
        private NetworkContext context;

        private EventLogger debug;

        private void Awake()
        {
            client = GetComponentInParent<RoomClient>();
            peerUUIDToConnection = new Dictionary<string, WebRtcPeerConnection>();
        }

        private void Start()
        {
            context = NetworkScene.Register(this);
            debug = new ContextEventLogger(context);
            client.OnJoinedRoom.AddListener(OnJoinedRoom);
            client.OnPeerRemoved.AddListener(OnPeerRemoved);
        }

        // It is the responsibility of the new peer (the one joining the room) to begin the process of creating a peer connection,
        // and existing peers to accept that connection.
        // This is because we need to know that the remote peer is established, before beginning the exchange of messages.

        public void OnJoinedRoom()
        {
            foreach (var peer in client.Peers)
            {
                if (peer.UUID == client.Me.UUID)
                {
                    continue; // don't connect to ones self!
                }

                var pcid = NetworkScene.GenerateUniqueId(); //A single audio channel exists between two peers. Each audio channel has its own Id.
                var pc = CreatePeerConnection(pcid, peer.UUID);
                pc.MakePolite();
                pc.AddLocalAudioSource();
                pc.stats.peer = peer.UUID;
                Message m;
                m.type = "RequestPeerConnection";
                m.objectid = pcid; // the shared Id is set by this peer, but it must be chosen so as not to conflict with any other shared id on the network
                m.uuid = client.Me.UUID; // this is so the other end can identify us if we are removed from the room
                Send(peer.NetworkObjectId, m);
                debug.Log("CreateAndRequestPeerConnection", pcid, peer.NetworkObjectId);
            }
        }

        public void OnPeerRemoved(PeerInfo peer)
        {
            try
            {
                Destroy(peerUUIDToConnection[peer.UUID].gameObject);
                peerUUIDToConnection.Remove(peer.UUID);
            }
            catch (KeyNotFoundException)
            {
                // never had this peer or already done
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = JsonUtility.FromJson<Message>(message.ToString());
            switch (msg.type)
            {
                case "RequestPeerConnection":
                    debug.Log("CreatePeerConnectionForRequest", msg.objectid);
                    var pc = CreatePeerConnection(msg.objectid, msg.uuid);
                    pc.stats.peer = msg.uuid;
                    pc.AddLocalAudioSource();
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

        private WebRtcPeerConnection CreatePeerConnection(NetworkId objectid, string uuid)
        {
            var go = new GameObject(objectid.ToString());
            go.transform.SetParent(transform);

            var pc = go.AddComponent<WebRtcPeerConnection>();
            pc.Id = objectid;

            peerUUIDToConnection.Add(uuid, pc);

            return pc;
        }

        public void Send(NetworkId sharedId, Message m)
        {
            context.SendJson(sharedId, m);
        }
    }
}