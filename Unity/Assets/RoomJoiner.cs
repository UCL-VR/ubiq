using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Messaging;

public class RoomJoiner : MonoBehaviour
{
    public string Guid;

    // Start is called before the first frame update
    void Start()
    {
        RoomClient.Find(this).Join(System.Guid.Parse(Guid));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
