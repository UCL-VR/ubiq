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
using System.Collections;

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
    public class LogCollector : MonoBehaviour
    {
        public NetworkId Id { get; private set; }
        public NetworkId BroadcastId { get; private set; } = new NetworkId("685a5b84-fec057d0");

        public NetworkId Destination { get { return destination; } }

        public EventType EventsFilter = (EventType)(-1); // -1 in 2's complement will set all bits to 1.

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
       
        private NetworkScene networkScene;
        public NetworkScene NetworkScene
        {
            get
            {
                if (!networkScene)
                {
                    networkScene = NetworkScene.Find(this);
                }

                return networkScene;
            }
        }

        /// <summary>
        /// Stores whether the Active Collector has at some point changed
        /// between one live Collector and another. If there has only ever been
        /// one Active Collector, we can say we have full visibility on all the
        /// events that flow to it.
        /// </summary>
        private bool activeCollectorChanged = false;

        /// <summary>
        /// The NetworkSceneId of the Active collector. If this is null, events
        /// should be cached, otherwise, they should be forwarded to this Peer.
        /// </summary>
        private NetworkId destination = new NetworkId(0);

        /// <summary>
        /// The logical clock that ensures snapshot messages will eventually
        /// converge on one peer.
        /// </summary>
        private int clock = 0;

        private struct PingRequest
        {
            public Action<PingResult> callback;
            public float time;
        }

        private Dictionary<string, PingRequest> pings = new Dictionary<string, PingRequest>();

        public OperatingMode Mode
        {
            get
            {
                if(destination == Id)
                {
                    return OperatingMode.Writing;
                }
                if (destination)
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

        public bool OnNetwork
        {
            get
            {
                return NetworkScene != null;
            }
        }

        private struct CC
        {
            public int clock;
            public NetworkId state;
        }

        private struct PingMessage
        {
            public NetworkId Source; // Id of the Source of the Request
            public NetworkId Responder; // Id of the Collector that Responded (usually the Active Collector)
            public string Token; // Unique Id of this request that should be reflected back
            public int Written; // Stats of the Active Collector if found
            public bool Aborted; // True if the responder is not the Active Collector
        }

        public struct PingResult
        {
            public NetworkId ActiveCollector;
            public float StartTime;
            public float EndTime;
            public int Written;
            public bool Aborted;
        }

        /// <summary>
        /// Registers this Collector as the primary LogCollector for the Peer
        /// Group. All cached and new logs will be forwarded to this Collector.
        /// All new Peers will see this as the primary Collector.
        /// </summary>
        public void StartCollection()
        {
            SendSnapshot(Id);
        }

        /// <summary>
        /// Stops collecting and surrenders its position as the primary
        /// Collector.
        /// </summary>
        /// <remarks>
        /// It will take time for this message to propagate, during this
        /// time this Component should be prepared to receive events
        /// otherwise messages may be lost.
        /// </remarks>
        public void StopCollection()
        {
            SendSnapshot(NetworkId.Null);
        }

        private void SendSnapshot(NetworkId newDestination)
        {
            SetDestination(newDestination);
            clock++;

            var msg = new CC();
            msg.clock = clock;
            msg.state = newDestination;

            Send(msg);
        }

        private void Send(CC msg)
        {
            // Command messages are sent to everyone
            Send(BroadcastId, LogCollectorMessage.RentCommandMessage(msg));
        }

        private void Send(NetworkId target, PingMessage msg)
        {
            Send(target, LogCollectorMessage.RentPingMessage(msg));
        }

        private void Send(NetworkId destination, ReferenceCountedSceneGraphMessage msg)
        {
            if (NetworkScene)
            {
                NetworkScene.Send(destination, msg);
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

                            SetDestination(cck.state);
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
                case LogCollectorMessage.MessageType.Ping:
                    {
                        var ping = logmessage.FromJson<PingMessage>();
                        if (ping.Responder)
                        {
                            // The ping is a response to a request that we originated

                            if (pings.ContainsKey(ping.Token))
                            {
                                var request = pings[ping.Token];
                                pings.Remove(ping.Token);
                                PingResult result;
                                result.Aborted = ping.Aborted;
                                result.ActiveCollector = ping.Responder;
                                result.StartTime = request.time;
                                result.EndTime = Time.time;
                                result.Written = ping.Written;
                                request.callback(result);
                            }
                            else
                            {
                                Debug.LogError("Received Ping Response with Invalid Token.");
                            }
                        }
                        else // Is a Request...
                        {
                            // We have received a Ping. Pings are meant to be replied to by the Active Collector, so
                            // what happens next depends on where that is..

                            if (isPrimary)
                            {
                                // We are the active collector so reply directly.

                                ping.Responder = Id;
                                ping.Written = Written;
                                Send(ping.Source,ping);
                            }
                            else if (destination)
                            {
                                // If the active collector has changed since the Ping was created, forward it.

                                message.Acquire();
                                Send(destination,message);
                            }
                            else
                            {
                                // There is no active collector and yet we've received a ping. This is not good, but could occur where there was
                                // an Active Collector but it failed. Return a warning.

                                ping.Responder = Id;
                                ping.Aborted = true;
                                Send(ping.Source,ping);
                            }
                        }
                    }
                    break;
                default:
                    Debug.LogException(new NotSupportedException($"Unknown LogManager message type {(int)logmessage.Type}"));
                    break;
            }
        }

        private void SetDestination(NetworkId newDestination)
        {
            if(destination && destination != newDestination)
            {
                activeCollectorChanged = true;
            }
            destination = newDestination;
        }

        private void Awake()
        {
            Written = 0;
            CreateFileOutputStreams();
        }

        public void CreateFileOutputStreams()
        {
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
            if (NetworkScene) // The LogCollector does not have to be in a NetworkScene to have some functionality
            {
                Id = CollectorId(NetworkScene);
                NetworkScene.AddProcessor(Id, ProcessMessage);
                NetworkScene.AddProcessor(BroadcastId, ProcessMessage);

                var roomClient = NetworkScene.GetComponentInChildren<RoomClient>();

                // This snippet listens for new peers so the start transmitting message can be re-transmitted if necessary,
                // ensuring all new peers in the room transmit immediately and relieve the user of having to worry about
                // peer lifetimes once they start collection.

                if (roomClient)
                {
                    roomClient.OnPeerAdded.AddListener(peer =>
                    {
                        if (isPrimary)
                        {
                            StartCollection();
                        }
                    });

                    roomClient.OnPeerRemoved.AddListener(peer =>
                    {
                        if (destination == this.CollectorId(peer))
                        {
                            // The Peer that went away was the Active Collector. This is unexpected in a bad way. Stop transmitting logs until someone else volunteers.
                            SetDestination(NetworkId.Null);

                            // Reset the clock as a fix for now to handle the case where the active collector leaves and rejoins as a new process.
                            clock = 0;
                        }
                    });
                }
            }
        }

        private NetworkId CollectorId(IPeer peer)
        {
            return NetworkId.Create(peer.networkId, "LogCollector");
        }

        private NetworkId CollectorId(NetworkScene peer)
        {
            return NetworkId.Create(peer.Id, "LogCollector");
        }

        private void Update()
        {
            if (destination) // There is an Active Collector
            {
                ReferenceCountedSceneGraphMessage message;
                while (events.Dequeue(out message))
                {
                    if (destination == Id)
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

                        Send(destination,message);
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

        public bool ShouldLog(EventType tag)
        {
            return (((sbyte)EventsFilter & (sbyte)tag) > 0);
        }

        /// <summary>
        /// Returns the number of Events with Tag in the Buffer. When this is zero, the LogCollector
        /// has transmitted or written all Events.
        /// </summary>
        /// <remarks>
        /// This method returns the count at a particular moment in time, that may even be the past
        /// by the time the value is used.
        /// This method is only useful when leveraged with other information, such as the fact that
        /// no new Events of a particular type will be raised.
        /// </remarks>
        public int GetBufferedEventCount(EventType tag)
        {
            int count = 0;
            foreach (var item in events)
            {
                var logEvent = new LogCollectorMessage(item);
                if(logEvent.Tag == (byte)tag)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Pings the Active Collector. When a response is received, response is called with the
        /// elapsed local round-trip time.
        /// If this is the Active Collector, or there is no Active Collector, response is called
        /// immediately with a time of zero.
        /// </summary>
        /// <remarks>
        /// There are no guarantees on the response times of the Collector.
        /// </remarks>
        public void Ping(Action<PingResult> response)
        {
            if(isPrimary)
            {
                PingResult result;
                result.Aborted = false;
                result.ActiveCollector = Id;
                result.StartTime = Time.time;
                result.EndTime = Time.time;
                result.Written = Written;
                response(result);
            }
            else if(!destination)
            {
                PingResult result;
                result.Aborted = true;
                result.ActiveCollector = NetworkId.Null;
                result.StartTime = Time.time;
                result.EndTime = Time.time;
                result.Written = -1;
                response(result);
            }
            else
            {
                PingMessage ping;
                ping.Source = Id;
                ping.Responder = NetworkId.Null;
                ping.Token = Guid.NewGuid().ToString();
                ping.Written = 0;
                ping.Aborted = false;

                PingRequest request;
                request.callback = response;
                request.time = Time.time;
                pings.Add(ping.Token, request);

                Send(destination, ping);
            }
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

            if (NetworkScene)
            {
                NetworkScene.RemoveProcessor(Id, ProcessMessage);
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

        /// <summary>
        /// Checks when all events of event type have been successfully
        /// transmitted.
        /// This can be used to check if all, for example, Experiment events
        /// have been uploaded before exiting an application.
        /// Beware that this method is only accurate when the integrity of
        /// every LogCollector method is visible. See the Logging documentation
        /// for more details.
        /// </summary>
        public void WaitForTransmitComplete(EventType eventType, Action<bool> callback)
        {
            StartCoroutine(WaitForTransmitCompleteCoroutine(eventType, callback));
        }

        private IEnumerator WaitForTransmitCompleteCoroutine(EventType eventType, Action<bool> callback)
        {
            while(GetBufferedEventCount(eventType) > 0)
            {
                yield return null;
            }

            Ping(result =>
            {
                callback(!result.Aborted && !activeCollectorChanged);
            });
        }
    }

}
