using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Logging;
using Ubiq.Messaging;
using Ubiq.Samples.Bots;
using UnityEngine;

/// <summary>
/// Implements a scalability experiment by remote controlling BotManager 
/// instances.
/// This Component creates bots at a regular frequency, and collects
/// latency measurements using the BotsController's RPC feature.
/// </summary>
public class ExperimentRunner : MonoBehaviour
{
    public BotsController Controller; // For the bots
    public BotsManager Manager; // For the prefab list
    public LogCollector LocalLogCollector; // For the log collector

    private LogEmitter experiment;

    [Serializable]
    public class Condition
    {
        public GameObject Bot;
        public int UpdateRate;
        public int Padding;
        public bool ManageRooms;
    }

    public int NumBots;

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
        Controller.OnMessage("Position", OnPosition);
        Controller.OnMessage("BotDestination", OnBotDestination);
    }

    private void OnRTT(string ev)
    {
        var measurement = JsonUtility.FromJson<LatencyMeter.Measurement>(ev);
        experiment.Log("RTT", measurement.source, measurement.destination, measurement.time, measurement.frameTime);
    }

    private void OnPosition(string parms)
    {
        var report = JsonUtility.FromJson<BotExperimentScript.PositionReport>(parms);
        experiment.Log("Position", report.slice, report.position, report.velocity);
    }

    private void OnBotDestination(string parms)
    {
        var destination = JsonUtility.FromJson<Vector3>(parms);
        experiment.Log("BotDestination", parms);
    }

    public void Begin()
    {
        StartCoroutine(RunAllConditions());
        StartCoroutine(Step());
        StartCoroutine(Step2());
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

    public IEnumerator Step2()
    {
        int slice = 0;
        while (true)
        {
            Controller.SendBotMessage("GetPosition", slice.ToString());
            slice++;
            yield return new WaitForSeconds(0.5f);
        }
    }

    public IEnumerator Run(int conditionIndex)
    {
        // This corountine implements one trial over multiple frames.

        Debug.Log("Preparing Condition " + conditionIndex);

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

        yield return new WaitForSeconds(5f); // Wait for a few seconds to let the server settle.

        Debug.Log("Starting Condition " + conditionIndex);

        experiment.Log("Condition", conditionIndex, condition.UpdateRate, condition.Padding, condition.Bot.name);

        Controller.UpdateRate = condition.UpdateRate;
        Controller.Padding = condition.Padding;

        var prefab = 0;
        for (int i = 0; i < Manager.BotPrefabs.Length; i++)
        {
            if(Manager.BotPrefabs[i] == condition.Bot)
            {
                prefab = i;
                break;
            }
        }
        Debug.Log("Prefab: " + prefab + " | " + Manager.BotPrefabs[prefab].name);

        if(condition.ManageRooms)
        {
            Controller.CreateBotsRoom();
        }

        float startTime = Time.time;

        int botManager = 0;
        int numBots = 0;

        while(numBots < NumBots)
        {
            var manager = Controller.BotManagers.ElementAt(botManager);
            botManager = (botManager + 1) % Controller.BotManagers.Count;

            if(manager.Pid == System.Diagnostics.Process.GetCurrentProcess().Id.ToString() && Controller.BotManagers.Count > 1)
            {
                manager = Controller.BotManagers.ElementAt(botManager);
                botManager = (botManager + 1) % Controller.BotManagers.Count;
            }

            manager.AddBots(prefab, 1);

            // Check for early termination criteria here...

            numBots++;
            yield return new WaitForSeconds(1.5f); // Every second add a new bot to a manager process in order
        }

        experiment.Log("ExperimentComplete"); // Footer for last log file (if any)

        Debug.Log("Condition " + conditionIndex + " Complete.");
    }
}
