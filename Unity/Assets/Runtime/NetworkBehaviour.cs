using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.Spawning;
using UnityEngine;

namespace Ubiq
{
    public class NetworkBehaviour : MonoBehaviour, INetworkSpawnable
    {
        public NetworkId NetworkId { get; set; }
        public NetworkContext context;

        // Start is called before the first frame update
        public virtual void Start()
        {
            if(!NetworkId.Valid) // If we haven't been spawned.. (if we were, the spawner would have set this).
            {
                NetworkId = NetworkId.Create(this);
            }
            context = NetworkScene.Register(this);
        }

        public virtual void Send(ReferenceCountedSceneGraphMessage message)
        {
            context.Send(message);
        }

        public virtual void SendJson<T>(T message)
        {
            context.SendJson<T>(message);
        }

        public virtual void Send(string message)
        {
            context.Send(message);
        }
    }
}