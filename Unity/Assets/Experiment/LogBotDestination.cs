using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Logging;
using Ubiq.Samples.Bots;

public class LogBotDestination : MonoBehaviour
{
    [SerializeField] public TimetableBotAgent timetableBotAgent;
    private Vector3 destination;
    ExperimentLogEmitter logEmitter;


    // Start is called before the first frame update
    void Start()
    {
        logEmitter = new ExperimentLogEmitter(this);
        logEmitter.mirrorToConsole = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (destination == null || destination != timetableBotAgent.destination)
        {
            destination = timetableBotAgent.destination;
            logEmitter.Log("BotDestination", destination.ToString());
        }
    }
}
