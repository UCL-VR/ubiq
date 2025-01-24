using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using System;
using Ubiq.Samples.Bots;

public class Room : MonoBehaviour
{
    public Guid roomId;

    void Awake()
    {
        roomId = Guid.NewGuid();
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Room created with id: " + roomId + " at position: " + transform.position + " with box collider scale:" + GetComponent<BoxCollider>().size);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerEnter called!!!!! at " + roomId);
        
   
        if (other.gameObject.tag == "Player")
        {
            RoomJoin bot = other.gameObject.GetComponent<RoomJoin>();
            bot.JoinRoom(roomId);


            Debug.Log("Player entered room " + roomId);
        }
    }
}
