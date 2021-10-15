using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Messaging;

namespace Ubiq.Samples
{
    [RequireComponent(typeof(Text))]
    public class NameAvatarIndicator : MonoBehaviour
    {
        private Text text;
        private Avatars.Avatar avatar;

        private void Awake ()
        {
            text = GetComponent<Text>();
            avatar = GetComponentInParent<Avatars.Avatar>();
        }

        private void Update ()
        {
            if (!avatar)
            {
                enabled = false;
                return;
            }

            text.text = avatar.Peer["ubiq.samples.social.name"] ?? "(unnamed)";
        }
    }
}