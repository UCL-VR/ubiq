using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Samples.Bots.Messaging;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    [NetworkComponentId(typeof(BotsController), 1)]
    public class BotsController : MonoBehaviour, INetworkComponent, INetworkObject
    {
        public static NetworkId Id = new NetworkId("c2af7158-69522e16");
        NetworkId INetworkObject.Id => Id;

        public NetworkContext Context { get; private set; }
        
        public string CommandJoinCode { get; private set; }
        public string BotsJoinCode { get; set; }
        public bool EnableAudio { get; set; }

        public int NumBotsRoomPeers => BotsRoom.NumPeers;
        public PerformanceMonitor BotsRoom;
        public ICollection<BotManagerProxy> BotManagers => proxies.Values;

        private Dictionary<string, BotManagerProxy> proxies;
        private float lastPingTime = 0;

        private void Awake()
        {
            proxies = new Dictionary<string, BotManagerProxy>();
            lastPingTime = Time.realtimeSinceStartup;
        }

        // Start is called before the first frame update
        void Start()
        {
            Context = NetworkScene.Register(this);
            var roomClient = Context.scene.GetComponent<RoomClient>();
            roomClient.OnJoinedRoom.AddListener(OnJoinedCommandRoom);
            roomClient.JoinNew("Bots Command and Control Room", false);

            BotsRoom.RoomClient.OnJoinedRoom.AddListener(Room => {
                AddBotsToRoom(Room.JoinCode);
            });
        }

        void OnJoinedCommandRoom(IRoom room)
        {
            CommandJoinCode = room.JoinCode;
            foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (var item in root.GetComponentsInChildren<BotsManager>())
                {
                    item.JoinCommandRoom(CommandJoinCode);
                }
            }
        }

        public void CreateBotsRoom()
        {
            BotsRoom.RoomClient.JoinNew("Bots Room", false);
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

        public void UpdateProxies()
        {
            foreach (var item in proxies.Values)
            {
                item.UpdateSettings();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if(Time.realtimeSinceStartup - lastPingTime > 0.25)
            {
                lastPingTime = Time.realtimeSinceStartup;
                foreach (var item in proxies.Values)
                {
                    item.GetStats();
                }
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var Base = message.FromJson<Message>();
            switch (Base.Type)
            {
                case "BotManagerAnnounce":
                    {
                        var Message = message.FromJson<BotManagerAnnounce>();
                        if (!proxies.ContainsKey(Message.Guid))
                        {
                            var Proxy = new BotManagerProxy(this);
                            Proxy.Id = Message.NetworkId;
                            Proxy.Guid = Message.Guid;
                            proxies.Add(Proxy.Guid, Proxy);
                            Proxy.UpdateSettings();
                        }
                    }
                    break;
                case "BotManagerStats":
                    {
                        var Message = message.FromJson<BotManagerStats>();
                        var Proxy = proxies[Message.Guid];
                        Proxy.NumBots = Message.NumBots;
                        Proxy.Fps = Message.Fps;
                        Proxy.LastMessageTime = Time.realtimeSinceStartup;
                    }
                    break;
            }
        }
    }

    public class BotManagerProxy
    {
        public BotManagerProxy(BotsController controller)
        {
            this.controller = controller;            
            LastMessageTime = Time.realtimeSinceStartup;
        }

        public void UpdateSettings()
        {
            controller.Context.SendJson(Id, 1, new BotManagerSettings()
            { 
                BotsRoomJoinCode = controller.BotsJoinCode,
                EnableAudio = controller.EnableAudio
            });
        }

        public void GetStats()
        {
            controller.Context.SendJson(Id, 1, new GetStats());
        }

        public void AddBots(int NumBots)
        {
            controller.Context.SendJson(Id, 1, new AddBots(NumBots));
        }

        private BotsController controller;

        public NetworkId Id;
        public string Guid;

        public int NumBots;
        public float Fps;
        public float LastMessageTime;
    }
}