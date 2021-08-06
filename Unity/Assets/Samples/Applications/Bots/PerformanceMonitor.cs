using System.Collections;
using System.Collections.Generic;
using Ubiq.Logging;
using Ubiq.Rooms;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    [RequireComponent(typeof(ThroughputMonitor))]
    [RequireComponent(typeof(LogCollector))]
    public class PerformanceMonitor : MonoBehaviour
    {
        public RoomClient Peer;
        private LogCollector collector;
        private LatencyMeter meter;
        private ThroughputMonitor throughput;

        public bool IsMeasuring { get; private set; }
        private float lastPingTime;

        private void Awake()
        {
            meter = Peer.GetComponentInChildren<LatencyMeter>();
            collector = GetComponent<LogCollector>();
            throughput = GetComponent<ThroughputMonitor>();
        }

        public void StartMeasurements()
        {
            collector.StartCollection();
            IsMeasuring = true;
        }

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
                    throughput.Sample();
                }
            }
        }
    }
}