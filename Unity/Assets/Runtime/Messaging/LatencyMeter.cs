using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ubiq.Logging;
using Ubiq.Messaging;
using Ubiq.Rooms;
using UnityEngine;

/// <summary>
/// Works with a counterpart on other Peers to measure the Round Trip Time to that Peer and write them to an Event Log.
/// </summary>
public class LatencyMeter : MonoBehaviour, INetworkComponent
{
    private NetworkContext context;
    private RoomClient client;
    private Dictionary<string, Stopwatch> transmissionTimes;
    private LogEmitter latencies;

    private void Awake()
    {
        transmissionTimes = new Dictionary<string, Stopwatch>();
    }

    // Start is called before the first frame update
    void Start()
    {
        context = NetworkScene.Register(this);
        UnityEngine.Debug.Assert(context.scene.Id == context.networkObject.Id); // The LatencyMeter has no way to find other meters other than to send messages to the Peer, so ensure that its GO is always the Peer
        client = context.scene.GetComponentInChildren<RoomClient>();
        latencies = new ExperimentLogEmitter(this);
    }

    private struct Message
    {
        public NetworkId source;
        public string token;
        public bool reply;
        public float deltaTime; // This is the frame interval at the remote PC; the RTT will be dependent on this so it is useful to have it too.
    }

    public void MeasurePeerLatencies(IPeer peer)
    {
        if (peer.UUID == client.Me.UUID)
        {
            return;
        }

        Message message;
        message.source = context.scene.Id;
        message.token = Guid.NewGuid().ToString();
        message.reply = false;
        message.deltaTime = 0;
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        transmissionTimes[message.token] = stopwatch;
        context.SendJson(peer.NetworkObjectId, message);
    }

    public void MeasurePeerLatencies()
    {
        foreach (var peer in client.Peers)
        {
            MeasurePeerLatencies(peer);
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