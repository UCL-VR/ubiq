using System.Collections.Generic;
using Ubiq.Rooms;
using UnityEngine;

namespace Ubiq.Samples
{
    public class BrowsePanelController : MonoBehaviour
    {
        public MainMenu mainMenu;
        public BrowseMenuControl joinedControl;
        public Transform controlsRoot;
        public GameObject roomListPanel;
        public GameObject noRoomsMessagePanel;
        public GameObject controlTemplate;

        private List<BrowseMenuControl> controls = new List<BrowseMenuControl>();
        private List<IRoom> lastRoomArgs;

        private void OnEnable()
        {
            mainMenu.roomClient.OnRoomsAvailable.AddListener(RoomClient_OnRoomsAvailable);
            mainMenu.roomClient.OnJoinedRoom.AddListener(RoomClient_OnJoinedRoom);
            UpdateAvailableRooms();
        }

        private void OnDisable()
        {
            if (mainMenu.roomClient)
            {
                mainMenu.roomClient.OnRoomsAvailable.RemoveListener(RoomClient_OnRoomsAvailable);
                mainMenu.roomClient.OnJoinedRoom.RemoveListener(RoomClient_OnJoinedRoom);
            }
        }

        private BrowseMenuControl InstantiateControl () {
            var go = GameObject.Instantiate(controlTemplate, controlsRoot);
            go.SetActive(true);
            return go.GetComponent<BrowseMenuControl>();
        }

        private void RoomClient_OnJoinedRoom(IRoom room)
        {
            UpdateAvailableRooms();

            // Immediately ask for a refresh - maybe room we left is now empty
            mainMenu.roomClient.GetRooms();
        }

        private void RoomClient_OnRoomsAvailable(List<IRoom> rooms)
        {
            lastRoomArgs = rooms;
            UpdateAvailableRooms();
        }

        private void UpdateAvailableRooms() {
            var rooms = lastRoomArgs;

            if (rooms == null || rooms.Count == 0)
            {
                for (int i = 0; i < controls.Count; i++)
                {
                    Destroy(controls[i].gameObject);
                }
                controls.Clear();
                noRoomsMessagePanel.SetActive(true);
                roomListPanel.SetActive(false);
                return;
            }

            noRoomsMessagePanel.SetActive(false);
            roomListPanel.SetActive(true);

            int controlI = 0;
            for (int roomI = 0; roomI < rooms.Count; controlI++,roomI++)
            {
                if (mainMenu.roomClient.JoinedRoom &&
                    mainMenu.roomClient.Room.UUID == rooms[roomI].UUID)
                {
                    joinedControl.gameObject.SetActive(true);
                    joinedControl.Bind(mainMenu.roomClient,rooms[roomI]);

                    // Joined control not created dynamically
                    controlI--;
                    continue;
                }

                if (controls.Count <= controlI) {
                    controls.Add(InstantiateControl());
                }

                controls[controlI].Bind(mainMenu.roomClient,rooms[roomI]);
            }

            while (controls.Count > controlI) {
                Destroy(controls[controlI].gameObject);
                controls.RemoveAt(controlI);
            }
        }

        private void Update()
        {
            mainMenu.roomClient.GetRooms();
        }
    }
}