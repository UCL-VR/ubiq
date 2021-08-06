using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Ubiq.Logging;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

public class LatencyMeter : MonoBehaviour, INetworkComponent
{
    private NetworkContext context;
    private RoomClient client;
    private Dictionary<string, Stopwatch> transmissionTimes;
    private EventLogger latencies;

    private void Awake()
    {
        transmissionTimes = new Dictionary<string, Stopwatch>();
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
        client = context.scene.GetComponentInChildren<RoomClient>();
        latencies = new UserEventLogger(this);
    }

    private struct Message
    {
        public NetworkId source;
        public string token;
        public bool reply;
        public float deltaTime; // This is the frame interval at the remote PC; the RTT will be dependent on this so it is useful to have it too.
    }

    public void MeasurePeerLatencies()
    {
        foreach (var item in client.Peers)
        {
            if (item.UUID == client.Me.UUID)
            {
                continue;
            }

            Message message;
            message.source = context.scene.Id;
            message.token = Guid.NewGuid().ToString();
            message.reply = false;
            message.deltaTime = 0;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            transmissionTimes[message.token] = stopwatch;
            context.SendJson(item.NetworkObjectId, message);
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
        var source = msg.source;

        if (msg.reply)
        {
            Stopwatch stopwatch;
            if (transmissionTimes.TryGetValue(msg.token, out stopwatch))
            {
                stopwatch.Stop();
                latencies.Log("RTT", context.scene.Id, source, stopwatch.ElapsedMilliseconds, msg.deltaTime);
            }
            transmissionTimes.Remove(msg.token);
        }
        else
        {
            msg.source = context.scene.Id;
            msg.reply = true;
            msg.deltaTime = Time.deltaTime;
            context.SendJson(source, msg);
        }
    }
}