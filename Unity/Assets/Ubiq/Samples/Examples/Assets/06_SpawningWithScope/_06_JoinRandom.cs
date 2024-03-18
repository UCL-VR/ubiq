using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;

namespace Ubiq.Examples
{
    public class _06_JoinRandom : MonoBehaviour
    {
        public RoomClient roomClient;

        public void JoinRandom()
        {
            if (roomClient)
            {
                roomClient.Join(Guid.NewGuid());
            }
        }
    }
}