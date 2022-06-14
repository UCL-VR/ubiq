using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Avatars;
using Ubiq.Extensions;
using Ubiq.Messaging;
using Ubiq.Networking;
using Ubiq.Rooms;
using Ubiq.Samples.Bots.Messaging;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq.Samples.Bots
{
    [NetworkComponentId(typeof(BotsManager), 1)]
    public class BotsManager : MonoBehaviour, INetworkComponent, INetworkObject
    {
        public GameObject BotPeer;
        public int NumBots { get => bots.Count; }
        public float Fps { get { return FpsMovingAverage.Mean(); } private set { FpsMovingAverage.Update(value); } }
        public string Guid { get; private set; }
        public string Pid { get; private set; }

        private MovingAverage FpsMovingAverage = new MovingAverage(30);

        public NetworkId Id { get; set; } = NetworkId.Unique();

        /// <summary>
        /// When True, Remote Avatars belonging to bots in this scene have their Mesh Renderers disabled.
        /// </summary>
        public bool HideBotAvatars = true;

        public int AvatarUpdateRate = 60;

        public int Padding = 0;

        /// <summary>
        /// When True, Bots are created with synthetic audio sources and sinks, and transmit and receive audio. When false, no Voip connections are made.
        /// </summary>
        public bool EnableAudio = true;

        
        [NonSerialized]
        public NetworkContext context;

        private List<Bot> bots;
        private float lastStatusTime;
        private string botsRoomJoinCode;

        public class BotPeerEvent : UnityEvent<Bot>
        {
        }

        public BotPeerEvent OnBot;

        private void Awake()
        {
            Guid = System.Guid.NewGuid().ToString();
            bots = new List<Bot>();
            bots.AddRange(MonoBehaviourExtensions.GetComponentsInScene<Bot>());
            bots.ForEach(b => GetRoomClient(b).SetDefaultServer(BotsConfig.BotServer));
            RoomClient.Find(this).SetDefaultServer(BotsConfig.CommandServer);
            lastStatusTime = Time.time;
            Pid = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
        }

        private void Start()
        {
            bots.ForEach(b => InitialiseBot(b));

            context = NetworkScene.Register(this);
            var roomClient = context.scene.GetComponent<RoomClient>();
            roomClient.Join(BotsConfig.CommandRoomGuid);
        }

        private void Update()
        {
            Fps = 1 / Time.deltaTime;
            if(Time.time - lastStatusTime > 0.25)
            {
                lastStatusTime = Time.time;
                context.SendJson(BotsController.Id, 1, new BotManagerStatus(this));
            }
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

        public void ClearBots()
        {
            foreach (var bot in bots)
            {
                GameObject.Destroy(bot.transform.parent.gameObject);
            }
            bots.Clear();
        }

        public void AddBotsToRoom(Bot bot)
        {
            var botRoomClient = GetRoomClient(bot);
            if(!string.IsNullOrEmpty(botsRoomJoinCode) && botRoomClient.Room.JoinCode != botsRoomJoinCode)
            {
                botRoomClient.Join(botsRoomJoinCode);
            }
        }

        private void InitialiseBot(Bot bot)
        {
            var rc = GetRoomClient(bot);
            rc.Me["ubiq.botmanager.id"] = Guid;
            rc.SetDefaultServer(BotsConfig.BotServer);

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

                    avatar.UpdateRate = AvatarUpdateRate;

                    if (avatar.IsLocal)
                    {
                        var adm = avatar.gameObject.AddComponent<AvatarDataGenerator>();
                        adm.BytesPerMessage = Padding;
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

            OnBot?.Invoke(bot);
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
                        AvatarUpdateRate = Message.AvatarUpdateRate;
                        Padding = Message.AvatarDataPadding;
                        if (botsRoomJoinCode != Message.BotsRoomJoinCode)
                        {
                            botsRoomJoinCode = Message.BotsRoomJoinCode;
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
                case "ClearBots":
                    {
                        ClearBots();
                    }
                    break;
            }
        }
    }
}