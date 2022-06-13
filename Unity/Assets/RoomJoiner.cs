using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;

public class RoomJoiner : MonoBehaviour
{
    public string Guid;

    // Start is called before the first frame update
    void Start()
    {
        GetComponent<RoomClient>().Join(System.Guid.Parse(Guid));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
