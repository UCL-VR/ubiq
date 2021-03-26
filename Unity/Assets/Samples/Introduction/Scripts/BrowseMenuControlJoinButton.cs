using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples
{
    public class BrowseMenuControlJoinButton : MonoBehaviour
    {
        public BrowseMenuControl browseMenuControl;

        private RoomClient roomClient;
        private string joincode;

        private void OnEnable()
        {
            browseMenuControl.OnBind.AddListener(BrowseRoomControl_OnBind);
        }

        private void OnDisable()
        {
            if (browseMenuControl)
            {
                browseMenuControl.OnBind.RemoveListener(BrowseRoomControl_OnBind);
            }
        }

        private void BrowseRoomControl_OnBind(RoomClient roomClient, RoomInfo roomInfo)
        {
            this.roomClient = roomClient;
            this.joincode = roomInfo.Joincode;
        }

        // Expected to be called by a UI element
        public void Join()
        {
            if (!roomClient || string.IsNullOrEmpty(joincode))
            {
                return;
            }

            roomClient.Join(joincode);
        }
    }
}