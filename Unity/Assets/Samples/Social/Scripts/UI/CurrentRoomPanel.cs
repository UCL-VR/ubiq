using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    public class CurrentRoomPanel : MonoBehaviour
    {
        public SocialMenu mainMenu;
        public GameObject notInRoomPanel;
        public GameObject inRoomPanel;
        public CurrentRoomPanelControl control;
        public Image background;
        public Color backgroundHighlightOnJoinColor;
        public float highlightOnJoinTimeSeconds;

        private Color backgroundDefaultColor;

        private void Start()
        {
            mainMenu.roomClient.OnJoinedRoom.AddListener(RoomClient_OnJoinedRoom);
        }

        private void OnDestroy()
        {
            if (mainMenu.roomClient)
            {
                mainMenu.roomClient.OnJoinedRoom.RemoveListener(RoomClient_OnJoinedRoom);
            }
        }

        private void OnEnable()
        {
            UpdateControl(false);
            backgroundDefaultColor = background.color;
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            background.color = backgroundDefaultColor;
        }

        private void RoomClient_OnJoinedRoom(IRoom room)
        {
            UpdateControl(true);
        }

        private void UpdateControl(bool onJoinedRoom)
        {
            if (!mainMenu.roomClient)
            {
                return;
            }

            if (mainMenu.roomClient.JoinedRoom)
            {
                notInRoomPanel.SetActive(false);
                inRoomPanel.SetActive(true);
                control.Bind(mainMenu.roomClient);

                if (isActiveAndEnabled && onJoinedRoom)
                {
                    StartCoroutine(PulseBackground());
                }
            }
            else
            {
                notInRoomPanel.SetActive(true);
                inRoomPanel.SetActive(false);
            }
        }

        private IEnumerator PulseBackground()
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
