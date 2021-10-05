using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Logging;
using Ubiq.Rooms;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    [RequireComponent(typeof(LogCollector))]
    [RequireComponent(typeof(LatencyMeter))]
    public class PerformanceMonitor : MonoBehaviour
    {
        public RoomClient RoomClient;
        private LogCollector collector;
        private LatencyMeter meter;
        private Queue<IPeer> peersToPing;

        // Local event logger
        private UserEventLogger info;

        public bool Measure;
        private float lastPingTime;

        private void Awake()
        {
            if(Application.isBatchMode) // The performance monitor should only run in headed builds
            {
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
            info = new UserEventLogger(this);
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



                if (Time.time - lastPingTime > 0.25f)
                {
                    if (peersToPing.Count > 0)
                    {
                        var peer = peersToPing.Dequeue();
                        meter.MeasurePeerLatencies(peer);
                    }

                    lastPingTime = Time.time;
                    info.Log("RoomInfo", RoomClient.Peers.Count());
                }
            }
        }


    }
}