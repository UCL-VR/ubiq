using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Networking;
using Ubiq.Rooms;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    public class BotsConfig : MonoBehaviour
    {
        [Header("Command and Control Server")]
        public ConnectionDefinition DefaultCommandServer;

        [Header("Bots Server")]
        public ConnectionDefinition DefaultBotServer;

        [Header("Rooms")]
        public string ControlRoomId;

        private static BotsConfig singleton;
        private static BotsConfig Singleton
        {
            get
            {
                if (singleton == null)
                {
                    // This may be called in any Awake method

                    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    foreach (var item in scene.GetRootGameObjects())
                    {
                        singleton = item.GetComponentInChildren<BotsConfig>();
                        if (singleton)
                        {
                            break;
                        }
                    }
                    if(singleton)
                    {
                        singleton.CheckCommandLineArguments();
                    }
                }
                return singleton;
            }
        }

        private void CheckCommandLineArguments()
        {
            try
            {
                DefaultBotServer = new ConnectionDefinition(CommandLine.GetArgument("-botsserver"));
            }catch(ArgumentException)
            {
                //Ignore this setting 
            }
        }

        public static ConnectionDefinition CommandServer
        {
            get
            {
                return Singleton.DefaultCommandServer;
            }
        }

        public static ConnectionDefinition BotServer
        {
            get
            {
                return Singleton.DefaultBotServer;
            }
        }

        public static Guid CommandRoomGuid
        {
            get
            {
                return new Guid(Singleton.ControlRoomId);
            }
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(ControlRoomId))
            {
                ControlRoomId = Guid.NewGuid().ToString();
            }
        }
    }
}