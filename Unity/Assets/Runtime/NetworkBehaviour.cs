using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;

namespace Ubiq
{
    /// <summary>
    /// Convenience class for implementing spawnable, messaging-enabled scripts.
    /// Internally manages NetworkId and serializes with script. Duplicates will
    /// receive new NetworkIds.
    ///
    /// Both spawning and messaging can work without this class. Spawning will work
    /// with any MonoBehaviour derived class implementing the INetworkSpawnable
    /// interface. Messaging only needs a NetworkId to be passed to a NetworkScene.
    /// </summary>
    public class NetworkBehaviour : MonoBehaviour, INetworkSpawnable
    {
        [HideInInspector]
        public NetworkId networkId = NetworkId.Unique();
        protected NetworkScene networkScene { get; private set; }

        [SerializeField,HideInInspector]
        private string __serializedGoid;

    #if UNITY_EDITOR
        private string __goid = null;
    #endif

        NetworkId INetworkSpawnable.networkId { set => this.networkId = value; }

        protected void Start ()
        {
            networkScene = NetworkScene.FindNetworkScene(this);
            if (networkScene)
            {
                networkScene.AddProcessor(networkId,ProcessMessage);
            }
            else
            {
                Debug.LogWarning("No NetworkScene could be found - this Behaviour will not receive messages");
            }
            Started();
        }

        protected void OnDestroy ()
        {
            if (networkScene)
            {
                networkScene.RemoveProcessor(networkId,ProcessMessage);
            }
            OnDestroyed();
        }

        protected void OnValidate ()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                // Provide a new network id if we are created or duplicated
                // Don't generate a new network id if we are loaded or modified
                // Note: quite fiddly - OnValidate can be called before deserialization
                if (string.IsNullOrEmpty(__goid))
                {
                    __goid = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(this).ToString();
                }

                if (!string.Equals(__goid,__serializedGoid))
                {
                    __serializedGoid = __goid;
                    networkId = NetworkId.Unique();
                    UnityEditor.EditorUtility.SetDirty(this);
                }
            }
#endif

            OnValidated();
        }

        protected void Send(string message)
        {
            if (networkScene)
            {
                networkScene.Send(networkId,message);
            }
        }

        protected void Send(ReferenceCountedSceneGraphMessage message)
        {
            if (networkScene)
            {
                networkScene.Send(networkId,message);
            }
        }

        protected void SendJson<T>(T message)
        {
            if (networkScene)
            {
                networkScene.SendJson(networkId,message);
            }
        }

        protected virtual void ProcessMessage(ReferenceCountedSceneGraphMessage message) {}
        protected virtual void Started() {}
        protected virtual void OnValidated() {}
        protected virtual void OnDestroyed() {}

    }
}