using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;

public class RoomJoin : MonoBehaviour
{
    [SerializeField] public RoomClient roomClient;
    public Guid currentRoomId { get; private set; }

    /// <summary>
    /// We assume the first networking room a client joins is the corridor, the room from which they can join other rooms.
    /// This is the join code of the corridor room.
    /// </summary>
    private string corridorJoinCode = null;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void JoinRoom(Guid roomId)
    {
        if (corridorJoinCode == null)
        {
            corridorJoinCode = roomClient.Room.JoinCode;
        }

        if (roomId == currentRoomId)
        {
            return;
        }
        currentRoomId = roomId;
        Debug.Log($"Joining room {currentRoomId}");
        roomClient.Join(currentRoomId);
    }

    public void LeaveRoom()
    {
        roomClient.Join(corridorJoinCode);
    }
}
