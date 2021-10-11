using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Rooms;
using Ubiq.Messaging;
using Ubiq.XR;

namespace Ubiq.Samples
{
    public interface ISocialMenuBindable
    {
        void Bind(SocialMenu mainMenu);
    }

    public class SocialMenu : MonoBehaviour
    {
        public NetworkScene networkSceneOverride;

        public MenuRequestHandler menuRequestHandler;
        public GameObject networkedIndicatorTemplate;

        public Transform spawnRelativeTransform;

        // Provide central access to NetworkScene for the whole UI
        private NetworkScene _networkScene;
        public NetworkScene networkScene
        {
            get
            {
                if (networkSceneOverride)
                {
                    return networkSceneOverride;
                }

                if (!_networkScene)
                {
                    _networkScene = NetworkScene.FindNetworkScene(this);
                }

                return _networkScene;
            }
        }

        // Provide central access to RoomClient for the whole UI
        private RoomClient _roomClient;
        public RoomClient roomClient
        {
            get
            {
                if (!_roomClient)
                {
                    if (networkScene)
                    {
                        _roomClient = networkScene.GetComponent<RoomClient>();
                    }
                }

                return _roomClient;
            }
        }

        [Serializable]
        public class SocialMenuEvent : UnityEvent<SocialMenu> { }
        public SocialMenuEvent OnOpen;
        public SocialMenuEvent OnClose;

        private GameObject uiIndicator;
        private string roomUUID;

        private void Start()
        {
            roomClient.OnJoinedRoom.AddListener(RoomClient_OnJoinedRoom);
            menuRequestHandler.OnRequest.AddListener(MenuRequestHandler_OnMenuRequest);

            Request();
        }

        private void OnDestroy()
        {
            if (roomClient)
            {
                roomClient.OnJoinedRoom.RemoveListener(RoomClient_OnJoinedRoom);
            }

            if (menuRequestHandler)
            {
                menuRequestHandler.OnRequest.RemoveListener(MenuRequestHandler_OnMenuRequest);
            }

            // todo: despawn
            // todo: cleanup networked obj?
        }

        private void OnEnable()
        {
            OnOpen.Invoke(this);
        }

        private void OnDisable()
        {
            OnClose.Invoke(this);
        }

        private void MenuRequestHandler_OnMenuRequest(GameObject requester)
        {
            Request();
        }

        private void RoomClient_OnJoinedRoom(IRoom room)
        {
            if (roomClient != null &&
                roomClient.Room != null &&
                roomClient.Room.UUID != roomUUID)
            {
                roomUUID = roomClient.Room.UUID;

                var spawner = NetworkSpawner.FindNetworkSpawner(networkScene);
                uiIndicator = spawner.SpawnPersistent(networkedIndicatorTemplate);
                var bindable = uiIndicator.GetComponent<ISocialMenuBindable>();
                if (bindable != null)
                {
                    bindable.Bind(this);
                    if (enabled)
                    {
                        OnOpen.Invoke(this);
                    }
                    else
                    {
                        OnClose.Invoke(this);
                    }
                }
            }
        }

        public void Request ()
        {
            var cam = Camera.main.transform;

            transform.position = cam.TransformPoint(spawnRelativeTransform.localPosition);
            transform.rotation = cam.rotation * spawnRelativeTransform.localRotation;
            gameObject.SetActive(true);
        }
    }
}