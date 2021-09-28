using System.Collections;
using System.Collections.Generic;
using Ubiq.Networking;
using Ubiq.Rooms;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    public class BotsServers : MonoBehaviour
    {
        [Header("Command and Control Server")]
        public ConnectionDefinition DefaultCommandServer;

        [Header("Bots Server")]
        public ConnectionDefinition DefaultBotServer;

        private static BotsServers singleton;
        private static BotsServers Singleton
        {
            get
            {
                if (singleton == null)
                {
                    // This may be called in any Awake method

                    var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                    foreach (var item in scene.GetRootGameObjects())
                    {
                        singleton = item.GetComponentInChildren<BotsServers>();
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
            // todo
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
    }
}