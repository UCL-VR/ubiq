using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using System;
using Ubiq.Samples.Bots;

public class Room : MonoBehaviour
{
    public Guid roomId {get; private set;}
    public Vector3 size {get; private set;}

    private BoxCollider boxCollider;

    void Awake()
    {
        roomId = Guid.NewGuid();
        
        boxCollider = gameObject.GetComponent<BoxCollider>();
        size = Vector3.zero;
        if (boxCollider != null)
            size = boxCollider.size;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Room created with id: " + roomId + " at position: " + transform.position + " with box collider scale:" + size);
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

    void OnTriggerExit(Collider other)
    {
        Debug.Log("OnTriggerExit called!!!!! at " + roomId);
        if (other.gameObject.tag == "Player")
        {
            RoomJoin bot = other.gameObject.GetComponent<RoomJoin>();
            bot.LeaveRoom();

            Debug.Log("Player left room " + roomId);
        }
    }
}
