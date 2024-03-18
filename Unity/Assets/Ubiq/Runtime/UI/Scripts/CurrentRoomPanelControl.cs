using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples
{
    public class CurrentRoomPanelControl : MonoBehaviour
    {
        public Text Joincode;
        public RawImage ScenePreview;

        private string existing;

        public void Bind(RoomClient client)
        {
            Joincode.text = client.Room.JoinCode.ToUpperInvariant();

            var sceneInfo = FindObjectOfType<SceneInfo>();
            if (sceneInfo)
            {
                ScenePreview.texture = sceneInfo.screenshot;
            }
        }
    }
}