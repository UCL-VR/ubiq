using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Ubiq.Samples
{
    public class AvatarSkinMenuControl : MonoBehaviour
    {
        private AvatarSkinMenuController controller;
        private Texture2D texture;

        public void Bind(AvatarSkinMenuController controller, Texture2D texture)
        {
            this.controller = controller;
            this.texture = texture;
            GetComponent<RawImage>().texture = texture;
        }

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(OnClick);
        }

        private void OnClick()
        {
            if(controller != null)
            {
                controller.ChangeTexture(texture);
            }
        }
    }
}