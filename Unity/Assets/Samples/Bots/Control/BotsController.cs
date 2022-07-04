using System.Collections;
using System.Collections.Generic;
using Ubiq.Logging;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Samples.Bots.Messaging;
using Ubiq.Utilities.Coroutines;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    public class BotsController : MonoBehaviour
    {
        public static NetworkId Id = new NetworkId("c2af7158-69522e16");

        public string CommandJoinCode { get; private set; }
        public string BotsJoinCode { get; set; }
        public bool EnableAudio { get; set; }
        public int UpdateRate { get; set; }
        public int Padding { get; set; }

        public int NumBotsRoomPeers => BotsRoom.NumPeers;
        public PerformanceMonitor BotsRoom;
        public ICollection<BotManagerProxy> BotManagers => proxies.Values;

        private Dictionary<string, BotManagerProxy> proxies = new Dictionary<string, BotManagerProxy>();

        private InfoLogEmitter Info;
        private NetworkScene networkScene;

        private void Awake()
        {
            if (Application.isBatchMode)
            {
                DestroyImmediate(this);
                return;
            }

            RoomClient.Find(this).SetDefaultServer(BotsConfig.CommandServer);
            BotsRoom.RoomClient.SetDefaultServer(BotsConfig.BotServer);
        }

        private void Start()
        {
            networkScene = NetworkScene.FindNetworkScene(this);
            if (networkScene)
            {
                networkScene.AddProcessor(Id,ProcessMessage);
            }

            var roomClient = networkScene.GetComponent<RoomClient>();
            roomClient.OnJoinedRoom.AddListener(Room => {
                CommandJoinCode = Room.JoinCode;
            });
            roomClient.Join(BotsConfig.CommandRoomGuid);

            BotsRoom.RoomClient.OnJoinedRoom.AddListener(Room => {
                AddBotsToRoom(Room.JoinCode);
            });

            Info = new InfoLogEmitter(this);

            StartCoroutine(Coroutines.Update(0.5f, () =>
            {
                var totalBots = 0;
                foreach (var item in proxies)
                {
                    totalBots += item.Value.NumBots;
                }
                Info.Log("BotsControllerInfo", totalBots, UpdateRate, Padding);
            }));
        }

        private void OnDestroy ()
        {
            if (networkScene)
            {
                networkScene.RemoveProcessor(Id,ProcessMessage);
            }
        }

        public void CreateBotsRoom()
        {
            BotsRoom.RoomClient.Join("Bots Room", false);
        }

        public void ToggleAudio(bool audio)
        {
            if(audio != EnableAudio)
            {
                EnableAudio = audio;
                UpdateProxies();
            }
        }

        public void AddBotsToRoom(string JoinCode)
        {
            if(BotsJoinCode != JoinCode)
            {
                BotsJoinCode = JoinCode;
                UpdateProxies();
            }
        }

        public void ClearBots()
        {
            foreach (var item in proxies.Values)
            {
                item.ClearBots();
            }
        }

        public void UpdateProxies()
        {
            foreach (var item in proxies.Values)
            {
                item.UpdateSettings();
            }
        }

        private void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var Base = message.FromJson<Message>();
            switch (Base.Type)
            {
                case "BotManagerStatus":
                    {
                        var Message = message.FromJson<BotManagerStatus>();
                        if(!proxies.ContainsKey(Message.Guid))
                        {
                            var Proxy = new BotManagerProxy(this,networkScene);
                            Proxy.Id = Message.NetworkId;
                            Proxy.ComponentId = Message.ComponentId;
                            Proxy.Guid = Message.Guid;
                            Proxy.Pid = Message.Pid;
                            proxies.Add(Proxy.Guid, Proxy);
                            Proxy.UpdateSettings();
                        }
                        {
                            var Proxy = proxies[Message.Guid];
                            Proxy.NumBots = Message.NumBots;
                            Proxy.Fps = Message.Fps;
                            Proxy.LastMessageTime = Time.realtimeSinceStartup;
                            Proxy.Pid = Message.Pid;
                        }
                    }
                    break;
            }
        }
    }

    public class BotManagerProxy
    {
        public BotManagerProxy(BotsController controller, NetworkScene networkScene)
        {
            this.controller = controller;
            this.networkScene = networkScene;
            LastMessageTime = Time.realtimeSinceStartup;
        }

        public void UpdateSettings()
        {
            if (networkScene)
            {
                networkScene.SendJson(Id, new BotManagerSettings()
                {
                    BotsRoomJoinCode = controller.BotsJoinCode,
                    EnableAudio = controller.EnableAudio,
                    AvatarDataPadding = controller.Padding,
                    AvatarUpdateRate = controller.UpdateRate
                });
            }
        }

        public void AddBots(int NumBots)
        {
            if (networkScene)
            {
                networkScene.SendJson(Id, new AddBots(NumBots));
            }
        }

        public void ClearBots()
        {
            if (networkScene)
            {
                networkScene.SendJson(Id, new ClearBots());
            }
        }

        private NetworkScene networkScene;
        private BotsController controller;

        public NetworkId Id;
        public ushort ComponentId;
        public string Guid;
        public string Pid;
        public int NumBots;
        public float Fps;
        public float LastMessageTime;
    }
}