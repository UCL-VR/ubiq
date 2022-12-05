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
using UnityEngine.Events;

namespace Ubiq.Messaging
{
    public struct NetworkContext
    {
        public NetworkScene Scene;
        public NetworkId Id;
        public MonoBehaviour Component;

        public void Send(string message)
        {
            Scene.Send(Id, message);
        }

        public void Send(ReferenceCountedSceneGraphMessage message)
        {
            Scene.Send(Id, message);
        }

        public void SendJson<T>(T message)
        {
            Scene.SendJson(Id, message);
        }
    }

    /// <summary>
    /// The Network Scene connects networked Components to Network Connections.
    /// </summary>
    public class NetworkScene : MonoBehaviour
    {
        private struct AutoProcessor
        {
            public NetworkContext context;
            public Action<ReferenceCountedSceneGraphMessage> processor;
            public AutoProcessor (NetworkContext context, Action<ReferenceCountedSceneGraphMessage> processor)
            {
                this.context = context;
                this.processor = processor;
            }
        }

        private List<INetworkConnection> connections = new List<INetworkConnection>();
        private List<Action> actions = new List<Action>();
        private Dictionary<NetworkId, List<Action<ReferenceCountedSceneGraphMessage>>> processorCollections = new Dictionary<NetworkId, List<Action<ReferenceCountedSceneGraphMessage>>>();
        private List<AutoProcessor> autoProcessors = new List<AutoProcessor>();

        private LogEmitter events;

        public struct MessageStatistics
        {
            public uint BytesSent;
            public uint BytesReceived;
            public uint MessagesSent;
            public uint MessagesReceived;
        }

        private MessageStatistics statistics;

        public UnityEvent OnUpdate = new UnityEvent();

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
                    if (item != this)
                    {
                        Destroy(item);
                    }
                }
            }

            events = new ComponentLogEmitter(this);
            events.Log("Awake", SystemInfo.deviceName, SystemInfo.deviceModel, SystemInfo.deviceUniqueIdentifier);
        }

        public void AddProcessor(NetworkId id, Action<ReferenceCountedSceneGraphMessage> processor)
        {
            List<Action<ReferenceCountedSceneGraphMessage>> processors;
            if (!processorCollections.TryGetValue(id, out processors))
            {
                processors = new List<Action<ReferenceCountedSceneGraphMessage>>();
                processorCollections.Add(id, processors);
            }

            processors.Add(processor);
        }

        public void RemoveProcessor(NetworkId id, Action<ReferenceCountedSceneGraphMessage> processor)
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

        public IEnumerable<KeyValuePair<Action<ReferenceCountedSceneGraphMessage>,NetworkId>> GetProcessors()
        {
            foreach (var pair in processorCollections)
            {
                foreach (var action in pair.Value)
                {
                    yield return new KeyValuePair<Action<ReferenceCountedSceneGraphMessage>, NetworkId>(action, pair.Key);
                }
            }
        }

        /// <summary>
        /// Registers a Networked Component with its closest Network Scene. The
        /// Network Id will be automatically assigned based on the Components
        /// location in the Scene Graph.
        /// </summary>
        public static NetworkContext Register(MonoBehaviour component)
        {
            return Register(component, GetNetworkId(component));
        }

        /// <summary>
        /// Registers a Networked Component with its closest Network Scene with
        /// the specified NetworkId.
        /// </summary>
        public static NetworkContext Register(MonoBehaviour component, NetworkId id)
        {
            NetworkContext context;
            context.Scene = Find(component);
            context.Id = id;
            context.Component = component;

            // Create a delegate for the method for the processor. This should
            // similar performance to a virtual method.
            //https://blogs.msmvps.com/jonskeet/2008/08/09/making-reflection-fly-and-exploring-delegates/

            try
            {
                var method = component.GetType().GetMethod("ProcessMessage");
                var processor = (Action<ReferenceCountedSceneGraphMessage>)Delegate.CreateDelegate(typeof(Action<ReferenceCountedSceneGraphMessage>), component, method);
                context.Scene.autoProcessors.Add(new AutoProcessor(context,processor));
                context.Scene.AddProcessor(context.Id, processor);
            }
            catch
            {
                Debug.LogError($"Could not find ProcessMessage on {component.GetType()} on {component.gameObject.name}. Make sure you have a public method ProcessMessage(ReferenceCountedSceneGraphMessage message)");
            }

            return context;
        }

        public static NetworkId GetNetworkId(MonoBehaviour component)
        {
            var property = (component.GetType().GetProperty("NetworkId", typeof(NetworkId)));
            if (property != null)
            {
                return (NetworkId)property.GetValue(component);
            }
            else
            {
                return NetworkId.Create(component);
            }
        }

        public static NetworkScene Find(MonoBehaviour component)
        {
            return Find(component.transform);
        }

        public static NetworkScene Find(Transform component)
        {
            // Check if the scene is simply a parent, or if we can find a root scene.
            var scene = component.GetComponentInParent<NetworkScene>();
            if (scene)
            {
                return scene;
            }
            if (rootNetworkScene != null)
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
            OnUpdate.Invoke();

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
            // Remove any destroyed autoprocessors
            for(int i = 0; i < autoProcessors.Count; i++)
            {
                if (!autoProcessors[i].context.Component)
                {
                    RemoveProcessor(autoProcessors[i].context.Id,autoProcessors[i].processor);
                    autoProcessors.RemoveAt(i);
                    i--;
                }
            }

            // Fan out all messages to all remaining processors
            ReferenceCountedMessage m;
            foreach (var c in connections)
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
                        foreach (var processor in processors)
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
            foreach (var c in connections)
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