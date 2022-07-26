using System;
using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Extensions;
using Ubiq.Samples.Bots;
using UnityEngine;
using Ubiq.Rooms;

// The counterpart of the experiment runner script, which implements various
// calls the experiment may need to make of the Bot

public class BotExperimentScript : MonoBehaviour
{
    private LatencyMeter latencyMeter;
    private RoomClient client;
    private Action<string, string> sendMessageBack;

    // Start is called before the first frame update
    void Start()
    {
        latencyMeter = this.GetClosestComponent<LatencyMeter>();
        latencyMeter.OnMeasurement.AddListener(OnMeasurement);
        client = RoomClient.Find(this);
    }

    // Called by latencyMeter when a ping has taken place
    public void OnMeasurement(LatencyMeter.Measurement ping)
    {
        if(sendMessageBack == null)
        {
            sendMessageBack = BotsManager.Find(this).SendMessageBack;
        }

        if(sendMessageBack != null) // If null, the ping mustve originated somewhere else
        {
            sendMessageBack("RTT", JsonUtility.ToJson(ping));
        }
    }

    // Called by SendMessage
    public void DoLatencyMeasurements(string parameter)
    {
        var numBots = float.Parse(parameter);
        if (UnityEngine.Random.value <= (1f / numBots)) // On average, one bot should do the pings each time
        {
            foreach (var peer in client.Peers)
            {
                if(peer["ubiq.rooms.roomid"] == client.Room.UUID)
                {
                    // Only ping bots in the same room, because we have the observer system in hex mode...
                    latencyMeter.MeasurePeerLatency(peer);
                }
            }
        }
    }
}
