using System;
using System.IO;
using System.Text;
using System.Threading;
using Ubiq.Messaging;
using Ubiq.Logging.Utf8Json;
using Ubiq.Networking.JmBucknall.Structures;
using Ubiq.Rooms;
using Ubiq.Extensions;
using UnityEngine;
using System.Collections.Generic;

namespace Ubiq.Logging
{
    /// <summary>
    /// Receives local and remote log events and writes them to a Stream. Events 
    /// are collected when StartCollection is called. Once StartCollection has 
    /// been called, all logs from the Peer group will be sent to that collector.
    /// </summary>
    /// <remarks>
    /// The LogCollector writes a FileStream internally to the persistent data 
    /// storage location of the platform. This is for convenience (only one 
    /// Component needed to start with), though it may be desirable to re-direct 
    /// this Stream to another destination.
    /// </remarks>
    [NetworkComponentId(typeof(LogCollector), ComponentId)]
    public class LogCollector : MonoBehaviour, INetworkComponent
    {
        public const ushort ComponentId = 3;

        public enum OperatingMode
        {
            Buffering,
            Forwarding,
            Writing
        }

        public int Written { get; private set; }

        public int Memory;
        public int MaxMemory = 1024 * 1024 * 50;

        /// <summary>
        /// Stores local events until they are written or forwarded
        /// </summary>
        private LockFreeQueue<ReferenceCountedSceneGraphMessage> events = new LockFreeQueue<ReferenceCountedSceneGraphMessage>();

        private IOutputStream[] outputStreams;
        private NetworkContext context;
        private RoomClient roomClient;

        /// <summary>
        /// The NetworkSceneId of the Active collector. If this is null, events should be cached,
        /// otherwise, they should be forwarded to this Peer.
        /// </summary>
        private NetworkId destinationCollector = new NetworkId(0);

        /// <summary>
        /// The logical clock that ensures snapshot messages will eventually converge on one peer.
        /// </summary>
        private int clock = 0;

        public OperatingMode Mode
        {
            get
            {
                if(destinationCollector == Id)
                {
                    return OperatingMode.Writing;
                }
                if (destinationCollector)
                {
                    return OperatingMode.Forwarding;
                }
                return OperatingMode.Buffering;
            }
        }

        public bool isPrimary
        {
            get 
            { 
                return Mode == OperatingMode.Writing; 
            }
        }

        public NetworkId Id
        {
            get
            {
                if (context != null)
                {
                    return context.networkObject.Id;
                }
                else
                {
                    return NetworkId.Null;
                }
            }
        }

        public bool OnNetwork
        {
            get
            {
                return context != null;
            }
        }

        private struct CC
        {
            public int clock;
            public NetworkId state;
        }

        /// <summary>
        /// Registers this Collector as the primary LogCollector for the Peer Group. All cached and new logs will be forwarded
        /// to this Collector. All new Peers will see this as the active Collector.
        /// </summary>
        public void StartCollection()
        {
            SendSnapshot(Id);
        }

        /// <summary>
        /// Stops collecting and surrenders its position as the primary Collector.
        /// </summary>
        public void StopCollection()
        {
            SendSnapshot(NetworkId.Null);
        }

