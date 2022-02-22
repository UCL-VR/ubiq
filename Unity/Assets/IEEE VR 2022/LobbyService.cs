using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Rooms;
using Ubiq.Extensions;
using Ubiq.Guids;
using UnityEngine;

/*
 * Welcome to the IEEE VR 2022 Ubiq Tutorial!
 * 
 * This Component splits users evenly between rooms into small groups 
 * automatically where they can discuss with Ubiq team-members, as well as
 * showing what kind of things can achieved with Ubiq.
 * 
 * Once moved into a breakout room, you can still easily move between rooms
 * using the Join Codes to meet up with specific people.
 * 
 * 
 * The Component works by monitoring the number of people in the Lobby Room.
 * Once the Peer Count reaches a threshold, all the Peers move together into
 * a new Room.
 * This is achieved by each Peer computing the same Room Id individually.
 * The Room Id is a hash of the last Peer to join the Room.
 * 
 */

public class LobbyService : MonoBehaviour
{
    private RoomClient roomClient;

    public int GroupSize = 5;
    public string LobbyRoom = "a1fa1cd0-f837-492f-8ee9-59eda9bd0220";

    private void Awake()
    {
        roomClient = this.GetClosestComponent<RoomClient>();
    }

    // Start is called before the first frame update
    void Start()
    {
        roomClient.OnPeerAdded.AddListener(OnPeerAdded);
        roomClient.Join(System.Guid.Parse(LobbyRoom)); // Join the Lobby Room the first time this Component loads. From then on, the user can change the room as desired.
    }

    void OnPeerAdded(IPeer peer)
    {
        if(roomClient.Room != null)
        {
            if(roomClient.Room.UUID == LobbyRoom)
            {
                if (roomClient.Peers.Count() >= GroupSize)
                {
                    // A V5 Guid is a procedurally generated Guid based on SHA1 hashing some data.
                    // When the threshold is reached, all Peers will individually Join the same new Room, based on them
                    // computing the same V5 GUID.

                    // To make sure all existing Peers join the same room, we need a function that is deterministic for all of them.
                    // The Server processes all Join and Leave requests in order, so the order of Joins and Leaves to the Peers list
                    // at each Peer will the same as well.
                    // We can reliably use this list to detect when the Peer count has reached the threshold, and identify which Peers
                    // should be in the new Room.

                    // What each Peer can't tell however is that *it* was the Nth Peer, so the final step is to pick the "lowest" Id
                    // from the set, and use that to seed the new Room.

                    var peers = new List<IPeer>(roomClient.Peers); // From all other peers,
                    peers.Add(roomClient.Me); // and myself,
                    var seed = peers.OrderBy(p => { return p.UUID; }).First(); // find the 'extreme' in some deterministic dimension

                    roomClient.Join(Guids.Generate(System.Guid.Parse(LobbyRoom), seed.NetworkObjectId.ToString())); // and use that to make a new room
                }
            }
        }
    }
}
