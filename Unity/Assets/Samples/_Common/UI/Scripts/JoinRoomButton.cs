using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    public class JoinRoomButton : MonoBehaviour
    {
        public SocialMenu mainMenu;
        public PanelSwitcher mainPanel;
        public Text joincodeText;
        public TextEntry textEntry;
        public Image textInputArea;
        public string failMessage;
        public Color failTextInputAreaColor;

        private Color defaultTextInputAreaColor;
        private string lastRequestedJoincode;

        private void OnEnable()
        {
            mainMenu.roomClient.OnJoinedRoom.AddListener(RoomClient_OnJoinedRoom);
            mainMenu.roomClient.OnJoinRejected.AddListener(RoomClient_OnJoinRejected);
            defaultTextInputAreaColor = textInputArea.color;
        }

        private void OnDisable()
        {
            mainMenu.roomClient.OnJoinedRoom.RemoveListener(RoomClient_OnJoinedRoom);
            mainMenu.roomClient.OnJoinRejected.RemoveListener(RoomClient_OnJoinRejected);
            textInputArea.color = defaultTextInputAreaColor;
        }

        private void RoomClient_OnJoinedRoom(IRoom room)
        {
            mainPanel.SwitchPanelToDefault();
        }

        private void RoomClient_OnJoinRejected(Rejection rejection)
        {
            if (rejection.joincode != lastRequestedJoincode)
            {
                return;
            }

            textEntry.SetText(failMessage,textEntry.defaultTextColor,true);
            textInputArea.color = failTextInputAreaColor;
        }

        // Expected to be called by a UI element
        public void Join()
        {
            lastRequestedJoincode = joincodeText.text.ToLowerInvariant();
            mainMenu.roomClient.Join(joincode:lastRequestedJoincode);
        }
    }
}
