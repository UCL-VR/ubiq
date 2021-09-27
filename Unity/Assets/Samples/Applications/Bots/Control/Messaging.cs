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

    public class GetStats : Message
    {
        public GetStats():base("GetStats")
        {
        }
    }

    public class BotManagerAnnounce : Message
    {
        public NetworkId NetworkId;
        public ushort ComponentId;
        public string Guid;

        public BotManagerAnnounce(BotsManager manager):base("BotManagerAnnounce")
        {
            NetworkId = manager.context.networkObject.Id;
            Guid = manager.Guid;
        }
    }

    public class BotManagerSettings : Message 
    {
        public bool EnableAudio;
        public string BotsRoomJoinCode;

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

    public class BotManagerStats : Message
    {
        public string Guid;
        public float Fps;
        public int NumBots;

        public BotManagerStats(BotsManager manager):base("BotManagerStats")
        {
            Guid = manager.Guid;
            Fps = manager.Fps;
            NumBots = manager.NumBots;
        }
    }

}