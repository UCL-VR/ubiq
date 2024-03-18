using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;

namespace Ubiq.Examples
{
    public class _09_AvatarWithSampleHints : MonoBehaviour, INetworkSpawnable
    {
        private Ubiq.Avatars.Avatar avatar;
        private NetworkContext context;

        // This networkId will be assigned by the spawner
        public NetworkId NetworkId { get; set; }

        private Transform networkSceneRoot;

        [System.Serializable]
        private struct Message
        {
            public Vector3 position;
            public Quaternion rotation;
        }

        private Message message;

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
                if (avatar.hints.TryGetVector3("Position", out var position))
                {
                    message.position = position;
                }
                if (avatar.hints.TryGetQuaternion("Rotation", out var rotation))
                {
                    message.rotation = rotation;
                }

                transform.position = message.position;
                transform.rotation = message.rotation;

                // Send position local to network scene. The co-ords will get
                // converted to world space relative to the OTHER network scene
                // on arriving at the remote user. This is not strictly
                // required, but allows us to have two network scenes in one
                // Unity scene for debugging and visualisation etc.
                var localSpaceMessage = new Message();
                localSpaceMessage.position = networkSceneRoot.InverseTransformPoint(message.position);
                localSpaceMessage.rotation = Quaternion.Inverse(networkSceneRoot.rotation) * message.rotation;
                context.SendJson<Message>(localSpaceMessage);
            }
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            // Receive hints local to network scene
            var localSpaceMessage = message.FromJson<Message>();

            // Convert to world space relative to THIS network scene
            transform.position = networkSceneRoot.TransformPoint(localSpaceMessage.position);
            transform.rotation = networkSceneRoot.rotation * localSpaceMessage.rotation;
        }
    }
}