using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Messaging;
using Ubiq.Rooms;

namespace Ubiq
{
    [RequireComponent(typeof(Text))]
    public class AvatarDisplayNameIndicator : MonoBehaviour
    {
        private Text text;
        private Avatars.Avatar avatar;

        private void Start()
        {
            text = GetComponent<Text>();
            avatar = GetComponentInParent<Avatars.Avatar>();

            if (!avatar || avatar.IsLocal)
            {
                text.enabled = false;
                return;
            }

            avatar.OnPeerUpdated.AddListener(Avatar_OnPeerUpdated);
        }

        private void OnDestroy()
        {
            if (avatar)
            {
                avatar.OnPeerUpdated.RemoveListener(Avatar_OnPeerUpdated);
            }
        }

        private void Avatar_OnPeerUpdated (IPeer peer)
        {
            UpdateName();
        }

        private void UpdateName()
        {
            var name = avatar.Peer[DisplayNameManager.KEY];
            if (name != string.Empty)
            {
                text.text = name;
            }
            else
            {
                text.enabled = false;
            }
        }
    }
}