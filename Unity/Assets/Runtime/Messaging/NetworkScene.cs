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
    public class NetworkContext
    {
        public NetworkScene scene;
        public INetworkObject networkObject;
        public ushort componentId;
        public INetworkComponent component;

        /// <summary>
        /// Sends the message to the network. If the objectid is uninitialised, the message will be sent
        /// to the context address (i.e. the objects with the same object and component ids).
        /// </summary>
        /// <param name="message"></param>
        public void Send(ReferenceCountedSceneGraphMessage message)
        {
            if (!message.objectid)
            {
                message.objectid = networkObject.Id;
                message.componentid = componentId;
            }
            scene.Send(message);
        }

        public void Send(NetworkId objectid, ushort componentid, ReferenceCountedSceneGraphMessage message)
        {
            message.objectid = objectid;
            message.componentid = componentid;

            scene.Send(message);

        }

        public void Send(NetworkId objectid, ushort componentid, string message)
        {
            var msg = ReferenceCountedSceneGraphMessage.Rent(message);
            msg.componentid = componentid;
            msg.objectid = objectid;
            scene.Send(msg);
        }

        public void Send(string message)
        {
            Send(networkObject.Id, componentId, message);
        }

        public void SendJson<T>(NetworkId objectid, T message)
        {
            Send(objectid, componentId, JsonUtility.ToJson(message));
        }

        public void SendJson<T>(T message)
        {
            SendJson(networkObject.Id, message);
        }
    }
    /// <summary>
    /// Classes implementing this interface are meant to collect to be sent and received network messages (like the RecorderReplayer class does)
    /// </summary>
    public interface IMessageRecorder
    {
        /// <summary>
        /// Collects and processes a message which is either either sent or recieved by networkObject.
        /// </summary>
        /// <param name="networkObject"></param>
        /// <param name="message"></param>
        void RecordMessage(INetworkObject networkObject, ReferenceCountedSceneGraphMessage message);

        /// <summary>
        /// Increments the frame number of the recorder.
        /// Should be called after all messages in one frame are processed (e.g. when Update() is finished)
        /// </summary>
        void NextFrame();
        /// <summary>
        /// Returns if the MessageRecorder is currently recording.
        /// </summary>
        /// <returns></returns>
        bool IsRecording();

        bool IsOwner();

        void StopRecording();
    }

    /// <summary>
    /// The Network Scene connects networked Components to Network Connections.
    /// </summary>
    public class NetworkScene : MonoBehaviour, IDisposable, INetworkObject
    {
        private List<INetworkConnection> connections = new List<INetworkConnection>();
        private List<Action> actions = new List<Action>();
        private HashSet<string> existingIdAssignments = new HashSet<string>();
        private List<ObjectProperties> matching = new List<ObjectProperties>();

        private EventLogger events;

        public IMessageRecorder recorder = null;



        public T GetNetworkComponent<T>() where T : class
        {
            foreach (var networkObject in objectProperties.Select(p => p.Value))
            {
                foreach (var component in networkObject.components)
                {
                    if(component is T)
                    {
                        return component as T;
                    }
                }
            }
            return null;
        }

        private class ObjectProperties
        {
            public NetworkScene scene;
            public INetworkObject identity;
            public Dictionary<int, INetworkComponent> components = new Dictionary<int, INetworkComponent>();
        }

        private Dictionary<INetworkObject, ObjectProperties> objectProperties = new Dictionary<INetworkObject, ObjectProperties>();

        public NetworkId Id { get; } = NetworkScene.GenerateUniqueId();

        private static NetworkScene rootNetworkScene;

        private void Awake()
        {
            Application.targetFrameRate = 30;

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

            events = new ComponentEventLogger(this);
            events.Log("Awake", Id, SystemInfo.deviceName, SystemInfo.deviceModel, SystemInfo.deviceUniqueIdentifier);

            // Try getting a RecorderReplayer if script is attached to the NetworkScene
            if(TryGetComponent(out IMessageRecorder recRep))
            {
                Debug.Log("Set Recorder in NetworkScene");
                recorder = recRep;
            }
            else
            {
                Debug.Log("No Recorder set in NetworkScene");
            }
        }

        /// <summary>
        /// Add a networked component to the network client. Once registered the object can be moved around.
        /// </summary>
        public static NetworkContext Register(INetworkComponent component)
        {
            return FindNetworkScene(component as MonoBehaviour).RegisterComponent(component);
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

        public static ushort GetComponentId<T>()
        {
            return typeof(T).GetComponentId();
        }

        public static ushort GetComponentId(INetworkComponent component)
        {
            return component.GetType().GetComponentId();
        }

        public NetworkContext RegisterComponent(INetworkComponent component)
        {
            INetworkObject networkObject = null;

            if(component is INetworkObject)
            {
                networkObject = component as INetworkObject;
            }
            else
            {
                foreach (var item in (component as MonoBehaviour).GetComponentsInParent<MonoBehaviour>()) // search up
                {
                    if(item is INetworkObject)
                    {
                        networkObject = item as INetworkObject;
                        break;
                    }
                }
            }

            if (!objectProperties.ContainsKey(networkObject))
            {
                objectProperties.Add(networkObject, new ObjectProperties()
                {
                    identity = networkObject,
                    scene = this,
                });
            }

            objectProperties[networkObject].components[GetComponentId(component)] = component;

            NetworkContext context = new NetworkContext();
            context.scene = this;
            context.networkObject = networkObject;
            context.componentId = GetComponentId(component);
            context.component = component;

            return context;
        }

        /// <summary>
        /// Sets the Network Object Id so that it will be consistent for this particular GameObject instance across
        /// different processes.
        /// This is currently based on the Name. There cannot be two objects with the same directory registered.
        /// </summary>
        /// <param name="monoBehaviour"></param>
        public static NetworkId ObjectIdFromName(INetworkObject monoBehaviour)
        {
            return (monoBehaviour as MonoBehaviour).GetComponentInParent<NetworkScene>().GetObjectIdFromName(monoBehaviour);
        }

        public static NetworkId GenerateUniqueId()
        {
            return IdGenerator.GenerateUnique();
        }

        public NetworkId GetObjectIdFromName(INetworkObject monoBehaviour)
        {
            var target = monoBehaviour as MonoBehaviour;
            var key = target.name;
            if(existingIdAssignments.Contains(key))
            {
                throw new Exception("Cannot add multiple objects of the same name.");
            }
            existingIdAssignments.Add(key);
            return IdGenerator.GenerateFromName(key);
        }

        public void AddConnection(INetworkConnection connection)
        {
            connections.Add(connection);
        }

        private void Update()
        {
            foreach (var item in objectProperties)
            {
                // Checks if the Unity object has been destroyed. When Unity objects are destroyed, the managed reference can remain around, but the object is invalid. Unity overrides the truth check to indicate this.

                if (item.Key is UnityEngine.Object)
                {
                    if (!(item.Key as UnityEngine.Object))
                    {
                        actions.Add(() =>
                        {
                            try
                            {
                                objectProperties.Remove(item.Key);
                            }
                            catch (KeyNotFoundException)
                            {
                            }
                        });
                        continue;
                    }
                }
            }

            foreach (var action in actions)
            {
                action();
            }
            actions.Clear();

            // increment frame number before 
            // it is possible that already at frame 0 a local Send() is recorded before the frame is incremented, is it?
            if (recorder != null && recorder.IsOwner() && recorder.IsRecording())
            {
                recorder.NextFrame(); // increments frame number when recording!
            }

            ReceiveConnectionMessages();
            
        }
        // want to exlude messages with these object ids for recording
        private NetworkId serverId = new NetworkId(1);
        private NetworkId spawnerId = new NetworkId("a369-2643-7725-a971");

        /// <summary>
        /// This checks all connections for messages and fans them out into the individual recieve queues
        /// </summary>
        public void ReceiveConnectionMessages()
        {
            ReferenceCountedMessage m;
            foreach(var c in connections)
            {
                do
                {
                    m = c.Receive();

                    if(m != null)
                    {
                        try
                        {
                            var sgbmessage = new ReferenceCountedSceneGraphMessage(m);

                            matching.Clear();
                            foreach (var item in objectProperties)
                            {
                                if (item.Key.Id == sgbmessage.objectid)
                                {
                                    matching.Add(item.Value);

                                    if (recorder != null && recorder.IsOwner() && recorder.IsRecording() /*&& item.Key is Ubiq.Avatars.Avatar*/)
                                    {
                                        // record just avatars and objects and exclude messages for the room client and the network spawner
                                        // and the network scene (bare in mind that LogManager and LogCollector are not exluded (yet))
                                        if (!item.Key.Id.Equals(serverId) && !item.Key.Id.Equals(spawnerId) && !item.Key.Id.Equals(Id))
                                        {
                                            //Debug.Log("Rcv");
                                            recorder.RecordMessage(item.Key, sgbmessage);
                                        }
                                    }
                                }
                            }

                            foreach (var item in matching)
                            {
                                INetworkComponent component = null;

                                try
                                {
                                    component = item.components[sgbmessage.componentid]; // select designated componet for sgbmessage
                                }
                                catch (KeyNotFoundException)
                                {
                                    continue;
                                }

                                try
                                {
                                   // Profiler.BeginSample("Component Message Processing " + component.ToString());
                                    component.ProcessMessage(sgbmessage);
                                }
                                catch (MissingReferenceException e)
                                {
                                    if (component is UnityEngine.Object)
                                    {
                                        if (!(component as UnityEngine.Object))
                                        {
                                            item.components.Remove(sgbmessage.componentid);
                                            return;
                                        }
                                    }

                                    throw e;
                                }
                                // finally
                                // {
                                //     Profiler.EndSample();
                                // }
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e); // because otherwise this will not be visible to the main thread
                        }
                        finally
                        {
                            m.Release();
                        }
                    }
                    else
                    {
                        break;
                    }
                } while (true);
            }
        }

        public void Send(ReferenceCountedSceneGraphMessage m)
        {
            Profiler.BeginSample("Send");

            foreach (var c in connections)
            {
                m.Acquire();

                if (recorder != null && recorder.IsOwner() && recorder.IsRecording())
                {
                    foreach (var item in objectProperties)
                    {
                        // can be removed later if other objects are recorded too
                        if (/*item.Key is Ubiq.Avatars.Avatar &&*/ item.Key.Id == m.objectid) // with second equality I make sure to exclude "Ping" mesages?
                        {
                            if (!item.Key.Id.Equals(serverId) && !item.Key.Id.Equals(spawnerId) && !item.Key.Id.Equals(Id))
                            {
                                //Debug.Log("Send");
                                recorder.RecordMessage(item.Key, m);
                            }
                        }
                    }
                }
                c.Send(m.buffer);
            }
            m.Release(); // m should have been acquired once on creation.

            Profiler.EndSample();
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
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

    public class NetworkComponentId : Attribute
    {
        public NetworkComponentId(Type type, ushort id)
        {
            this.type = type;
            this.id = id;
        }

        // The type member serves only as a safety check, in the case that new users copy an existing class with an override and do not realise they need to change the id to avoid conflicts.
        private Type type;
        private ushort id;

        public ushort GetComponentId(Type other)
        {
            if(other != type)
            {
                throw new Exception("NetworkComponentId Attribute is attached to an object with a different type than was declared. Did you copy the attribute and forget to change the class and Id?");
            }
            return id;
        }
    }

    public static class ComponentIdExtensions
    {
        public static ushort GetComponentId(this Type type)
        {
            if(cache.ContainsKey(type))
            {
                return cache[type];
            }

            var idattributes = type.GetCustomAttributes(typeof(NetworkComponentId), true);
            if (idattributes.Length > 0)
            {
                var id = (idattributes[0] as NetworkComponentId).GetComponentId(type);
                cache.Add(type, id);
                return id;
            }
            else
            {
                var id = type.FullName.GetPortableHashCode();
                cache.Add(type, id);
                return id;
            }
        }

        private static Dictionary<Type, ushort> cache = new Dictionary<Type, ushort>();
    }
}