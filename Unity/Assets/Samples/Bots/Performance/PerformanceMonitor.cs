using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Logging;
using Ubiq.Rooms;
using UnityEngine;
using Ubiq.Messaging;

namespace Ubiq.Samples.Bots
{
    /// <summary>
    /// The performance monitor records information about the bots and their 
    /// room to an Experiment log.
    /// </summary>
    [RequireComponent(typeof(LogCollector))]
    [RequireComponent(typeof(LatencyMeter))]
    public class PerformanceMonitor : MonoBehaviour
    {
        public RoomClient RoomClient;
        private LogCollector collector;
        private LatencyMeter meter;
        private Queue<IPeer> peersToPing;

        // Local event logger
        private ExperimentLogEmitter info;

        public bool Measure;
        private float lastPingTime;

        private void Awake()
        {
            if(Application.isBatchMode) 
            {
                Destroy(this);
                return;
            }

            if(!Application.isEditor)
            {
                Debug.LogWarning("Running Bots Sample in a headed build - the Performance Monitor is disabled to avoid confusion."); // The confusion being the *bots* will join the bots room, but not this entity, meaning there will be duplicate log files
                Destroy(this);
                return;
            }

            meter = RoomClient.GetComponentInChildren<LatencyMeter>();
            collector = GetComponent<LogCollector>();
            peersToPing = new Queue<IPeer>();
        }

        public void StartMeasurements()
        {
            Measure = true;
        }

        public int NumPeers => RoomClient.Peers.Count();

        // Start is called before the first frame update
        void Start()
        {
            info = new ExperimentLogEmitter(this);
            info.Log("Sync", DateTime.Now.Second + DateTime.Now.Hour * 60);
            collector.StartCollection();
        }

        // Update is called once per frame
        void Update()
        {
            if (Measure)
            {
                // Iterate over all peers doing a time-divided ping
                if(peersToPing.Count <= 0)
                {
                    foreach (var item in RoomClient.Peers)
                    {
                        peersToPing.Enqueue(item);
                    }
                }

                if (Time.time - lastPingTime > 0.1f)
                {
                    if (peersToPing.Count > 0)
                    {
                        var peer = peersToPing.Dequeue();
                        meter.MeasurePeerLatency(peer);
                    }

                    lastPingTime = Time.time;
                    info.Log("RoomInfo", RoomClient.Peers.Count());
                }
            }
        }


    }
}