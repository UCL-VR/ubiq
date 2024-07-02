using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using Ubiq.Messaging;
using Ubiq.Geometry;

namespace Ubiq.Avatars
{
    [RequireComponent(typeof(Avatar))]
    public class ThreePointTrackedAvatar : MonoBehaviour
    {
        [Tooltip("The Avatar to get input from. If null, will try to find an Avatar among parents at start.")]
        [SerializeField] private Avatar avatar;

        [Serializable]
        public class PoseUpdateEvent : UnityEvent<InputVar<Pose>> { }
        public PoseUpdateEvent OnHeadUpdate;
        public PoseUpdateEvent OnLeftHandUpdate;
        public PoseUpdateEvent OnRightHandUpdate;

        [Serializable]
        public class GripUpdateEvent : UnityEvent<InputVar<float>> { }
        public GripUpdateEvent OnLeftGripUpdate;
        public GripUpdateEvent OnRightGripUpdate;
        
        [Serializable]
        private struct State
        {
            public Pose head;
            public Pose leftHand;
            public Pose rightHand;
            public float leftGrip;
            public float rightGrip;
        }
        
        private State[] state = new State[1];
        private NetworkContext context;
        private Transform networkSceneRoot;
        private float lastTransmitTime;
        
        protected void Start()
        {
            if (!avatar)
            {
                avatar = GetComponentInParent<Avatar>();
                if (!avatar)
                {
                    Debug.LogWarning("No Avatar could be found among parents. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }
            
            context = NetworkScene.Register(this, NetworkId.Create(avatar.NetworkId, "ThreePointTracked"));
            networkSceneRoot = context.Scene.transform;
            lastTransmitTime = Time.time;
        }

        private void Update ()
        {
            if (!avatar.IsLocal)
            {
                return;
            }
            
            // Update state from input
            state[0] = avatar.Input.TryGet(out IHeadAndHandsProvider p)
                ? new State
                {
                    head = ToNetwork(p.head),
                    leftHand = ToNetwork(p.leftHand),
                    rightHand = ToNetwork(p.rightHand),
                    leftGrip = ToNetwork(p.leftGrip),
                    rightGrip = ToNetwork(p.rightGrip)
                }
                : new State
                { 
                    head = ToNetwork(InputVar<Pose>.invalid),
                    leftHand = ToNetwork(InputVar<Pose>.invalid),
                    rightHand = ToNetwork(InputVar<Pose>.invalid),
                    leftGrip = ToNetwork(InputVar<float>.invalid),
                    rightGrip = ToNetwork(InputVar<float>.invalid)
                };

            // Send it through network
            if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
            {
                lastTransmitTime = Time.time;
                Send();
            }

            // Update local listeners
            OnStateChange();
        }

        private Pose ToNetwork (InputVar<Pose> input)
        {
            return input.valid
                ? Transforms.ToLocal(input.value,networkSceneRoot)
                : GetInvalidPose();
        }

        private float ToNetwork (InputVar<float> input)
        {
            return input.valid ? input.value : GetInvalidFloat(); 
        }
        
        private InputVar<Pose> FromNetwork (Pose net)
        {
            return !IsInvalid(net)
                ? new InputVar<Pose>(Transforms.ToWorld(net,networkSceneRoot))
                : InputVar<Pose>.invalid;
        }
        
        private InputVar<float> FromNetwork (float net)
        {
            return !IsInvalid(net) 
                ? new InputVar<float>(net)
                : InputVar<float>.invalid;
        }
        
        private void Send()
        {
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
            OnStateChange();
        }
        
        // State has been set either remotely or locally so update listeners
        private void OnStateChange ()
        {
            OnHeadUpdate.Invoke(FromNetwork(state[0].head));
            OnLeftHandUpdate.Invoke(FromNetwork(state[0].leftHand));
            OnRightHandUpdate.Invoke(FromNetwork(state[0].rightHand));
            OnLeftGripUpdate.Invoke(FromNetwork(state[0].leftGrip));
            OnRightGripUpdate.Invoke(FromNetwork(state[0].rightGrip));
        }
        
        private static Pose GetInvalidPose()
        {
            return new Pose(new Vector3{x = float.NaN},Quaternion.identity);
        }
        
        private static float GetInvalidFloat()
        {
            return float.NaN;
        }
        
        private static bool IsInvalid(Pose p)
        {
            return float.IsNaN(p.position.x); 
        }
        
        private static bool IsInvalid(float f)
        {
            return float.IsNaN(f);
        }
    }
}
