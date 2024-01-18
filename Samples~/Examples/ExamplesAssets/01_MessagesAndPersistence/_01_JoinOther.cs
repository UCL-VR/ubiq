using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    public class _01_JoinOther : MonoBehaviour
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