using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Samples.Bots.Messaging
{
    [Serializable]
    public class Message
    {
        public string Type;

        public Message(string type)
        {
            this.Type = type;
        }
    }

    [Serializable]
    public class BotManagerSettings : Message 
    {
        public bool EnableAudio;
        public string BotsRoomJoinCode;
        public int AvatarUpdateRate;
        public int AvatarDataPadding;

        public BotManagerSettings():base("UpdateBotManagerSettings")
        {
        }
    }

    [Serializable]
    public class AddBots : Message
    {
        public int PrefabIndex;
        public int NumBots;

        public AddBots(int PrefabIndex, int NumBots):base("AddBots")
        {
            this.NumBots = NumBots;
            this.PrefabIndex = PrefabIndex;
        }
    }

    [Serializable]
    public class ClearBots : Message
    {
        public ClearBots():base("ClearBots")
        {
        }
    }

    [Serializable]
    public class Quit : Message
    {
        public Quit():base("Quit")
        {
        }
    }

    [Serializable]
    public class SendMessage : Message
    {
        public string MethodName;
        public string Parameter;

        public SendMessage():base("SendMessage")
        {
        }
    }

    public class BotManagerStatus : Message
    {
        public NetworkId NetworkId;
        public string Guid;
        public float Fps;
        public int NumBots;
        public string Pid;

        public BotManagerStatus(BotsManager manager):base("BotManagerStatus")
        {
            NetworkId = manager.Id;
            Guid = manager.Guid;
            Fps = manager.Fps;
            NumBots = manager.NumBots;
            Pid = manager.Pid;
        }
    }

}