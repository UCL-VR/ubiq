using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;

namespace Ubiq.Samples
{
    public class PanelSwitcher : MonoBehaviour
    {
        public GameObject defaultPanel;

        private GameObject currentPanel;
        // private List<GameObject> childPanels = new List<GameObject>();

        // public void AddPanel(GameObject panel)
        // {
        //     if (!childPanels.Contains(panel))
        //     {
        //         childPanels.Add(panel);
        //     }
        // }

        // public void RemovePanel (GameObject panel)
        // {
        //     childPanels.Remove(panel);
        // }

        // Expected to be called by a UI element
        public void SwitchPanel (GameObject newPanel)
        {
            if (!currentPanel)
            {
                currentPanel = defaultPanel;
            }

            if (currentPanel != newPanel)
            {
                currentPanel.SetActive(false);
            }

            newPanel.SetActive(true);
            currentPanel = newPanel;

            // currentPanel.SetActive(false);

            // var newPanelI = childPanels.IndexOf(newPanel);
            // if (newPanelI < 0) {
            //     return;
            // }

            // for (int i = 0; i < childPanels.Count; i++)
            // {
            //     if (i != newPanelI)
            //     {
            //         childPanels[i].SetActive(false);
            //     }
            // }

            // childPanels[newPanelI].SetActive(true);
        }

        public void SwitchPanelToDefault()
        {
            SwitchPanel(defaultPanel);
        }

        // // Required
        // public GameObject buttonsPanel;

        // // Optional
        // public GameObject joinRoomPanel;
        // public GameObject newRoomPanel;

        // // Expected to be called by a UI element
        // public void SwitchToJoinRoom()
        // {
        //     buttonsPanel.SetActive(false);

        //     if (newRoomPanel)
        //     {
        //         newRoomPanel.SetActive(false);
        //     }

        //     joinRoomPanel.SetActive(true);
        // }

        // // Expected to be called by a UI element
        // public void SwitchToNewRoom()
        // {
        //     buttonsPanel.SetActive(false);
        //     newRoomPanel.SetActive(true);

        //     if (joinRoomPanel)
        //     {
        //         joinRoomPanel.SetActive(false);
        //     }
        // }

        // // Expected to be called by a UI element
        // public void SwitchToMainButtonsPanel()
        // {
        //     buttonsPanel.SetActive(true);

        //     if (newRoomPanel)
        //     {
        //         newRoomPanel.SetActive(false);
        //     }

        //     if (joinRoomPanel)
        //     {
        //         joinRoomPanel.SetActive(false);
        //     }
        // }
    }
}
