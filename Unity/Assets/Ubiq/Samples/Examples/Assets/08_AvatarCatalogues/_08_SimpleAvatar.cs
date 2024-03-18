using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;

namespace Ubiq.Examples
{
    public class _08_SimpleAvatar : MonoBehaviour, INetworkSpawnable
    {
        private Ubiq.Avatars.Avatar avatar;
        private NetworkContext context;

        // This networkId will be assigned by the spawner
        public NetworkId NetworkId { get; set; }

        private Transform networkSceneRoot;

        private void Start()
        {
            avatar = GetComponent<Ubiq.Avatars.Avatar>();

            // Register using the NetworkID assigned to us by the spawner
            context = NetworkScene.Register(this, NetworkId);
            networkSceneRoot = context.Scene.transform;
        }

        private void Update()
        {
            if (avatar.IsLocal)
            {
                if (avatar.hints.TryGetVector3("Position",out var position))
                {
                    transform.position = position;

                    // Send position local to network scene. The co-ords will get
                    // converted to world space relative to the OTHER network scene
                    // on arriving at the remote user. This is not strictly
                    // required, but allows us to have two network scenes in one
                    // Unity scene for debugging and visualisation etc.
                    var localPosition = networkSceneRoot.InverseTransformPoint(position);
                    context.SendJson<Vector3>(localPosition);
                }
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            // Receive position local to network scene
            var localPosition = message.FromJson<Vector3>();

            // Convert to world space relative to THIS network scene
            var worldPosition = networkSceneRoot.TransformPoint(localPosition);
            transform.position = worldPosition;
        }
    }
}