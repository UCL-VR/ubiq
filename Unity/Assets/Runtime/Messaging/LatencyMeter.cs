using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ubiq.Logging;
using Ubiq.Rooms;
using UnityEngine;

namespace Ubiq.Messaging
{
    /// <summary>
    /// Works with a counterpart on other Peers to measure the Round Trip Time to that Peer and write them to an Event Log.
    /// </summary>
    public class LatencyMeter : NetworkBehaviour
    {
        private RoomClient client;
        private LogEmitter latencies;
        private Dictionary<string, Stopwatch> transmissionTimes = new Dictionary<string, Stopwatch>();

        // Start is called before the first frame update
        protected override void Started()
        {
            latencies = new ExperimentLogEmitter(this);
            client = networkScene.GetComponentInChildren<RoomClient>();
        }

        private struct Message
        {
            public string srcPeer;
            public string dstPeer;
            public string token;
            public bool reply;
            public float deltaTime; // This is the frame interval at the remote PC; the RTT will be dependent on this so it is useful to have it too.
        }

        public void MeasurePeerLatencies(IPeer peer)
        {
            if (peer.uuid == client.Me.uuid)
            {
                return;
            }

            Message message;
            message.srcPeer = client.Me.uuid;
            message.dstPeer = peer.uuid;
            message.token = Guid.NewGuid().ToString();
            message.reply = false;
            message.deltaTime = 0;
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            transmissionTimes[message.token] = stopwatch;
            SendJson(message);
        }

        public void MeasurePeerLatencies()
        {
            foreach (var peer in client.Peers)
            {
                MeasurePeerLatencies(peer);
            }
        }

        protected override void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();

            if (!client || msg.dstPeer != client.Me.uuid)
            {
                return;
            }

            if (msg.reply)
            {
                if (transmissionTimes.TryGetValue(msg.token, out var stopwatch))
                {
                    stopwatch.Stop();
                    latencies.Log("RTT", msg.dstPeer, msg.srcPeer, stopwatch.ElapsedMilliseconds, msg.deltaTime);
                }
                transmissionTimes.Remove(msg.token);
            }
            else
            {
                var src = msg.srcPeer;
                msg.srcPeer = msg.dstPeer;
                msg.dstPeer = src;
                msg.reply = true;
                msg.deltaTime = Time.deltaTime;
                SendJson(msg);
            }
        }
    }
}