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
        Debug.Log("Room created with id: " + roomId);
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
            // RoomJoin bot = other.gameObject.GetComponent<RoomJoin>();


            Debug.Log("Player entered room " + roomId);
        }
    }
}
