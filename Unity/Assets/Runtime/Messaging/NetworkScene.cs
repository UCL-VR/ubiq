using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System;
using Ubiq.Networking;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Profiling;
using System.Linq;
using Ubiq.Logging;

namespace Ubiq.Messaging
{
    /// <summary>
    /// The Network Scene connects networked Components to Network Connections.
    /// </summary>
    public class NetworkScene : MonoBehaviour
    {
        private List<INetworkConnection> connections = new List<INetworkConnection>();
        private List<Action> actions = new List<Action>();
        private Dictionary<NetworkId, List<Action<ReferenceCountedSceneGraphMessage>>> processorCollections = new Dictionary<NetworkId, List<Action<ReferenceCountedSceneGraphMessage>>>();

        private LogEmitter events;

        public struct MessageStatistics
        {
            public uint BytesSent;
            public uint BytesReceived;
            public uint MessagesSent;
            public uint MessagesReceived;
        }

        private MessageStatistics statistics;

        /// <summary>
        /// Statistics on the number of messages and bytes sent and received by this NetworkScene.
        /// </summary>
        /// <remarks>
        /// The statistics are before network fanout (i.e. one message from one component counts
        /// as one message here, even if it is duplicated across two or more connections).
        /// Byte counts include the Message Headers (Object and Component Ids) but not connection
        /// protocol headers or prefixes.
        /// </remarks>
        public MessageStatistics Statistics { get => statistics; }
        public NetworkId Id { get; } = NetworkScene.GenerateUniqueId();

        private static NetworkScene rootNetworkScene;

        private void Awake()
        {
            if (transform.parent == null)
            {
                if (rootNetworkScene == null)
                {
                    rootNetworkScene = this;
                    DontDestroyOnLoad(gameObject);
                    Extensions.MonoBehaviourExtensions.DontDestroyOnLoadGameObjects.Add(gameObject);
                }
                else // Only one networkscene can exist at the top level of the hierarchy
                {
                    gameObject.SetActive(false); // Deactivate the branch to avoid Start() being called until the branch is destroyed
                    Destroy(gameObject);
                }
            }
            else // the network scene is in a forest
            {
                foreach (var item in GetComponents<NetworkScene>())
                {
                    if(item != this)
                    {
                        Destroy(item);
                    }
                }
            }

            events = new ComponentLogEmitter(this);
            events.Log("Awake", SystemInfo.deviceName, SystemInfo.deviceModel, SystemInfo.deviceUniqueIdentifier);
        }

        public void AddProcessor (NetworkId id, Action<ReferenceCountedSceneGraphMessage> processor)
        {
            List<Action<ReferenceCountedSceneGraphMessage>> processors;
            if (!processorCollections.TryGetValue(id, out processors))
            {
                processors = new List<Action<ReferenceCountedSceneGraphMessage>>();
                processorCollections.Add(id,processors);
            }

            processors.Add(processor);
        }

        public void RemoveProcessor (NetworkId id, Action<ReferenceCountedSceneGraphMessage> processor)
        {
            if (processorCollections.TryGetValue(id, out var processors))
            {
                processors.Remove(processor);
                if (processors.Count == 0)
                {
                    processorCollections.Remove(id);
                }
            }
        }

        public static NetworkScene FindNetworkScene(MonoBehaviour component)
        {
            return FindNetworkScene(component.transform);
        }

        public static NetworkScene FindNetworkScene(Transform component)
        {
            // Check if the scene is simply a parent, or if we can find a root scene.
            var scene = component.GetComponentInParent<NetworkScene>();
            if (scene)
            {
                return scene;
            }
            if(rootNetworkScene != null)
            {
                return rootNetworkScene;
            }

            // Check each common ancestor to find cousin scenes

            do
            {
                scene = component.GetComponentInChildren<NetworkScene>();
                if (scene)
                {
                    return scene;
                }
                component = component.parent;
            } while (component != null);

            return null;
        }

        public static NetworkId GenerateUniqueId()
        {
            return IdGenerator.GenerateUnique();
        }

        public void AddConnection(INetworkConnection connection)
        {
            connections.Add(connection);
        }

        private void Update()
        {
            foreach (var action in actions)
            {
                action();
            }
            actions.Clear();

            ReceiveConnectionMessages();
        }

        /// <summary>
        /// This checks all connections for messages and fans them out into the individual receive queues
        /// </summary>
        public void ReceiveConnectionMessages()
        {
            ReferenceCountedMessage m;
            foreach(var c in connections)
            {
                while (true)
                {
                    m = c.Receive();
                    if (m == null)
                    {
                        break;
                    }

                    var sgbmessage = new ReferenceCountedSceneGraphMessage(m);
                    if (processorCollections.TryGetValue(sgbmessage.objectid, out var processors))
                    {
                        foreach(var processor in processors)
                        {
                            processor(sgbmessage);
                        }
                    }
                }
            }
        }

        private void Send(ReferenceCountedMessage message)
        {
            foreach (var c in connections)
            {
                message.Acquire();
                c.Send(message);
            }
            message.Release(); // message should have been acquired once on creation.
        }

        public void Send(NetworkId objectid, ReferenceCountedSceneGraphMessage message)
        {
            message.objectid = objectid;
            Send(message.buffer);
        }

        public void Send(NetworkId objectid, string message)
        {
            var msg = ReferenceCountedSceneGraphMessage.Rent(message);
            msg.objectid = objectid;
            Send(msg.buffer);
        }

        public void SendJson<T>(NetworkId objectid, T message)
        {
            Send(objectid, JsonUtility.ToJson(message));
        }

        private void OnDestroy()
        {
            foreach(var c in connections)
            {
                try
                {
                    c.Dispose();
                }
                catch
                {

                }
            }
            connections.Clear();
        }
    }
}