        private void SendSnapshot(NetworkId newDestination)
        {
            destinationCollector = newDestination;
            clock++;

            var msg = new CC();
            msg.clock = clock;
            msg.state = Id;

            foreach (var item in roomClient.Peers)
            {
                context.Send(item.NetworkObjectId, ComponentId, LogCollectorMessage.RentCommandMessage(msg));
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var logmessage = new LogCollectorMessage(message);
            switch (logmessage.Type)
            {
                case LogCollectorMessage.MessageType.Event:
                    {
                        message.Acquire();
                        Push(message);
                    }
                    break;
                case LogCollectorMessage.MessageType.Command:
                    {
                        // Changing the Log Collector is performed using a "Global Snapshot".
                        // A message is sent with a logical clock value. If it exceeds the current
                        // shared value, then state is updated.
                        //
                        // If two Peers in are in a race condition, there may be clock collisions.
                        // In this case the initiating Peers will detect this (because they will
                        // receive eachothers' conflicting messages), and perform resolution
                        // by each adding a random value to the clock and re-transmitting.
                        //
                        // The assumption is that all messages are reliably delivered. If this is
                        // true, the System will converge to a Common State after all messages
                        // have been received (the State with the highest overall clock value).
                        //
                        // If a LogCollector ever volunteers as the Primary, it must be prepared
                        // to receive messages until all peers Converge to a new Collector. This
                        // is an indeterminate amount of time, for which Ubiq does not specify an
                        // upper bound, so be careful when nominating a LogCollector.

                        var cck = logmessage.FromJson<CC>();

                        if(cck.clock > clock)
                        {
                            // The Initiator has full knowledge, and wishes to change the destination

                            destinationCollector = cck.state;
                            clock = cck.clock;
                        }
                        else
                        {
                            if(cck.clock == clock && isPrimary)
                            {
                                // Another Initiator has created an update that conflicts with ours, at the same time.
                                // Re-transmit the state update with a random interval. They will do the same. Whoever
                                // re-sends with the higher value wins.

                                clock += UnityEngine.Random.Range(0, 10);
                                SendSnapshot(Id);
                            }
                        }
                    }
                    break;
                default:
                    Debug.LogException(new NotSupportedException($"Unknown LogManager message type {(int)logmessage.Type}"));
                    break;
            }
        }

        private void Awake()
        {
            Written = 0;
            roomClient = this.GetClosestComponent<RoomClient>();

            // We create some FileOutputStreams before we know whether we will recieve events of that type - this is because the JsonFileOutputStream
            // doesn't create the file until it actually receives an event, so it is cheap to make these objects.

            outputStreams = new IOutputStream[255];
            outputStreams[(byte)EventType.Application] = new JsonFileOutputStream("Application");
            outputStreams[(byte)EventType.Experiment] = new JsonFileOutputStream("Experiment");
            outputStreams[(byte)EventType.Info] = new JsonFileOutputStream("Info");
            outputStreams[(byte)EventType.Debug] = new JsonFileOutputStream("Debug");
        }

        // Start is called before the first frame update
        void Start()
        {
            try
            {
                context = NetworkScene.Register(this);
            }
            catch(NullReferenceException)
            {
                // The LogCollector does not have to be in a NetworkScene to have some functionality
            }

            // This snippet listens for new peers so the start transmitting message can be re-transmitted if necessary,
            // ensuring all new peers in the room transmit immediately and relieve the user of having to worry about 
            // peer lifetimes once they start collection.

            roomClient.OnPeerAdded.AddListener(Peer =>
            {
                if (isPrimary)
                {
                    StartCollection();
                }
            });
        }

        void Update()
        {
            if (destinationCollector) // There is an Active Collector
            {
                ReferenceCountedSceneGraphMessage message;
                while (events.Dequeue(out message))
                {
                    if (destinationCollector == context.networkObject.Id)
                    {
                        // We are the Active Collector, so log the event directly

                        var logEvent = new LogCollectorMessage(message);
                        ResolveOutputStream(logEvent.Tag).Write(logEvent.Bytes);
                        Written++;
                        message.Release();
                    }
                    else
                    {
                        // The Active Collector is elsewhere, so forward the event

                        message.objectid = destinationCollector;
                        message.componentid = ComponentId;
                        context.Send(message);
                    }

                    Interlocked.Add(ref Memory, -message.length);
                }
            }

            // If there is no active collector, continue to cache the events...
        }

        private void Push(ReferenceCountedSceneGraphMessage message) // Though we accept a ReferenceCountedSceneGraphMessage its assumed that it will always be a LogCollectorMessage 
        {
            events.Enqueue(message);
            Interlocked.Add(ref Memory, message.length);
        }

        public void Push(ref JsonWriter writer)
        {
            Push(LogCollectorMessage.Rent(writer.GetSpan(), writer.Tag)); // Repurpose the message pool functionality to cache log events           
        }

        private IOutputStream ResolveOutputStream(byte tag)
        {
            if(outputStreams[tag] == null)
            {
                outputStreams[tag] = new JsonFileOutputStream(tag.ToString());
            }
            return outputStreams[tag];
        }

        private interface IOutputStream : IDisposable
        {
            void Write(ReadOnlySpan<byte> record);
        }

        private class JsonFileOutputStream : IOutputStream
        {
            Stream outputStream;

            private byte[] startArray;
            private byte[] endArray;
            private byte[] seperator;
            private int entries;
            private byte[] workingArea;
            private string postfix;

            public JsonFileOutputStream(string postfix)
            {
                startArray = Encoding.UTF8.GetBytes("[");
                endArray = Encoding.UTF8.GetBytes("]");
                seperator = Encoding.UTF8.GetBytes(",\n");

                workingArea = new byte[0];
                entries = 0;
                this.postfix = postfix;
            }

            public void Write(ReadOnlySpan<byte> record)
            {
                if (record.Length > workingArea.Length)
                {
                    Array.Resize(ref workingArea, record.Length); // Until Unity updates to Core 2.1 so Streams can take a Span
                }

                if(outputStream == null)
                {
                    int i = 0;
                    string filename = Filepath(i);
                    while (File.Exists(filename))
                    {
                        filename = Filepath(i++);
                    }

                    outputStream = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
                    outputStream.Write(startArray, 0, startArray.Length);
                }

                if (entries > 0)
                {
                    outputStream.Write(seperator, 0, seperator.Length);
                }

                record.CopyTo(new Span<byte>(workingArea));
                outputStream.Write(workingArea, 0, record.Length);
                outputStream.Flush(); // This is mainly so other programs can read the logs while Ubiq is running; we don't care about the progress of the flush.
                entries++;
            }

            private string Filepath(int i)
            {
                return Path.Combine(Application.persistentDataPath, $"{postfix}_log_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}_{i}.json");
            }

            public void Dispose()
            {
                if (outputStream != null)
                {
                    outputStream.Write(endArray, 0, endArray.Length);
                    outputStream.Close();
                    outputStream = null;
                }
            }
        }

        private void OnDestroy()
        {
            if (outputStreams != null)
            {
                for (int i = 0; i < outputStreams.Length; i++)
                {
                    if(outputStreams[i] != null)
                    {
                        outputStreams[i].Dispose();
                        outputStreams[i] = null;
                    }
                }
            }
        }

        public static LogCollector Find(MonoBehaviour component)
        {
            return component.GetClosestComponent<LogCollector>();
        }

        public void Register(LogEmitter emitter)
        {
            emitter.collector = this;
        }
    }

}
