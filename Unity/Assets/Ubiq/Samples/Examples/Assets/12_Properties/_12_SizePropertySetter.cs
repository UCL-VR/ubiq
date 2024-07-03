using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Messaging;

namespace Ubiq.Examples
{
    public class _12_SizePropertySetter : MonoBehaviour
    {
        private string networkIdString;
        private RoomClient roomClient;

        private void Start()
        {
            networkIdString = NetworkId.Create(this).ToString();
            roomClient = GetComponent<RoomClient>();
            roomClient.OnRoomUpdated.AddListener(RoomClient_OnRoomUpdated);
        }

        private void RoomClient_OnRoomUpdated(IRoom room)
        {
            var sizeProperty = room[networkIdString];
            if (!string.IsNullOrEmpty(sizeProperty))
            {
                if (float.TryParse(sizeProperty, out var size))
                {
                    GetComponent<Transform>().transform.localScale = size * Vector3.one;
                }
            }
        }

        public void SetSize(float size)
        {
            GetComponent<Transform>().transform.localScale = size * Vector3.one;
            roomClient.Room[networkIdString] = size.ToString();
        }
    }
}