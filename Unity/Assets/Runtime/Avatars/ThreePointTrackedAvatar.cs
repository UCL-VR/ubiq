using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Messaging;

namespace Ubiq.Avatars
{
    [RequireComponent(typeof(Avatar))]
    public class ThreePointTrackedAvatar : MonoBehaviour, INetworkComponent
    {
        [Serializable]
        public class TransformUpdateEvent : UnityEvent<Vector3,Quaternion> { }
        public TransformUpdateEvent OnHeadUpdate;
        public TransformUpdateEvent OnLeftHandUpdate;
        public TransformUpdateEvent OnRightHandUpdate;

        private NetworkContext context;
        private Transform networkSceneRoot;
        private State[] state = new State[1];
        private Avatar avatar;

        [Serializable]
        private struct State
        {
            public PositionRotation leftHand;
            public PositionRotation rightHand;
            public PositionRotation head;
        }

        private void Awake ()
        {
            avatar = GetComponent<Avatar>();
        }

        private void Start ()
        {
            context = NetworkScene.Register(this);
            networkSceneRoot = context.scene.transform;
        }

        private void Update ()
        {
            if(avatar.IsLocal)
            {
                // Update state from hints
                state[0].head = GetHintNode (AvatarHints.NodePosRot.Head);
                state[0].leftHand = GetHintNode(AvatarHints.NodePosRot.LeftHand);
                state[0].rightHand = GetHintNode(AvatarHints.NodePosRot.RightHand);

                // Send it through network
                Send();

                // Update local listeners
                OnRecv();
            }
        }

        // Local to world space
        private PositionRotation TransformPosRot (PositionRotation local, Transform root)
        {
            var world = new PositionRotation();
            world.position = root.TransformPoint(local.position);
            world.rotation = root.rotation * local.rotation;
            return world;
        }

        // World to local space
        private PositionRotation InverseTransformPosRot (PositionRotation world, Transform root)
        {
            var local = new PositionRotation();
            local.position = root.InverseTransformPoint(world.position);
            local.rotation = Quaternion.Inverse(root.rotation) * world.rotation;
            return local;
        }

        private PositionRotation GetHintNode (AvatarHints.NodePosRot node)
        {
            if (AvatarHints.TryGet(node,out PositionRotation nodePosRot))
            {
                return new PositionRotation
                {
                    position = nodePosRot.position,
                    rotation = nodePosRot.rotation
                };
            }

            return new PositionRotation();
        }

        private void Send()
        {
            // Co-ords from hints are already in local to our network scene
            // so we can send them without any changes
            var transformBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<State>(state));

            var message = ReferenceCountedSceneGraphMessage.Rent(transformBytes.Length);
            transformBytes.CopyTo(new Span<byte>(message.bytes, message.start, message.length));

            context.Send(message);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            MemoryMarshal.Cast<byte, State>(
                new ReadOnlySpan<byte>(message.bytes, message.start, message.length))
                .CopyTo(new Span<State>(state));

            OnRecv();
        }

        // State has been set either remotely or locally so update listeners
        private void OnRecv ()
        {
            // Transform with our network scene root to get world position/rotation
            var head = TransformPosRot(state[0].head,networkSceneRoot);
            var leftHand = TransformPosRot(state[0].leftHand,networkSceneRoot);
            var rightHand = TransformPosRot(state[0].rightHand,networkSceneRoot);

            OnHeadUpdate.Invoke(head.position,head.rotation);
            OnLeftHandUpdate.Invoke(leftHand.position,leftHand.rotation);
            OnRightHandUpdate.Invoke(rightHand.position,rightHand.rotation);
        }
    }
}
