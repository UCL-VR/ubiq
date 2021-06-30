using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Avatars;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

namespace Ubiq.Samples
{
    [NetworkComponentId(typeof(SampleAvatarVisibilityHelper), 4543)]
    public class SampleAvatarVisibilityHelper : MonoBehaviour, INetworkComponent, INetworkObject
    {
        public enum Role
        {
            Player1,
            Player2
        }

        public Role role;

        private AvatarManager manager;

        private void Awake()
        {
            manager = AvatarManager.Find(this);
        }

        NetworkContext context;

        public NetworkId Id { get; private set; } = new NetworkId("9df26448-7bc92abb");

        // Start is called before the first frame update
        void Start()
        {
            var role = Application.isEditor ? "Experimentor" : "Participant";

            NetworkScene.FindNetworkScene(this).GetComponentInChildren<RoomClient>().Me["ubiq.samples.avatars.visibility.role"] = role.ToString();
            context = NetworkScene.Register(this);
        }

        // Update is called once per frame
        void Update()
        {

        }

        [Serializable]
        struct Message
        {
            public bool visibility;
            public string role;
        }

        public void ShowPlayer1()
        {
            Message message;
            message.role = "Player1";
            message.visibility = true;

            context.SendJson(message);
            HandleMessage(message);
        }

        public void HidePlayer1()
        {
            Message message;
            message.role = "Player1";
            message.visibility = false;

            context.SendJson(message);
            HandleMessage(message);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            HandleMessage(JsonUtility.FromJson<Message>(message.ToString()));
        }

        private void HandleMessage(Message m)
        {
            foreach (var item in manager.Avatars)
            {
                bool isRole = item.Peer["ubiq.samples.avatars.visibility.role"] == m.role;
                if (isRole)
                {
                    item.gameObject.SetActive(m.visibility);
                }
            }
        }
    }
}