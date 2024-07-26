using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ubiq.Examples
{
    public class _32_JoinRoomClients : MonoBehaviour
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