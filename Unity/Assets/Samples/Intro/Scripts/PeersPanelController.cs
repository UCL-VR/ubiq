using System.Collections.Generic;
using Ubiq.Rooms;
using UnityEngine;

namespace Ubiq.Samples
{
    public class PeersPanelController : MonoBehaviour
    {
        public Transform controlsRoot;
        public GameObject peersListPanel;
        public GameObject noRoomMessagePanel;
        public GameObject controlPrefab;

        private BrowseMenuControl meControl;
        private List<BrowseMenuControl> controls = new List<BrowseMenuControl>();
        private List<IRoom> lastRoomArgs;

        private MainMenu mainMenu;

        private void Awake()
        {
            mainMenu = GetComponentInParent<MainMenu>();
        }

        private void OnEnable()
        {
            if (!mainMenu)
            {
                enabled = false;
                return;
            }

            mainMenu.roomClient.OnPeerAdded.AddListener(RoomClient_OnPeer);
            mainMenu.roomClient.OnPeerUpdated.AddListener(RoomClient_OnPeer);
            mainMenu.roomClient.OnPeerRemoved.AddListener(RoomClient_OnPeer);
            UpdatePeers();
        }

        private void OnDisable()
        {
            if (mainMenu.roomClient)
            {
                mainMenu.roomClient.OnPeerAdded.RemoveListener(RoomClient_OnPeer);
                mainMenu.roomClient.OnPeerUpdated.RemoveListener(RoomClient_OnPeer);
                mainMenu.roomClient.OnPeerRemoved.RemoveListener(RoomClient_OnPeer);
            }
        }

        private BrowseMenuControl InstantiateControl () {
            var go = GameObject.Instantiate(controlPrefab, controlsRoot);
            return go.GetComponent<BrowseMenuControl>();
        }

        private void RoomClient_OnPeer(IPeer peer)
        {
            UpdatePeers();
        }

        private void UpdatePeers() {
            if (!mainMenu || !mainMenu.roomClient)
            {
                return;
            }

            if (!mainMenu.roomClient.JoinedRoom)
            {
                noRoomMessagePanel.SetActive(true);
                peersListPanel.SetActive(false);
                return;
            }

            noRoomMessagePanel.SetActive(false);
            peersListPanel.SetActive(true);

            if (!meControl)
            {
                meControl = InstantiateControl();
                meControl.Bind(mainMenu.roomClient,mainMenu.roomClient.Me);
            }

            var controlI = 0;
            foreach(var peer in mainMenu.roomClient.Peers)
            {
                if (controls.Count >= controlI)
                {
                    controls.Add(InstantiateControl());
                }

                controls[controlI].Bind(mainMenu.roomClient,peer);
                controlI++;
            }

            while (controls.Count > controlI)
            {
                Destroy(controls[controls.Count-1].gameObject);
                controls.RemoveAt(controls.Count-1);
            }
        }
    }
}