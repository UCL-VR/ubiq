using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;

public class RoomJoin : MonoBehaviour
{
    [SerializeField] public RoomClient roomClient;
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
        Debug.Log($"Joining room {roomId}");
        roomClient.Join(roomId);
    }
}
