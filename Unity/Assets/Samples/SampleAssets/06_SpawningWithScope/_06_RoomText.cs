using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    public class _06_RoomText : MonoBehaviour
    {
        public RoomClient roomClient;

        private UnityEngine.UI.Text text;
        private string originalTextContent;

        private void Start()
        {
            roomClient.OnJoinedRoom.AddListener(RoomClient_OnJoinedRoom);

            text = GetComponent<UnityEngine.UI.Text>();
            originalTextContent = text.text;
        }

        private void RoomClient_OnJoinedRoom(IRoom room)
        {
            if (room != null && room.UUID != null && room.UUID.Length > 0)
            {
                text.text = $"{originalTextContent} #{room.UUID.Substring(0,4)}";
            }
        }
    }
}