using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;

namespace Ubiq.Examples
{
    public class _11_JoinOther : MonoBehaviour
    {
        public RoomClient roomClient;
        public RoomClient otherPeerRoomClient;

        public void Join()
        {
            if (otherPeerRoomClient != null && otherPeerRoomClient.Room != null)
            {
                roomClient.Join(otherPeerRoomClient.Room.JoinCode);
            }
        }
    }
}