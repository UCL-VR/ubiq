using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Avatars;
using Ubiq.Extensions;
using Ubiq.Rooms;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    public class BotsManager : MonoBehaviour
    {
        public GameObject BotPeer;
        public string JoinCode { get; set; }
        public int NumBots { get => bots.Count; }

        public Camera Camera;
        private int cullingMask;

        private string botManagerInstance;

        /// <summary>
        /// When True, Remote Avatars belonging to bots in this scene have their Mesh Renderers disabled.
        /// </summary>
        public bool HideBotAvatars = true;

        private List<Bot> bots;

        private void Awake()
        {
            botManagerInstance = Guid.NewGuid().ToString();
            bots = new List<Bot>();
            bots.AddRange(MonoBehaviourExtensions.GetComponentsInScene<Bot>());
            cullingMask = Camera.cullingMask;
        }

        private void Start()
        {
            bots.ForEach(b => InitialiseBot(b));
        }

        /// <summary>
        /// Create a new room and have all the bots join it. Any new bots will also join the room.
        /// </summary>
        public void CreateRoom()
        {
            if(bots.Count > 0)
            {
                var bot = bots.First();
                var roomClient = GetRoomClient(bot);
                roomClient.OnJoinedRoom.AddListener(room =>
                {
                    JoinCode = room.JoinCode;
                    JoinRoom();
                });
                GetRoomClient(bot).JoinNew("Bots Room", false);
            }
        }

        public void AddBot()
        {
            var newBot = GameObject.Instantiate(BotPeer);
            var bot = newBot.GetComponentInChildren<Bot>();
            bots.Add(bot);
            InitialiseBot(bot);
            JoinRoom(bot);
        }

        public void AddBots(int count)
        {
            for (int i = 0; i < count; i++)
            {
                AddBot();
            }
        }

        public void JoinRoom()
        {
            foreach (var bot in bots)
            {
                JoinRoom(bot);
            }
        }

        public void JoinRoom(Bot bot)
        {
            var rc = GetRoomClient(bot);
            if(!string.IsNullOrEmpty(JoinCode) && rc.Room.JoinCode != JoinCode)
            {
                rc.Join(JoinCode);
            }
        }

        private void InitialiseBot(Bot bot)
        {
            var rc = GetRoomClient(bot);
            rc.Me["ubiq.botmanager.id"] = botManagerInstance;

            var am = AvatarManager.Find(bot);
            if(am)
            {
                am.OnAvatarCreated.AddListener(avatar =>
                {
                    if(HideBotAvatars)
                    {
                        if(avatar.Peer["ubiq.botmanager.id"] == botManagerInstance && !avatar.IsLocal)
                        {
                            foreach(var r in avatar.GetComponentsInChildren<MeshRenderer>())
                            {
                                r.enabled = false;
                            }
                            foreach (var r in avatar.GetComponentsInChildren<SkinnedMeshRenderer>())
                            {
                                r.enabled = false;
                            }
                        }
                    }
                });
            }
        }

        private RoomClient GetRoomClient(Bot bot)
        {
            return bot.GetClosestComponent<RoomClient>();
        }

        public void ToggleCamera()
        {
            Camera.cullingMask ^= 1 << LayerMask.NameToLayer("Default");
        }
    }
}