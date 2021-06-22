using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples
{
    public class RoomsMenuControl : MonoBehaviour
    {
        public Text Name;
        public Text SceneName;
        public RawImage ScenePreview;
        public Button SelfButton;
        public Button JoinButton;

        [HideInInspector]
        public RoomInfo room;

        private string existing;

        private void Awake()
        {
            if (JoinButton)
            {
                JoinButton.onClick.AddListener(() => GetComponentInParent<RoomsMenuController>().Join(this));
            }
            if (SelfButton)
            {
                SelfButton.onClick.AddListener(() => GetComponentInParent<RoomsMenuController>().Select(this));
            }
        }

        public void Bind(RoomInfo args, RoomClient client)
        {
            Name.text = args.Name;
            SceneName.text = args["scene-name"];

            var image = args["scene-image"];
            if (image != null && image != existing)
            {
                client.GetBlob(args.UUID, image, (base64image) =>
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

            room = args;
        }
    }
}