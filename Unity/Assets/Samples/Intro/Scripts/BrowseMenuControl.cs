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

        public void Bind(RoomClient client, IRoom roomInfo)
        {
            Name.text = roomInfo.Name;
            SceneName.text = roomInfo["scene-name"];

            var image = roomInfo["scene-image"];
            if (image != null && image != existing)
            {
                client.GetBlob(roomInfo.UUID, image, (base64image) =>
                {
                    if (base64image.Length > 0)
                    {
                        var texture = new Texture2D(1, 1);
                        texture.LoadImage(Convert.FromBase64String(base64image));
                        existing = image;
                        ScenePreview.texture = texture;
                    }
                });
            }

            OnBind.Invoke(client,roomInfo);
        }
    }
}