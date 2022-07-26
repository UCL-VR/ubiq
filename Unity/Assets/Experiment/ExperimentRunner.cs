using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Logging;
using Ubiq.Messaging;
using Ubiq.Samples.Bots;
using UnityEngine;

public class ExperimentRunner : MonoBehaviour
{
    public BotsController Controller; // For the bots
    public LogCollector LocalLogCollector; // For the log collector

    private LogEmitter experiment;

    [Serializable]
    public class Condition
    {
        public string Mode;
        public int UpdateRate;
        public int Padding;
        public bool ManageRooms;
    }

    public List<Condition> Conditions;

    private void Awake()
    {
        if (Application.isBatchMode)
        {
            Destroy(this);
            return;
        }

        if (!Application.isEditor)
        {
            Destroy(this);
            return;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        experiment = new ExperimentLogEmitter(LocalLogCollector);
        LocalLogCollector.StartCollection();
        Controller.OnMessage("RTT", OnRTT);
    }

    private void OnRTT(string ev)
    {
        var measurement = JsonUtility.FromJson<LatencyMeter.Measurement>(ev);
        experiment.Log("RTT", measurement.source, measurement.destination, measurement.time, measurement.frameTime);
    }

    public void Begin()
    {
        StartCoroutine(RunAllConditions());
        StartCoroutine(Step());
    }

    public IEnumerator RunAllConditions()
    {
        for (int i = 0; i < Conditions.Count; i++)
        {
            yield return Run(i);
        }
    }

    public IEnumerator Step()
    {
        while (true)
        {
            // Ask all the bots to do a latency measurement. This message is recieved by the 
            // BotExperimentScript Component.
            Controller.SendBotMessage("DoLatencyMeasurements", Controller.NumBots.ToString());

            // Log the number of peers in the Bots room
            experiment.Log("PeerCount", Controller.BotsRoom.Peers.Count());

            yield return new WaitForSeconds(0.2f);
        }
    }

    public IEnumerator Run(int conditionIndex)
    {
        Debug.Log("Starting Condition " + conditionIndex);

        var condition = Conditions[conditionIndex];

        // First shut down the existing experiment/demo if any. We wait for both
        // the Bots Room to be empty, and the Bot Managers status reports to
        // show all bots have been destroyed.

        Controller.ClearBots();

        while(Controller.BotsRoom.Peers.Count() > 0)
        {
            yield return null;
        }

        while(Controller.BotManagers.Any(m => m.NumBots > 0))
        {
            yield return null;
        }

        yield return new WaitForSeconds(3f); // Wait for a few seconds to let the server settle.

        experiment.Log("Condition", conditionIndex, condition.UpdateRate, condition.Padding, condition.Mode);

        Controller.UpdateRate = condition.UpdateRate;
        Controller.Padding = condition.Padding;
        Controller.SendMessage(condition.Mode);

        if(condition.ManageRooms)
        {
            Controller.CreateBotsRoom();
        }

        float startTime = Time.time;
        float totalTime = 100f;

        int botManager = 0;

        while(true)
        {
            if((Time.time - startTime) > totalTime) // For the duration of a trial (100 seconds)
            {
                break;
            }

            var manager = Controller.BotManagers.ElementAt(botManager);
            botManager = (botManager + 1) % Controller.BotManagers.Count;

            if(manager.Pid == System.Diagnostics.Process.GetCurrentProcess().Id.ToString() && Controller.BotManagers.Count > 1)
            {
                manager = Controller.BotManagers.ElementAt(botManager);
                botManager = (botManager + 1) % Controller.BotManagers.Count;
            }

            manager.AddBots(1);

            // Check for early termination criteria here...

            yield return new WaitForSeconds(1.5f); // Every second add a new bot to a manager process in order
        }

        experiment.Log("ExperimentComplete"); // Footer for last log file (if any)

        Debug.Log("Condition " + conditionIndex + " Complete.");
    }
}
