using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Samples.Bots.Messaging
{
    public class Message
    {
        public string Type;

        public Message(string type)
        {
            this.Type = type;
        }
    }

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

    public class AddBots : Message
    {
        public int NumBots;

        public AddBots(int NumBots):base("AddBots")
        {
            this.NumBots = NumBots;
        }
    }

    public class ClearBots : Message
    {
        public ClearBots():base("ClearBots")
        {
        }
    }

    public class BotManagerStatus : Message
    {
        public NetworkId NetworkId;
        public ushort ComponentId;
        public string Guid;
        public float Fps;
        public int NumBots;

        public BotManagerStatus(BotsManager manager):base("BotManagerStatus")
        {
            NetworkId = manager.Id;
            ComponentId = 1;
            Guid = manager.Guid;
            Fps = manager.Fps;
            NumBots = manager.NumBots;
        }
    }

}