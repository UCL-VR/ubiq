using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Messaging;
using Ubiq.Spawning;

namespace Ubiq.Avatars
{
    [RequireComponent(typeof(Avatar))]
    public class ThreePointTrackedAvatar : MonoBehaviour
    {
        private struct PositionRotation
        {
            public Vector3 position;
            public Quaternion rotation;

            public static PositionRotation identity
            {
                get
                {
                    return new PositionRotation
                    {
                        position = Vector3.zero,
                        rotation = Quaternion.identity
                    };
                }
            }
        }

        [Serializable]
        public class TransformUpdateEvent : UnityEvent<Vector3,Quaternion> { }
        public TransformUpdateEvent OnHeadUpdate;
        public TransformUpdateEvent OnLeftHandUpdate;
        public TransformUpdateEvent OnRightHandUpdate;

        [Serializable]
        public class GripUpdateEvent : UnityEvent<float> { }
        public GripUpdateEvent OnLeftGripUpdate;
        public GripUpdateEvent OnRightGripUpdate;

        private NetworkContext context;
        private Transform networkSceneRoot;
        private State[] state = new State[1];
        private Avatar avatar;
        private float lastTransmitTime;

        [Serializable]
        private struct State
        {
            public PositionRotation head;
            public PositionRotation leftHand;
            public PositionRotation rightHand;
            public float leftGrip;
            public float rightGrip;
        }

        protected void Start()
        {
            avatar = GetComponent<Avatar>();
            context = NetworkScene.Register(this, NetworkId.Create(avatar.NetworkId, "ThreePointTracked"));
            networkSceneRoot = context.Scene.transform;
            lastTransmitTime = Time.time;
        }

        private void Update ()
        {
            if(avatar.IsLocal)
            {
                // Update state from hints
                state[0].head = GetPosRotHint("HeadPosition","HeadRotation");
                state[0].leftHand = GetPosRotHint("LeftHandPosition","LeftHandRotation");
                state[0].rightHand = GetPosRotHint("RightHandPosition","RightHandRotation");
                state[0].leftGrip = GetFloatHint("LeftGrip");
                state[0].rightGrip = GetFloatHint("RightGrip");

                // Send it through network
                if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
                {
                    lastTransmitTime = Time.time;
                    Send();
                }

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

        private PositionRotation GetPosRotHint (string position, string rotation)
        {
            if (avatar == null || avatar.hints == null)
            {
                return PositionRotation.identity;
            }

            var posrot = PositionRotation.identity;
            if (avatar.hints.TryGetVector3(position, out var pos))
            {
                posrot.position = pos;
            }
            if (avatar.hints.TryGetQuaternion(rotation, out var rot))
            {
                posrot.rotation = rot;
            }
            return InverseTransformPosRot(posrot,networkSceneRoot);
        }

        private float GetFloatHint (string node)
        {
            if (avatar == null || avatar.hints == null)
            {
                return 0.0f;
            }

            if (avatar.hints.TryGetFloat(node, out var f))
            {
                return f;
            }
            return 0.0f;
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
            var leftGrip = state[0].leftGrip;
            var rightGrip = state[0].rightGrip;

            OnHeadUpdate.Invoke(head.position,head.rotation);
            OnLeftHandUpdate.Invoke(leftHand.position,leftHand.rotation);
            OnRightHandUpdate.Invoke(rightHand.position,rightHand.rotation);
            OnLeftGripUpdate.Invoke(leftGrip);
            OnRightGripUpdate.Invoke(rightGrip);
        }
    }
}
