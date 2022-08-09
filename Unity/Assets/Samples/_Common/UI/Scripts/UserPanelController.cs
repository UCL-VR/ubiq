using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Samples;
using Ubiq.Rooms;

namespace Ubiq.Samples.Social
{
    public class UserPanelController : MonoBehaviour
    {
        public Text nameText;
        private SocialMenu mainMenu;

        private void Awake()
        {
            mainMenu = GetComponentInParent<SocialMenu>();
        }

        private void Start()
        {
            AttemptInit();
        }

        private void OnEnable()
        {
            AttemptInit();
        }

        private void AttemptInit()
        {
            if (mainMenu && mainMenu.roomClient)
            {
                // Multiple AddListener calls still only adds one listener
                mainMenu.roomClient.OnPeerUpdated.AddListener(RoomClient_OnPeer);
                UpdatePanel();
            }
        }

        private void OnDisable()
        {
            if (mainMenu.roomClient)
            {
                mainMenu.roomClient.OnPeerUpdated.RemoveListener(RoomClient_OnPeer);
            }
        }

        private void RoomClient_OnPeer(IPeer peer)
        {
            if (mainMenu && mainMenu.roomClient && mainMenu.roomClient.Me == peer)
            {
                UpdatePanel();
            }
        }

        private void UpdatePanel()
        {
            if (mainMenu && mainMenu.roomClient && mainMenu.roomClient.Me != null)
            {
                var name = mainMenu.roomClient.Me["ubiq.samples.social.name"];
                nameText.text = name != null ? name : "(unnamed)";
            }
        }
    }
}
