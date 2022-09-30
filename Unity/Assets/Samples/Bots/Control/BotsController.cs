using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Logging;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Samples.Bots.Messaging;
using Ubiq.Utilities.Coroutines;
using UnityEngine;
using UnityEngine.Events;

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

        public int NumBots => BotManagers.Select(x => x.NumBots).Sum();

        public int NumBotsRoomPeers => BotsRoom.Peers.Count();
        public RoomClient BotsRoom;
        public ICollection<BotManagerProxy> BotManagers => proxies.Values;

        private Dictionary<string, BotManagerProxy> proxies = new Dictionary<string, BotManagerProxy>();

        private InfoLogEmitter Info;
        private NetworkScene networkScene;

        private Dictionary<string, Action<string>> sendMessageFunctions = new Dictionary<string, Action<string>>();

        public void OnMessage(string methodName, Action<string> callback)
        {
            sendMessageFunctions[methodName] = callback;
        }

        private void Awake()
        {
            if (Application.isBatchMode)
            {
                DestroyImmediate(this);
                return;
            }

            UpdateRate = 60;

            RoomClient.Find(this).SetDefaultServer(BotsConfig.CommandServer);
            BotsRoom.SetDefaultServer(BotsConfig.BotServer);
        }

        private void Start()
        {
            networkScene = NetworkScene.Find(this);
            if (networkScene)
            {
                networkScene.AddProcessor(Id, ProcessMessage);
            }

            var roomClient = networkScene.GetComponent<RoomClient>();
            roomClient.OnJoinedRoom.AddListener(room =>
            {
                CommandJoinCode = room.JoinCode;
            });
            roomClient.Join(BotsConfig.CommandRoomGuid);

            roomClient.OnPeerRemoved.AddListener(peer =>
            {
                proxies.Remove(peer.uuid);
            });

            BotsRoom.OnJoinedRoom.AddListener((room) =>
            {
                AddBotsToRoom(room.JoinCode);
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
            BotsRoom.Join("Bots Room", false);
        }

        public void ToggleAudio(bool audio)
        {
            if(audio != EnableAudio)
            {
                EnableAudio = audio;
                UpdateProxies();
            }
        }

        public void AddBotsToRoom(string joinCode)
        {
            if(BotsJoinCode != joinCode)
            {
                BotsJoinCode = joinCode;
                UpdateProxies();
            }
        }

        public void TerminateProcesses()
        {
            foreach (var item in proxies.Values)
            {
                item.Quit();
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

        /// <summary>
        /// Issues a SendMessage to the specified methodName on all Bots on all
        /// BotManagers known to this controller.
        /// </summary>
        public void SendBotMessage(string methodName, string parameters)
        {
            foreach (var item in proxies.Values)
            {
                item.SendMessage(methodName, parameters);
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
                case "SendMessage":
                    {
                        var Message = message.FromJson<SendMessage>();
                        if (sendMessageFunctions.ContainsKey(Message.MethodName))
                        {
                            sendMessageFunctions[Message.MethodName].Invoke(Message.Parameter);
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
                    AvatarUpdateRate = controller.UpdateRate,
                });
            }
        }

        public void AddBots(int NumBots)
        {
            if (networkScene)
            {
                networkScene.SendJson(Id, new AddBots(0, NumBots));
            }
        }

        public void AddBots(int BotPrefab, int NumBots)
        {
            if (networkScene)
            {
                networkScene.SendJson(Id, new AddBots(BotPrefab, NumBots));
            }
        }

        public void ClearBots()
        {
            if (networkScene)
            {
                networkScene.SendJson(Id, new ClearBots());
            }
        }

        public void Quit()
        {
            if (networkScene)
            {
                networkScene.SendJson(Id, new Quit());
            }
        }

        /// <summary>
        /// Issues a SendMessage call to each Bot run by the Bot Manager. The 
        /// SendMessage arguments are a Json,
        /// </summary>
        public void SendMessage(string methodName, string parameters)
        {
            if(networkScene)
            {
                networkScene.SendJson(Id, new SendMessage() { MethodName = methodName, Parameter = parameters });
            }
        }

        private NetworkScene networkScene;
        private BotsController controller;

        public NetworkId Id;
        public string Guid;
        public string Pid;
        public int NumBots;
        public float Fps;
        public float LastMessageTime;
    }
}