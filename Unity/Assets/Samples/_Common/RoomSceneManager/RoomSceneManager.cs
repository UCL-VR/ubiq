using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ubiq.Rooms;
using Ubiq.Messaging;

namespace Ubiq.Samples
{
    public class RoomSceneManager : MonoBehaviour
    {
        private RoomClient client;

        private void Awake()
        {
            client = GetComponentInParent<RoomClient>();
        }

        private void Start()
        {
            client.OnRoomUpdated.AddListener(OnRoomUpdated);
            client.OnJoinedRoom.AddListener(OnJoinedRoom);
        }

        private void OnJoinedRoom(IRoom room)
        {
            var name = room["scene-name"];
            if(name == string.Empty)
            {
                UpdateRoomScene(); // if we've joined a room without a scene
            }
        }

        private void OnRoomUpdated(IRoom room)
        {
            var name = room["scene-name"];
            if (name != string.Empty)
            {
                LoadSceneAsync(name);
            }
        }

        public void UpdateRoomScene()
        {
            var active = SceneManager.GetActiveScene();
            client.Room["scene-name"] = active.name;
            client.Room["scene-path"] = active.path;
        }

        public void ChangeScene(string name)
        {
            LoadSceneAsync(name);
        }

        private void LoadSceneAsync(string name)
        {
            if(SceneManager.GetSceneByName(name).isLoaded)
            {
                return;
            }

            SceneManager.LoadSceneAsync(name, LoadSceneMode.Single).completed +=
                (operation) =>
                {
                    SceneManager.SetActiveScene(SceneManager.GetSceneByName(name));
                    UpdateRoomScene();
                };
        }

        public static void ChangeScene(MonoBehaviour caller, string sceneName)
        {
            var manager = FindSceneManager(NetworkScene.Find(caller));
            manager.ChangeScene(sceneName);
        }

        private static RoomSceneManager FindSceneManager(NetworkScene scene)
        {
            var manager = scene.GetComponentInChildren<RoomSceneManager>();
            Debug.Assert(manager != null, $"Cannot find RoomSceneManager Component for {scene}. Ensure a RoomSceneManager Component has been added.");
            return manager;
        }
    }
}