using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Networking;
using System.Linq;

public class Rooms 
{
    private static ConnectionDefinition server = new ConnectionDefinition("nexus.cs.ucl.ac.uk:8007");
    private static float timeout = 10f;

    /// <summary>
    /// Convenience class to create a Peer (in a forest, so many of them can be 
    /// instantiated) for testing the RoomClient.
    /// </summary>
    public class Peer
    {
        public Peer()
        {
            var forest = new GameObject("Forest");
            var peer = new GameObject("Peer");
            peer.transform.parent = forest.transform;
            scene = peer.AddComponent<NetworkScene>();
            client = peer.AddComponent<RoomClient>();
            client.SetDefaultServer(server);

            client.OnJoinedRoom.AddListener((room) =>
            {
                hasJoined = true;
            });
        }

        public NetworkScene scene;
        public RoomClient client;

        public bool hasJoined = false;
    }


    [UnityTest]
    public IEnumerator JoinRoom()
    {
        float time = 0;

        var room = System.Guid.NewGuid();

        var peer1 = new Peer();
        peer1.client.Join(room);

        while (!peer1.hasJoined && CheckTimeout(ref time, "Joining Peer 1"))
        {
            yield return null;
        }

        Assert.AreEqual(peer1.client.Peers.Count(), 0); // Peers list should not include me.

        var peer2 = new Peer();
        peer2.client.Join(room);

        while (!peer2.hasJoined && CheckTimeout(ref time, "Joining Peer 2"))
        {
            yield return null;
        }

        Assert.AreEqual(peer2.client.Peers.Count(), 1);

        var peer1AsSeenByPeer2 = peer2.client.Peers.First();

        Assert.AreEqual(peer1.scene.Id, peer1AsSeenByPeer2.networkId);
        Assert.AreEqual(peer1.client.Me.uuid, peer1AsSeenByPeer2.uuid);
    }

    [UnityTest]
    public IEnumerator DiscoverRooms()
    {
        float time = 0;

        string publicRoomName = System.Guid.NewGuid().ToString();

        var peer1 = new Peer();
        peer1.client.Join(publicRoomName, true);

        while (!peer1.hasJoined && CheckTimeout(ref time, "Joining Peer 1"))
        {
            yield return null;
        }

        bool complete = false;

        var peer2 = new Peer();
        peer2.client.OnRooms.AddListener((rooms, request) =>
        {
            Assert.GreaterOrEqual(rooms.Count, 1);
            Assert.IsTrue(rooms.Select(r => r.Name).Contains(publicRoomName));

            complete = true;
        });

        peer2.client.DiscoverRooms();

        while (!complete && CheckTimeout(ref time, "Discover Rooms"))
        {
            yield return null;
        }
    }

    private bool CheckTimeout(ref float time, string message)
    {
        Assert.IsTrue((time += Time.deltaTime) < timeout, message);
        return time < timeout;
    }
}
