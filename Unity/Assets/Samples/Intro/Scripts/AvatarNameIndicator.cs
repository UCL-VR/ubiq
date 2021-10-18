using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Messaging;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    [RequireComponent(typeof(Text))]
    public class AvatarNameIndicator : MonoBehaviour
    {
        private Text text;
        private Avatars.Avatar avatar;
        private SocialMenu socialMenu;

        private void Awake()
        {
            text = GetComponent<Text>();
        }

        private void Start()
        {
            avatar = GetComponentInParent<Avatars.Avatar>();

            if (!avatar || avatar.IsLocal)
            {
                text.enabled = false;
                return;
            }

            socialMenu = GetComponentInParent<NetworkScene>()?.
                GetComponentInChildren<SocialMenu>();

            if (socialMenu == null || !socialMenu)
            {
                text.enabled = false;
                return;
            }

            avatar.OnPeerUpdated.AddListener(Avatar_OnPeerUpdated);
            socialMenu.OnOpen.AddListener(SocialMenu_OnOpen);
            socialMenu.OnClose.AddListener(SocialMenu_OnClose);
        }

        private void OnDestroy()
        {
            if (avatar)
            {
                avatar.OnPeerUpdated.RemoveListener(Avatar_OnPeerUpdated);
            }

            if (socialMenu)
            {
                socialMenu.OnOpen.RemoveListener(SocialMenu_OnOpen);
                socialMenu.OnClose.RemoveListener(SocialMenu_OnClose);
            }
        }

        private void Avatar_OnPeerUpdated (IPeer peer)
        {
            UpdateName();
        }

        private void SocialMenu_OnOpen(SocialMenu menu)
        {
            text.enabled = true;
        }

        private void SocialMenu_OnClose(SocialMenu menu)
        {
            text.enabled = false;
        }

        private void UpdateName()
        {
            text.text = avatar.Peer["ubiq.samples.social.name"] ?? "(unnamed)";
        }
    }
}