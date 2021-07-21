using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    public class JoincodeTextInput : MonoBehaviour
    {
        public string failMessage;
        public Color failTextInputAreaColor;
        public MainMenu mainMenu;
        public TextEntry textEntry;
        public Image textInputArea;

        private Color defaultTextInputAreaColor;

        private void OnEnable()
        {
            mainMenu.roomClient.OnJoinRejected.AddListener(RoomClient_OnJoinRejected);
            defaultTextInputAreaColor = textInputArea.color;
        }

        private void OnDisable()
        {
            mainMenu.roomClient.OnJoinRejected.RemoveListener(RoomClient_OnJoinRejected);
            textInputArea.color = defaultTextInputAreaColor;
        }

        private void RoomClient_OnJoinRejected(Rejection args)
        {
            textEntry.SetText(failMessage,textEntry.defaultTextColor,true);
            textInputArea.color = failTextInputAreaColor;
        }
    }
}
