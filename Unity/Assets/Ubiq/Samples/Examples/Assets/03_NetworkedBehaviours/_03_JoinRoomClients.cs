using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;

namespace Ubiq.Examples
{
    public class _03_JoinRoomClients : MonoBehaviour
    {
        private void Start()
        {
            var guid = Guid.NewGuid();
            foreach (var roomClient in GetComponentsInChildren<RoomClient>())
            {
                roomClient.Join(guid);
            }
        }
    }
}