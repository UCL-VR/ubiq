using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Ubiq.XR;

namespace Ubiq.Samples
{
    public class SocialMenuRequestHandler : MonoBehaviour
    {
        public SocialMenu socialMenu;
        public MenuRequestSource source;

        private void OnEnable()
        {
            if (source)
            {
                source.OnRequest.AddListener(MenuRequestSource_OnMenuRequest);
            }
        }

        private void OnDisable()
        {
            if (source)
            {
                source.OnRequest.RemoveListener(MenuRequestSource_OnMenuRequest);
            }
        }

        private void MenuRequestSource_OnMenuRequest(GameObject requester)
        {
            socialMenu.Request();
        }
    }
}