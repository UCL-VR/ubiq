using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Linq;
using Ubiq.Extensions;
using Ubiq.Messaging;
using Ubiq.Networking.JmBucknall.Structures;
using Ubiq.Logging.Utf8Json;
using UnityEngine;


namespace Ubiq.Logging
{
    /// <summary>
    /// The LogManager Component receives events from the local application and forwards them (if necessary).
    /// LogManager is a Network Component and so can interact over Ubiq, however it can also function outside
    /// of a room, or even messaging network, if it only has to run locally.
    /// </summary>
    /// <remarks>
    /// LogManagers have a static network Id, but have native internal identifiers.
    /// </remarks>
    [NetworkComponentId(typeof(LogManager), ComponentId)]
    public class LogManager : MonoBehaviour, INetworkComponent, INetworkObject
    {
        public static NetworkId Id = new NetworkId("92e9-e831-8281-2761");
        NetworkId INetworkObject.Id => Id;
        public const ushort ComponentId = 0;

        public EventType Listen = (EventType)(-1);

        public int Memory;
        public int MaxMemory = 1024 * 1024 * 50;

        private LockFreeQueue<JsonWriter> events;
        private List<LogCollector> localCollectors;

        private NetworkContext context;

        public enum LogManagerMode : int

        {
            /// <summary>
            /// Don't log anything. EventLoggers attached to this Manager will not log or serialise.
            /// </summary>
            Off = 0,
            /// <summary>
            /// Buffer log events until the Buffer is exhausted, at which point older log events will be dropped.
            /// </summary>
            Buffer = 1,
            /// <summary>
            /// Buffer log events indefinitely. A LogCollector still must be attached at some point to retrieve the logs.
            /// </summary>
            Hold = 2,
            /// <summary>
            /// Send all log events to LogCollectors on the network. No events are cached. If a LogCollector is not prepared beforehand, the log events will be dropped.
            /// </summary>
            Transmit = 3
        }

        public LogManagerMode Mode = LogManagerMode.Buffer;
        private LogManagerMode previousMode;

        // Start is called before the first frame update
        void Start()
        {
            context = NetworkScene.Register(this);
        }

        private void Awake()
        {
            previousMode = Mode;
            EnsureAwake();
        }

        // EventLoggers may be instantiated and used in Awake, or even in other Constructors. Therefore, this object should
        // be initialised on-demand.
        private void EnsureAwake()
        {
            if(events == null)
            {
                events = new LockFreeQueue<JsonWriter>();
            }

            if(localCollectors == null)
            {
                localCollectors = new List<LogCollector>();
            }
        }

        public void StartTransmitting()
        {
            previousMode = Mode;
            Mode = LogManagerMode.Transmit;
        }

        private void StopTransmitting()
        {
            Mode = previousMode;
        }

        // Update is called once per frame
        void Update()
        {
            bool transmitLocal = localCollectors.Any(c => c.Collecting);
            bool transmitRemote = Mode == LogManagerMode.Transmit;
            bool holdLogs = Mode == LogManagerMode.Hold;

            if (transmitLocal || transmitRemote)
            {
                JsonWriter writer;
                while (events.Dequeue(out writer))
                {
                    try
                    {
                        if (transmitRemote)
                        {
                            var message = LogManagerMessage.Rent(writer.GetSpan(), writer.Tag);
                            message.objectid = LogCollector.Id;
                            message.componentid = LogCollector.ComponentId;
                            context.Send(message);
                        }

                        if (transmitLocal)
                        {
                            foreach (var item in localCollectors)
                            {
                                item.Push(writer.GetSpan(), writer.Tag);
                            }
                        }
                    }
                    finally
                    {
                        Release(ref writer);
                    }
                }
            }

            if(!holdLogs)
            {
                while(Memory > MaxMemory) // If the queued events are taking up too much space, then dump them starting from the back of the queue
                {
                    JsonWriter writer;
                    if(events.Dequeue(out writer))
                    {
                        Release(ref writer);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private void Release(ref JsonWriter writer)
        {
            Interlocked.Add(ref Memory, -writer.BufferSize);
            writer.Dispose();
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var logmessage = new LogManagerMessage(message);
            switch (logmessage.Header[0])
            {
                case 0x2:
                    {
                        switch (logmessage.ToString())
                        {
                            case "StartTransmitting":
                                StartTransmitting();
                                break;
                            case "StopTransmitting":
                                StopTransmitting();
                                break;
                        }
                    }
                    break;
                default:
                    Debug.LogException(new NotSupportedException($"Uknown LogManager message type {logmessage.Header[0]}"));
                    break;
            };
        }

        public void Register(LogCollector collector)
        {
            EnsureAwake();
            localCollectors.Add(collector);
        }

        public void Register(EventLogger logger)
        {
            EnsureAwake();
            logger.manager = this;
        }

        public static LogManager Find(MonoBehaviour component)
        {
            return component.GetClosestComponent<LogManager>();
        }

        public void Push(ref JsonWriter writer)
        {
            events.Enqueue(writer);
            Interlocked.Add(ref Memory, writer.BufferSize);
        }
    }
}