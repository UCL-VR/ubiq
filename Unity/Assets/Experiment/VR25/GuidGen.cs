using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// Generates a number of guids and writes them to a file.
/// Every room generated is by default a knnroom, with a k value of 256.
/// </summary>
public class GuidGen : MonoBehaviour
{
    // Start is called before the first frame update
    public int numRooms = 3;
    // private string filePath = "Assets/Experiment/VR25/RoomGuids.csv";
    private string filePath = "../Scalability-Experiment/RoomGuids.csv";
    void Start()
    {
        GenerateGuids();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void GenerateGuids()
    {
        System.IO.File.AppendAllText(filePath, "Guid,k\n");
        for (int i = 0; i < numRooms; i++)
        {
            Guid roomId = Guid.NewGuid();
            Debug.Log("Room created with id: " + roomId);
            System.IO.File.AppendAllText(filePath, roomId.ToString() + ",256\n");
        }
        Debug.Log("Rooms created");
    }
}
