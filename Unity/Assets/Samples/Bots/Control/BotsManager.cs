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
    public class BotsManager : MonoBehaviour
    {
        public GameObject[] BotPrefabs;
        public int NumBots { get => bots.Count; }
        public float Fps { get { return FpsMovingAverage.Mean(); } private set { FpsMovingAverage.Update(value); } }
        public string Guid { get; private set; }
        public string Pid { get; private set; }

        public class MovingAverage
        {
            float[] samples;
            int i;

            public MovingAverage(int N)
            {
                samples = new float[N];
                i = 0;
            }

            public void Update(float s)
            {
                i = (int)Mathf.Repeat(i, samples.Length);
                samples[i++] = s;
            }

            public float Mean()
            {
                float acc = 0;
                for (int i = 0; i < samples.Length; i++)
                {
                    acc += samples[i];
                }
                return acc / (float)samples.Length;
            }
        }

        private MovingAverage FpsMovingAverage = new MovingAverage(30);

        public NetworkId Id { get; set; } = NetworkId.Unique();

        /// <summary>
        /// When True, Remote Avatars belonging to bots in this scene have their Mesh Renderers disabled.
        /// </summary>
        public bool HideBotAvatars = true;

        public int AvatarUpdateRate = 60;

        public int Padding = 0;

        /// <summary>
        /// When set (not null) this is passed to new bots.
        /// </summary>
        public string BotMessage { get; private set; }

        /// <summary>
        /// When True, Bots are created with synthetic audio sources and sinks, and transmit and receive audio. When false, no Voip connections are made.
        /// </summary>
        public bool EnableAudio = true;

        private List<Bot> bots;
        private float lastStatusTime;
        private string botsRoomJoinCode;
        private NetworkScene networkScene;
        private RoomClient roomClient;

        public class BotPeerEvent : UnityEvent<Bot>
        {
        }

        public BotPeerEvent OnBot = new BotPeerEvent();

        public class SendMessageArguments
        {
            public string parameter;

            /// <summary>
            /// Sends a message back to the Controller that initiated this call.
            /// </summary>
            public Action<string, string> SendMessageBack;
        }

        private void Awake()
        {
            bots = new List<Bot>();
            bots.AddRange(MonoBehaviourExtensions.GetComponentsInScene<Bot>());
            bots.ForEach(b => GetRoomClient(b).SetDefaultServer(BotsConfig.BotServer));
            roomClient = RoomClient.Find(this);
            roomClient.SetDefaultServer(BotsConfig.CommandServer);
            lastStatusTime = Time.time;
            Pid = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
        }

        private void Start()
        {
            bots.ForEach(b => InitialiseBot(b));

            networkScene = NetworkScene.Find(this);
            if (networkScene)
            {
                networkScene.AddProcessor(Id,ProcessMessage);
            }

            Guid = roomClient.Me.uuid;
            roomClient.Join(BotsConfig.CommandRoomGuid);
        }

        private void OnDestroy()
        {
            if (networkScene)
            {
                networkScene.RemoveProcessor(Id,ProcessMessage);
            }
        }

        private void Update()
        {
            Fps = 1 / Time.deltaTime;
            if(Time.time - lastStatusTime > 0.25)
            {
                lastStatusTime = Time.time;
                networkScene.SendJson(BotsController.Id, new BotManagerStatus(this));
            }
        }

        private void AddBot(int Prefab)
        {
            var newBot = GameObject.Instantiate(BotPrefabs[Prefab]);
            var bot = newBot.GetComponentInChildren<Bot>();
            bots.Add(bot);
            InitialiseBot(bot);
            AddBotsToRoom(bot);
        }

        public void AddBots(int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                AddBot(index);
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

        /// <summary>
        /// Sends a a message to the Bot Controller of this Bot Manager.
        /// </summary>
        public void SendMessageBack(string methodName, string parameter)
        {
            networkScene.SendJson(BotsController.Id, new SendMessage()
            {
                MethodName = methodName,
                Parameter = parameter
            });
        }

        public void AddBotsToRoom(Bot bot)
        {
            var botRoomClient = GetRoomClient(bot);
            if(!string.IsNullOrEmpty(botsRoomJoinCode) && botRoomClient.Room.JoinCode != botsRoomJoinCode)
            {
                botRoomClient.Join(botsRoomJoinCode);
            }
        }

        public void SendBotsMessage(string methodName)
        {
            BotMessage = methodName;
            if (!String.IsNullOrWhiteSpace(BotMessage))
            {
                foreach (var bot in bots)
                {
                    bot.gameObject.SendMessage(BotMessage);
                }
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

            if (!String.IsNullOrWhiteSpace(BotMessage))
            {
                bot.gameObject.SendMessage(BotMessage);
            }

            OnBot.Invoke(bot);
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
                        AddBots(Message.PrefabIndex, Message.NumBots);
                    }
                    break;
                case "ClearBots":
                    {
                        ClearBots();
                    }
                    break;
                case "Quit":
                    {
                        if(!Application.isEditor)
                        {
                            Application.Quit();
                        }
                    }
                    break;
                case "SendMessage":
                    {
                        var request = message.FromJson<SendMessage>();
                        foreach (var bot in bots)
                        {
                            bot.SendMessage(request.MethodName, request.Parameter, SendMessageOptions.DontRequireReceiver);
                        }
                    }
                    break;
            }
        }

        public static BotsManager Find(MonoBehaviour bot)
        {
            return bot.GetClosestComponent<BotsManager>();
        }
    }
}