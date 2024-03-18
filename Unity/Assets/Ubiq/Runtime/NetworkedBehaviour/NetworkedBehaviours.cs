using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Ubiq.Messaging;
using UnityEngine;

namespace Ubiq.NetworkedBehaviour
{
    public class NetworkTransform : Attribute
    {
    }

    public static class NetworkedBehaviours
    {
        private class NetworkedBehaviour
        {
            public MonoBehaviour component;
            public NetworkScene scene;
            public NetworkId id;
            public float period = 0.1f;
            private float lastTransmitTime;
            private string state;

            public void Send()
            {
                if ((Time.time - lastTransmitTime) > period)
                {
                    lastTransmitTime = Time.time;

                    var json = JsonUtility.ToJson(component);
                    if(json != state)
                    {
                        state = json;
                        scene.Send(id, state);
                    }                    
                }
            }

            public void Receive(ReferenceCountedSceneGraphMessage message)
            {
                var json = message.ToString();
                state = json;
                JsonUtility.FromJsonOverwrite(state, component);
            }
        }

        private class NetworkedTransform
        {
            public Transform transform;
            public NetworkScene scene;
            public NetworkId id;
            public float period = 0.05f;
            private float lastTransmitTime;
            private Message state;

            private struct Message
            {
                public Vector3 localPosition;
                public Quaternion localRotation;
                public Vector3 localScale;

                public bool Equals(Message other)
                {
                    if(localPosition != other.localPosition)
                    {
                        return false;
                    }
                    if(localRotation != other.localRotation)
                    {
                        return false;
                    }
                    if(localScale != other.localScale)
                    {
                        return false;
                    }
                    return true;
                }
            }

            public void Send()
            {
                if ((Time.time - lastTransmitTime) > period)
                {
                    lastTransmitTime = Time.time;
                    Message m;
                    m.localPosition = transform.localPosition;
                    m.localRotation = transform.localRotation;
                    m.localScale = transform.localScale;

                    if(!state.Equals(m))
                    {
                        state = m;
                        scene.SendJson(id, m);
                    }
                }
            }

            public void Receive(ReferenceCountedSceneGraphMessage message)
            {
                var m = message.FromJson<Message>();
                state = m;
                transform.localPosition = state.localPosition;
                transform.localRotation = state.localRotation;
                transform.localScale = state.localScale;
            }
        }


        private static Dictionary<MonoBehaviour, NetworkedBehaviour> behaviours = new Dictionary<MonoBehaviour, NetworkedBehaviour>();
        private static Dictionary<Transform, NetworkedBehaviour> transforms = new Dictionary<Transform, NetworkedBehaviour>();

        public static void Register(MonoBehaviour component)
        {
            if (!behaviours.ContainsKey(component))
            {
                var scene = NetworkScene.Find(component);

                var behaviour = new NetworkedBehaviour();
                behaviour.component = component;
                behaviour.scene = scene;
                behaviour.id = NetworkScene.GetNetworkId(component);

                // Register the behaviour on behalf of itself
                scene.OnUpdate.AddListener(behaviour.Send);
                scene.AddProcessor(behaviour.id, behaviour.Receive);

                behaviours.Add(component, behaviour);

                // If this is the first time we've encountered the transform, set
                // up a behaviour to synchronise it too.

                if (component.GetType().GetCustomAttribute(typeof(NetworkTransform)) != null)
                {
                    if (!transforms.ContainsKey(component.transform))
                    {
                        var transform = new NetworkedTransform();
                        transform.transform = component.transform;
                        transform.scene = scene;
                        transform.id = NetworkId.Create(behaviour.id, "Transform");

                        scene.OnUpdate.AddListener(transform.Send);
                        scene.AddProcessor(transform.id, transform.Receive);

                        transforms.Add(component.transform, behaviour);
                    }
                }
            }
        }

        public static IEnumerable<MonoBehaviour> GetNetworkedBehaviours()
        {
            return behaviours.Keys;
        }

        public static bool IsNetworkedBehaviour(MonoBehaviour behaviour)
        {
            return behaviours.ContainsKey(behaviour);
        }
    }
}