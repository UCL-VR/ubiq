using System.Collections;
using System.Collections.Generic;
using Ubiq.Logging;
using Ubiq.Messaging;
using Ubiq.Voip;
using UnityEngine;

namespace Ubiq.Samples.Bots
{
    /// <summary>
    /// Registers to the NetworkScene and VoipManager to instrument the bandwidth in and out for this Peer.
    /// Summaries are transmitted using the ContextEventLogger.
    /// If Components create their own Connections, or VoipPeerConnections outside the VoipPeerConnectionManager,
    /// they must be manually added to be recorded.
    /// </summary>
    public class ThroughputMonitor : MonoBehaviour
    {
        private InfoLogEmitter info;
        private NetworkScene scene;
        private List<VoipPeerConnection> openVoipConnections;
        private List<VoipPeerConnection> toRemove;

        /// <summary>
        /// Summary statistics on traffic in and out of a NetworkScene. The seperate message and byte counts allow the
        /// messaging overheads to be calculated.
        /// </summary>
        struct Report
        {
            public uint MessagesSent;
            public uint MessagesReceived;
            public uint MessageBytesReceived;
            public uint MessageBytesSent;
            public uint RtcBytesReceived;
            public uint RtcBytesSent;
        }

        private void Awake()
        {
            openVoipConnections = new List<VoipPeerConnection>();
            toRemove = new List<VoipPeerConnection>();
        }

        // Start is called before the first frame update
        void Start()
        {
            scene = NetworkScene.FindNetworkScene(this);
            info = new InfoLogEmitter(this);

            var voipManager = scene.GetComponentInChildren<VoipPeerConnectionManager>();
            if(voipManager)
            {
                voipManager.OnPeerConnection.AddListener((VoipPeerConnection pc) =>
                {
                    AddVoipConnection(pc);
                },
                true);
            }
        }

        public void AddVoipConnection(VoipPeerConnection pc)
        {
            openVoipConnections.Add(pc);
        }

        public void Sample()
        {
            var report = new Report();

            foreach (var pc in openVoipConnections)
            {
                if (pc)
                {
                    var stats = pc.GetStatistics();
                    report.RtcBytesReceived += stats.Audio.BytesReceived;
                    report.RtcBytesSent += stats.Audio.BytesSent;
                }
                else
                {
                    toRemove.Add(pc);
                }
            }
            foreach (var pc in toRemove)
            {
                openVoipConnections.Remove(pc);
            }
            toRemove.Clear();

            report.MessageBytesReceived = scene.Statistics.BytesReceived;
            report.MessageBytesSent = scene.Statistics.BytesSent;
            report.MessagesSent = scene.Statistics.MessagesSent;
            report.MessagesReceived = scene.Statistics.MessagesReceived;

            info.Log("Throughput", scene.Id, report.MessagesSent, report.MessagesReceived, report.MessageBytesSent, report.MessageBytesReceived, report.RtcBytesSent, report.RtcBytesReceived);
        }
    }
}