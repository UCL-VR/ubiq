using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using System;

namespace Ubiq.Samples
{
public class Rooms : MonoBehaviour
{
    public Guid[] rooms = new Guid[3];

    void Awake()
    {
        for (int i = 0; i < 3; i++) {
            rooms[i] = Guid.NewGuid();
        }
    }

    // Start is called before the first frame update
    void Start() {}

    // Update is called once per frame
    void Update() {}
}
}
