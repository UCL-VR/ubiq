using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.XR;

namespace Ubiq.Samples
{
    public class SocialMenuIndicatorSpawner : MonoBehaviour
    {
        public SocialMenu socialMenu;
        public GameObject indicatorTemplate;

        private RoomClient roomClient;
        private NetworkScene networkScene;
        private string roomUUID;

        private void Start()
        {
            if (socialMenu && socialMenu.roomClient && socialMenu.networkScene)
            {
                roomClient = socialMenu.roomClient;
                networkScene = socialMenu.networkScene;
                roomClient.OnJoinedRoom.AddListener(RoomClient_OnJoinedRoom);
            }
        }

        private void OnDestroy()
        {
            if (roomClient)
            {
                roomClient.OnJoinedRoom.RemoveListener(RoomClient_OnJoinedRoom);
            }
        }

        private void RoomClient_OnJoinedRoom(IRoom room)
        {
            if (roomClient && networkScene && roomClient.Room != null &&
                roomClient.Room.UUID != roomUUID )
            {
                roomUUID = roomClient.Room.UUID;

                var indicator = NetworkSpawnManager.Find(this).SpawnWithPeerScope(indicatorTemplate);
                var bindable = indicator.GetComponent<ISocialMenuBindable>();
                if (bindable != null)
                {
                    bindable.Bind(socialMenu);
                }
            }
        }
    }
}