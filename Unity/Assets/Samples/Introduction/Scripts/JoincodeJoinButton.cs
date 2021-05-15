using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    public class JoincodeJoinButton : MonoBehaviour
    {
        public MainMenu mainMenu;
        public PanelSwitcher mainPanel;
        public Text joincodeText;

        private void OnEnable()
        {
            mainMenu.roomClient.OnJoinedRoom.AddListener(RoomClient_OnJoinedRoom);
        }

        private void OnDisable()
        {
            mainMenu.roomClient.OnJoinedRoom.RemoveListener(RoomClient_OnJoinedRoom);
        }

        private void RoomClient_OnJoinedRoom(RoomInfo room)
        {
            mainPanel.SwitchPanelToDefault();
        }

        // Expected to be called by a UI element
        public void Join()
        {
            mainMenu.roomClient.Join(joincodeText.text.ToLowerInvariant());
        }
    }
}
