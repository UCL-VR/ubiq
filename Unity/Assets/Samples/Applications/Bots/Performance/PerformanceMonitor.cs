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

        // Local event logger
        private UserEventLogger info;

        public bool IsMeasuring { get; private set; }
        private float lastPingTime;

        private void Awake()
        {
            meter = RoomClient.GetComponentInChildren<LatencyMeter>();
            collector = GetComponent<LogCollector>();
        }

        public void StartMeasurements()
        {
            info = new UserEventLogger(this);
            collector.StartCollection();
            IsMeasuring = true;
        }

        public int NumPeers => RoomClient.Peers.Count();

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            if (IsMeasuring)
            {
                if (Time.time - lastPingTime > 1f)
                {
                    lastPingTime = Time.time;
                    meter.MeasurePeerLatencies();
                    info.Log("RoomInfo", RoomClient.Peers.Count());
                }
            }
        }
    }
}