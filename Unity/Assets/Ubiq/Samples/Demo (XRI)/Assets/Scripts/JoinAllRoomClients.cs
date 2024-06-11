using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Ubiq.Samples
{
    public class JoinAllRoomClients : MonoBehaviour
    {
        private void Start()
        {
            var guid = Guid.NewGuid();
            foreach (var roomClient in FindObjectsByType<RoomClient>(FindObjectsSortMode.None))
            {
                roomClient.Join(guid);
            }
        }
    }
}