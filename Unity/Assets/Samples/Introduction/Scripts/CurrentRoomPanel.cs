using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    public class CurrentRoomPanel : MonoBehaviour
    {
        public MainMenu mainMenu;
        public GameObject notInRoomPanel;
        public GameObject inRoomPanel;
        public CurrentRoomPanelControl control;
        public Image background;
        public Color backgroundHighlightOnJoinColor;
        public float highlightOnJoinTimeSeconds;

        private Color backgroundDefaultColor;

        private void OnEnable()
        {
            UpdateControl(false);
            backgroundDefaultColor = background.color;

            mainMenu.roomClient.OnJoinedRoom.AddListener(RoomClient_OnJoinedRoom);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            background.color = backgroundDefaultColor;

            if (mainMenu.roomClient)
            {
                mainMenu.roomClient.OnJoinedRoom.RemoveListener(RoomClient_OnJoinedRoom);
            }
        }

        private void RoomClient_OnJoinedRoom()
        {
            UpdateControl(true);
        }

        private void UpdateControl(bool onJoinedRoom)
        {
            if (!mainMenu.roomClient)
            {
                return;
            }

            if (mainMenu.roomClient.joinedRoom)
            {
                notInRoomPanel.SetActive(false);
                inRoomPanel.SetActive(true);
                control.Bind(mainMenu.roomClient);

                if (onJoinedRoom)
                {
                    StartCoroutine(HighlightBackground());
                }
            }
            else
            {
                notInRoomPanel.SetActive(true);
                inRoomPanel.SetActive(false);
            }
        }

        private IEnumerator HighlightBackground()
        {
            var delta = backgroundDefaultColor - backgroundHighlightOnJoinColor;
            var totalTime = 0.0f;
            while (totalTime < highlightOnJoinTimeSeconds)
            {
                var t = totalTime / highlightOnJoinTimeSeconds;
                background.color = backgroundHighlightOnJoinColor + delta * t;
                totalTime += Time.deltaTime;
                yield return null;
            }

            background.color = backgroundDefaultColor;
        }
    }
}
