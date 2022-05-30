using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Ubiq.Samples.Social
{
    public class AvatarPagePanelControl : MonoBehaviour
    {
        public RawImage image;
        public Button button;

        [System.Serializable]
        public class BindEvent : UnityEvent<Texture2D> { };
        public BindEvent OnBind;

        private Action<Texture2D> onClick;
        private Texture2D texture;

        public void Bind(Action<Texture2D> onClick, Texture2D texture)
        {
            this.texture = texture;
            this.onClick = onClick;

            image.texture = texture;
            button.onClick.AddListener(Button_OnClick);

            OnBind.Invoke(texture);
        }

        private void Button_OnClick()
        {
            if (onClick != null && texture != null)
            {
                onClick(texture);
            }
        }
    }
}