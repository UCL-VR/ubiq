using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Avatars;
using Ubiq.Extensions;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Samples.Bots.Messaging;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    [NetworkComponentId(typeof(BotsManager), 1)]
    public class BotsManager : MonoBehaviour, INetworkComponent, INetworkObject
    {
        public GameObject BotPeer;
        public int NumBots { get => bots.Count; }
        public float Fps { get; private set; }
        public string Guid { get; private set; }

        public NetworkId Id { get; set; } = NetworkId.Unique();

        /// <summary>
        /// When True, Remote Avatars belonging to bots in this scene have their Mesh Renderers disabled.
        /// </summary>
        public bool HideBotAvatars = true;

        /// <summary>
        /// When True, Bots are created with synthetic audio sources and sinks, and transmit and receive audio. When false, no Voip connections are made.
        /// </summary>
        public bool EnableAudio = true;

        /// <summary>
        /// The default join code to use when adding new bots to a room.
        /// </summary>
        [NonSerialized]
        public string joinCode;

        [NonSerialized]
        public NetworkContext context;

        private List<Bot> bots;

        private void Awake()
        {
            Guid = System.Guid.NewGuid().ToString();
            bots = new List<Bot>();
            bots.AddRange(MonoBehaviourExtensions.GetComponentsInScene<Bot>());
            joinCode = "";
        }

        private void Start()
        {
            bots.ForEach(b => InitialiseBot(b));

            context = NetworkScene.Register(this);
            var roomClient = context.scene.GetComponent<RoomClient>();
            roomClient.OnJoinedRoom.AddListener(room =>
            {
                Announce();
            });
            roomClient.OnPeerAdded.AddListener(peer =>
            {
                Announce();
            });
        }

        public void Announce()
        {
            context.SendJson(BotsController.Id, 1, new BotManagerAnnounce(this));
        }

        public void JoinCommandRoom(string JoinCode)
        {
            context.scene.GetComponent<RoomClient>().Join(JoinCode);
        }

        private void Update()
        {
            Fps = 1 / Time.deltaTime;
        }

        public void AddBot()
        {
            var newBot = GameObject.Instantiate(BotPeer);
            var bot = newBot.GetComponentInChildren<Bot>();
            bots.Add(bot);
            InitialiseBot(bot);
            AddBotsToRoom(bot);
        }

        public void AddBots(int count)
        {
            for (int i = 0; i < count; i++)
            {
                AddBot();
            }
        }

        public void AddBotsToRoom()
        {
            foreach (var bot in bots)
            {
                AddBotsToRoom(bot);
            }
        }

        public void AddBotsToRoom(Bot bot)
        {
            var rc = GetRoomClient(bot);
            if(!string.IsNullOrEmpty(joinCode) && rc.Room.JoinCode != joinCode)
            {
                rc.Join(joinCode);
            }
        }

        private void InitialiseBot(Bot bot)
        {
            var rc = GetRoomClient(bot);
            rc.Me["ubiq.botmanager.id"] = Guid;

            var am = AvatarManager.Find(bot);
            if(am)
            {
                am.OnAvatarCreated.AddListener(avatar =>
                {
                    if(HideBotAvatars)
                    {
                        if(avatar.Peer["ubiq.botmanager.id"] == Guid && !avatar.IsLocal)
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

            if(!EnableAudio)
            {
                var voipManager = Voip.VoipPeerConnectionManager.Find(bot);
                if(voipManager)
                {
                    DestroyImmediate(voipManager);
                }
            }
        }

        private RoomClient GetRoomClient(Bot bot)
        {
            return bot.GetClosestComponent<RoomClient>();
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var Base = message.FromJson<Message>();
            switch (Base.Type)
            {
                case "UpdateBotManagerSettings":
                    {
                        var Message = message.FromJson<BotManagerSettings>();
                        EnableAudio = Message.EnableAudio;
                        if(joinCode != Message.BotsRoomJoinCode)
                        {
                            joinCode = Message.BotsRoomJoinCode;
                            AddBotsToRoom();
                        }
                    }
                    break;
                case "AddBots":
                    {
                        var Message = message.FromJson<AddBots>();
                        AddBots(Message.NumBots);
                    }
                    break;
                case "GetStats":
                    {
                        context.SendJson(BotsController.Id, 1, new BotManagerStats(this));
                    }
                    break;
            }
        }
    }
}