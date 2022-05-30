using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Ubiq.Samples
{
    public class BrowseMenuControl : MonoBehaviour
    {
        public Text Name;
        public Text SceneName;
        public RawImage ScenePreview;

        [System.Serializable]
        public class BindEvent : UnityEvent<RoomClient, IRoom> { };
        public BindEvent OnBind;

        private string existing;

        public void Bind(RoomClient client, IRoom room)
        {
            Name.text = room.Name;
            SceneName.text = room["scene-name"];

            OnBind.Invoke(client,room);
        }
    }
}