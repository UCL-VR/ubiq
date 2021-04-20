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

        private byte[] workingArea;
        private Stream outputStream;

        public int Count => entries;

        private int entries;
        private byte[] startArray;
        private byte[] endArray;
        private byte[] seperator;

        private NetworkContext context;

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
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var logmessage = new LogManagerMessage(message);
            switch (logmessage.Header[0])
            {
                case 0x1:
                    {
                        Push(logmessage.Bytes);
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
            workingArea = new byte[0];
            startArray = Encoding.UTF8.GetBytes("[");
            endArray = Encoding.UTF8.GetBytes("]");
            seperator = Encoding.UTF8.GetBytes(",\n");
        }

        // Start is called before the first frame update
        void Start()
        {
            FindLocalManagers();
            OpenFileStream();
            try
            {
                context = NetworkScene.Register(this);
            }
            catch(NullReferenceException)
            {
                // The LogCollector does not have to be in a NetworkScene to have some functionality
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

        /// <summary>
        /// Receive a Span containing a structured log message. While the Span is a byte type, this method assumes it is UTF8.
        /// </summary>
        /// <remarks>
        /// This method will take a copy of record before returning, so - as implied by the Span argument - it is safe to call
        /// with reference counted argumnets. The Span will also prevent this being called across yield or await boundaries, which
        /// is not supported.
        /// </remarks>
        public void Push(ReadOnlySpan<byte> record)
        {
            if(record.Length > workingArea.Length)
            {
                Array.Resize(ref workingArea, record.Length); // Until Unity updates to Core 2.1 
            }

            if(outputStream != null)
            {
                if(entries > 0)
                {
                    outputStream.Write(seperator, 0, seperator.Length);
                }

                record.CopyTo(new Span<byte>(workingArea));
                outputStream.Write(workingArea, 0, record.Length);
                outputStream.FlushAsync(); // This is mainly so other programs can read the logs while Ubiq is running; we don't care about the progress of the flush.
                entries++;
            }
        }

        private string Filepath(int i)
        {
            return Path.Combine(Application.persistentDataPath, $"ubiqlog_{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}_{i}.json");
        }

        public void OpenFileStream()
        {
            int i = 0;
            string filename = Filepath(i);
            while (File.Exists(filename))
            {
                filename = Filepath(i++);
            }

            outputStream = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
            entries = 0;
            outputStream.Write(startArray, 0, startArray.Length);
        }

        private void OnDestroy()
        {
            outputStream.Write(endArray, 0, endArray.Length);
            outputStream.Close();
            outputStream = null;
        }
    }

}
