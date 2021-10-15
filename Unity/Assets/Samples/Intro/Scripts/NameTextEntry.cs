using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;

namespace Ubiq.Samples.Social
{
    [RequireComponent(typeof(TextEntry))]
    public class NameTextEntry : MonoBehaviour
    {
        private TextEntry textEntry;
        private SocialMenu mainMenu;

        private void Awake()
        {
            textEntry = GetComponent<TextEntry>();
            mainMenu = GetComponentInParent<SocialMenu>();
        }

        private void OnEnable()
        {
            AttemptInit();
        }

        private void Start()
        {
            AttemptInit();
        }

        private void AttemptInit()
        {
            if (mainMenu && mainMenu.roomClient)
            {
                mainMenu.roomClient.OnPeerUpdated.AddListener(RoomClient_OnPeer);
                UpdateTextEntry();
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
                UpdateTextEntry();
            }
        }

        private void UpdateTextEntry()
        {
            if (mainMenu && mainMenu.roomClient && mainMenu.roomClient.Me != null)
            {
                var name = mainMenu.roomClient.Me["ubiq.samples.social.name"];
                if(name != null)
                {
                    textEntry.SetText(name,textEntry.defaultTextColor,true);
                }
            }
        }
    }
}