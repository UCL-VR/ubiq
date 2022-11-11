using System.Collections;
using System.Collections.Generic;
using Ubiq.Logging;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    /// <summary>
    /// Writes information about this Bot server to the network.
    /// </summary>
    public class BotsMonitor : MonoBehaviour
    {
        //Even if attached to the top level BotsManager this will find the first Bot's LogManager to feedback through
        private LogEmitter Info;
        private BotsManager Manager;
        private float lastTime;

        private void Awake()
        {
            Manager = GetComponent<BotsManager>();
        }

        // Start is called before the first frame update
        void Start()
        {
            Info = new InfoLogEmitter(this);
        }

        // Update is called once per frame
        void Update()
        {
            if((Time.realtimeSinceStartup - lastTime) > 1)
            {
                lastTime = Time.realtimeSinceStartup;
                Info.Log("BotsManager", Manager.Guid, Manager.NumBots, Time.deltaTime);
            }
        }
    }
}