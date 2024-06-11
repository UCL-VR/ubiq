using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Ubiq.Logging;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq.Messaging
{
    /// <summary>
    /// Works with a counterpart on other Peers to measure the Round Trip Time to that Peer and write them to an Event Log.
    /// </summary>
    public class LatencyMeter : MonoBehaviour
    {
        private NetworkId id;
        private NetworkScene scene;
        private RoomClient client;
        private Dictionary<string, Stopwatch> transmissionTimes = new Dictionary<string, Stopwatch>();

        [Serializable]
        public class MeasurementEvent : UnityEvent<Measurement> { }
        public MeasurementEvent OnMeasurement;

        private void Awake()
        {
            if(OnMeasurement == null)
            {
                OnMeasurement = new MeasurementEvent();
            }
        }

        // Start is called before the first frame update
        protected void Start()
        {
            client = RoomClient.Find(this);
            scene = NetworkScene.Find(this);
            id = NetworkId.Create(scene.Id, "LatencyMeter");
            scene.AddProcessor(id, ProcessMessage);
        }

        [Serializable]
        private struct Message
        {
            public NetworkId source; // The source and destination are the Peer NetworkScene Ids (for making it easier to associate logs with others afterwards)
            public NetworkId destination;
            public string token;
            public bool reply;
            public float deltaTime; // This is the frame interval at the remote PC; the RTT will be dependent on this so it is useful to have it too.
        }

        [Serializable]
        public struct Measurement
        {
            public NetworkId source;
            public NetworkId destination;
            public float time;
            public float frameTime;
        }

        public void MeasurePeerLatency(IPeer peer)
        {
            if (peer.uuid == client.Me.uuid)
            {
                return;
            }

            Message message;
            message.source = scene.Id;
            message.destination = peer.networkId;
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
                MeasurePeerLatency(peer);
            }
        }

        protected void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var msg = message.FromJson<Message>();
            if (msg.reply)
            {
                if (transmissionTimes.TryGetValue(msg.token, out var stopwatch))
                {
                    stopwatch.Stop();
                    OnMeasurement.Invoke(
                        new Measurement()
                        {
                            destination = msg.destination,
                            source = msg.source,
                            time = stopwatch.ElapsedMilliseconds,
                            frameTime = msg.deltaTime
                        }
                    );
                }
                transmissionTimes.Remove(msg.token);
            }
            else
            {
                var src = msg.source;
                msg.source = msg.destination;
                msg.destination = src;
                msg.reply = true;
                msg.deltaTime = Time.deltaTime;
                SendJson(msg);
            }
        }

        private void SendJson(Message msg)
        {
            scene.SendJson(NetworkId.Create(msg.destination, "LatencyMeter"), msg);
        }
    }
}