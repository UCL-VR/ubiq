using System;
using System.IO;
using System.Text;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.Logging
{
    /// <summary>
    /// Receives log events from LogManagaers throughout the network and writes them to a native Stream.
    /// LogCollector will recieve events from local and remote LogManagers automatically; the LogManagers
    /// however must be configured to transmit. LogCollector can instruct LogManagers to begin transmission.
    /// The Stream written by the LogCollector is standard-compliant Json - log events are put into a top
    /// level Json array.
    /// </summary>
    /// <remarks>
    /// The LogCollector writes a FileStream internally to the persistent data storage location of the platform.
    /// This is for convenience (only one Component needed to start with), though it may be desirable to
    /// re-direct this Stream to another destination.
    /// </remarks>
    [NetworkComponentId(typeof(LogCollector), ComponentId)]
    public class LogCollector : MonoBehaviour, INetworkObject, INetworkComponent
    {
        public static NetworkId Id = new NetworkId("fc26-78b8-4498-9953");
        NetworkId INetworkObject.Id => Id;

        public const ushort ComponentId = 0;

        public int Count { get; private set; }

        private IOutputStream[] outputStreams;
        private NetworkContext context;

        public bool Collecting { get; private set; }

        /// <summary>
        /// The LogCollector can operate entirely locally. If this is True it means the Collector has network connectivity and messages will be exchanged with remote LogManagers.
        /// </summary>
        public bool NetworkEnabled
        {
            get
            {
                return context != null;
            }
        }

        /// <summary>
        /// Instructs all the LogManager instances on the network to begin transmitting their logs (new, and backlogged)
        /// </summary>
        public void StartCollection()
        {
            if(context != null)
            {
                context.Send(LogManager.Id, LogManager.ComponentId, LogManagerMessage.Rent("StartTransmitting"));
            }
            Collecting = true;
        }

        /// <summary>
        /// Instructs all the LogManager instances on the network to stop transmitting their logs. Any new logs will be cached locally again.
        /// </summary>
        public void StopCollection()
        {
            if (context != null)
            {
                context.Send(LogManager.Id, LogManager.ComponentId, LogManagerMessage.Rent("StopTransmitting"));
            }
            Collecting = false;
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var logmessage = new LogManagerMessage(message);
            switch (logmessage.Header[0])
            {
                case 0x1:
                    {
                        Push(logmessage.Bytes, logmessage.Header[1]);
                    }
                    break;
                case 0x2:
                    {
                        switch (logmessage.ToString())
                        {
                            default:
                                Debug.LogException(new NotSupportedException($"Uknown LogManager message {logmessage.ToString()}"));
                                break;
                        }
                    }
                    break;
                default:
                    Debug.LogException(new NotSupportedException($"Uknown LogManager message type {logmessage.Header[0]}"));
                    break;
            }
        }

        private void Awake()
        {
            Count = 0;

            // Right now we only support two tag types; this may change in the future.
            // We create the FileOutputStreams before we know whether we will recieve events of that type - this is because the JsonFileOutputStream
            // doesn't create the file until it actually receives an event, so it is cheap to make these objects, and then we can rely on them 
            // always existing.

            outputStreams = new IOutputStream[3];
            outputStreams[(byte)EventType.Application] = new JsonFileOutputStream("Application");
            outputStreams[(byte)EventType.User] = new JsonFileOutputStream("User");

            Collecting = false;
        }

        // Start is called before the first frame update
        void Start()
        {
            FindLocalManagers();
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

            if(context != null)
            {
                var roomClient = context.scene.GetComponentInChildren<Rooms.RoomClient>();
                if(roomClient)
                {
                    roomClient.OnPeerAdded.AddListener(Peer =>
                    {
                        if (Collecting)
                        {
                            StartCollection();
                        }
                    });
                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }

        private void FindLocalManagers()
        {
            LogManager[] managers;
            if (transform.parent == null) // Is at the root of the scene graph.
            {
                managers = UnityEngine.Object.FindObjectsOfType<LogManager>();
            }
            else
            {
                var root = transform;
                while (root.parent != null)
                {
                    root = root.parent;
                }
                managers = root.GetComponentsInChildren<LogManager>();
            }

            foreach (var item in managers)
            {
                item.Register(this);
            }
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

        private IOutputStream ResolveOutputStream(byte tag)
        {
            return outputStreams[tag];
        }

        /// <summary>
        /// Receive a Span containing a structured log message. While the Span is a byte type, this method assumes it is UTF8.
        /// </summary>
        /// <remarks>
        /// This method will take a copy of record before returning, so - as implied by the Span argument - it is safe to call
        /// with reference counted argumnets. The Span will also prevent this being called across yield or await boundaries, which
        /// is not supported.
        /// </remarks>
        public void Push(ReadOnlySpan<byte> record, byte tag)
        {
            ResolveOutputStream(tag).Write(record);
            Count++;
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
    }

}
