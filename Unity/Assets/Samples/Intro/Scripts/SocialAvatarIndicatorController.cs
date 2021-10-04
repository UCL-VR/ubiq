using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;

namespace Ubiq.Samples
{
    public class SocialAvatarIndicatorController : MonoBehaviour
    {
        private Avatars.Avatar avatar;
        private SocialMenu socialMenu;

        private void Awake()
        {
            avatar = GetComponentInParent<Avatars.Avatar>();
        }

        private void Start()
        {
            if (avatar.IsLocal)
            {
                gameObject.SetActive(false);
                return;
            }

            socialMenu = GetComponentInParent<NetworkScene>().
                GetComponentInChildren<SocialMenu>();
            if (!socialMenu)
            {
                gameObject.SetActive(false);
            }
            socialMenu.OnOpen.AddListener(SocialMenu_OnOpen);
            socialMenu.OnClose.AddListener(SocialMenu_OnClose);
        }

        private void OnDestroy()
        {
            if (socialMenu)
            {
                socialMenu.OnOpen.RemoveListener(SocialMenu_OnOpen);
                socialMenu.OnClose.RemoveListener(SocialMenu_OnClose);
            }
        }

        private void SocialMenu_OnOpen(SocialMenu menu)
        {
            gameObject.SetActive(true);
        }

        private void SocialMenu_OnClose(SocialMenu menu)
        {
            gameObject.SetActive(false);
        }
    }
}