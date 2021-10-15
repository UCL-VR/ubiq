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

            var image = client.Room["scene-image"];
            if (image != null && image != existing)
            {
                client.GetBlob(client.Room.UUID, image, (base64image) =>
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
        }
    }
}