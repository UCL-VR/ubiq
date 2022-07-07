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

    [UnityTest]
    public IEnumerator PeerDictionaries()
    {
        float time = 0;
        var room = System.Guid.NewGuid();

        var peer1 = new Peer();

        // Peer sets a key before joining a room

        peer1.client.Me["key1"] = "key1";
        peer1.client.Me["key3"] = "oldkey3";

        peer1.client.Join(room);

        while (!peer1.hasJoined && CheckTimeout(ref time, "Joining Room"))
        {
            yield return null;
        }

        // Peer sets a key after joining a room
        peer1.client.Me["key2"] = "key2";

        // Peer updates a key after joining a room
        peer1.client.Me["key3"] = "key3";

        var peer2 = new Peer();

        peer2.client.Join(room);

        while (!peer2.hasJoined && CheckTimeout(ref time, "Joining Room"))
        {
            yield return null;
        }

        // Wait for peer 1 to show up

        while (peer2.client.Peers.Count() <= 0)
        {
            yield return null;
        }

        Dictionary<string, IPeer> peers = new Dictionary<string, IPeer>();
        peers.Add(peer1.client.Me.uuid, peer1.client.Me);
        peers.Add(peer2.client.Me.uuid, peer2.client.Me);

        foreach (var item in peer2.client.Peers)
        {
            ComparePeerKeys1(peers[item.uuid], item);
        }

        // Update some keys on Peer 2

        peer2.client.Me["key1"] = "key1";
        peer2.client.Me["key2"] = "key2";
        peer2.client.Me["key3"] = "key3";

        bool peerUpdated = false;

        peer1.client.OnPeerUpdated.AddListener(peer =>
        {
            if(peer.uuid == peer2.client.Me.uuid){
                peerUpdated = true;
            }
        });

        // Note that updates should be batched (all updates within a frame sent in update),
        // so we should only have to wait for one Updated event.

        while (!peerUpdated && CheckTimeout(ref time, "Peer Update"))
        {
            yield return null;
        }

        // Wait for peer1 to be updated

        // And check they are recieved at Peer 1

        foreach (var item in peer1.client.Peers)
        {
            ComparePeerKeys1(peers[item.uuid], item);
        }
    }

    private void ComparePeerKeys1(IPeer p1, IPeer p2)
    {
        Assert.AreEqual(p1["key1"], p2["key1"]);
        Assert.AreEqual(p1["key2"], p2["key2"]);
        Assert.AreEqual(p1["key3"], p2["key3"]);
        Assert.AreEqual(p1["key1"], "key1");
        Assert.AreEqual(p1["key2"], "key2");
        Assert.AreEqual(p1["key3"], "key3");
        Assert.AreEqual(p2["key1"], "key1");
        Assert.AreEqual(p2["key2"], "key2");
        Assert.AreEqual(p2["key3"], "key3");
    }

    private IEnumerator WaitForJoin(Peer p)
    {
        float time = 0;
        while(!p.hasJoined)
        {
            time += Time.deltaTime;
            if(time > timeout)
            {
                Assert.Fail("Timeout waiting to join room");
                break;
            }
            yield return null;
        }
    }

    private bool CheckTimeout(ref float time, string message)
    {
        Assert.IsTrue((time += Time.deltaTime) < timeout, message);
        return time < timeout;
    }
}
