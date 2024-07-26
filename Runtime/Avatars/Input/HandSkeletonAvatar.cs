using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.Events;
using Avatar = Ubiq.Avatars.Avatar;
using Joint = Ubiq.HandSkeleton.Joint;
using Handedness = Ubiq.HandSkeleton.Handedness;

namespace Ubiq
{
    /// <summary>
    /// Makes <see cref="IHandSkeletonInput"/> information available to an
    /// avatar. Input will be sourced from <see cref="Avatar.input"/> if this is
    /// a local avatar or over the network if this is remote.
    /// </summary>
    public class HandSkeletonAvatar : MonoBehaviour
    {
        [Tooltip("The Avatar to use as the source of input. If null, will try to find an Avatar among parents at start.")]
        [SerializeField] private Avatar avatar;
        
        [Serializable]
        public class HandSkeletonUpdateEvent : UnityEvent<HandSkeleton> { }
        
        /// <summary>
        /// Invoked when new hand skeleton input information has been received.
        /// </summary>
        public HandSkeletonUpdateEvent OnHandUpdate;
        
        /// <summary>
        /// Invoked when new hand skeleton input information has been received.
        /// This version is a convenience event that is only called for the left
        /// hand.
        /// </summary>
        public HandSkeletonUpdateEvent OnLeftHandUpdate;
        
        /// <summary>
        /// Invoked when new hand skeleton input information has been received.
        /// This version is a convenience event that is only called for the
        /// right hand.
        /// </summary>
        public HandSkeletonUpdateEvent OnRightHandUpdate;
        
        private class WritableSkeleton
        {
            public HandSkeleton hand;
            public List<InputVar<Pose>> joints;
            
            public WritableSkeleton(Handedness handedness)
            {
                joints = new List<InputVar<Pose>>();
                for (var i = Joint.BeginMarker.Idx(); i < Joint.EndMarker.Idx(); i++)
                {
                    joints.Add(InputVar<Pose>.invalid);
                }
                hand = new HandSkeleton(handedness,joints.AsReadOnly());
            }
        }
        
        private Pose[] netSkeleton;
        private WritableSkeleton leftSkeleton;
        private WritableSkeleton rightSkeleton;
        private NetworkContext context; 
        private Transform networkSceneRoot;
        private float lastTransmitTime;
        
        private void Start()
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
            
            leftSkeleton = new WritableSkeleton(Handedness.Left);
            rightSkeleton = new WritableSkeleton(Handedness.Right);
            netSkeleton = new Pose[(int)Joint.Count*2];
            
            context = NetworkScene.Register(this, NetworkId.Create(avatar.NetworkId, nameof(HandSkeletonAvatar)));
            networkSceneRoot = context.Scene.transform;
            lastTransmitTime = Time.time;
        }
        
        private void OnEnable()
        {
            Application.onBeforeRender += Application_OnBeforeRender;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= Application_OnBeforeRender;
        }

        private void OnDestroy()
        {
            InvalidateJoints(leftSkeleton?.joints);
            InvalidateJoints(rightSkeleton?.joints);
        }

        private void Update()
        {
            if (!avatar.IsLocal)
            {
                return;
            }
            
            // Send it through network
            if ((Time.time - lastTransmitTime) > (1f / avatar.UpdateRate))
            {
                UpdateState();
                
                // Transmit
                lastTransmitTime = Time.time;
                Send();
            }
        }
        
        [BeforeRenderOrder(50)] // Update after the XRHand Subsystem (0?)
        private void Application_OnBeforeRender()
        {
            if (!avatar.IsLocal)
            {
                Application.onBeforeRender -= Application_OnBeforeRender;
                return;
            }
            
            UpdateState();
            
            // Update local listeners
            OnStateChange();
        }
        
        private void UpdateState()
        {
            // Update input to latest
            if (avatar.input.TryGet(out IHandSkeletonInput src))
            {
                ToNetwork(src.leftHandSkeleton,src.rightHandSkeleton,netSkeleton);
            }
            else
            {
                ToNetworkInvalid(netSkeleton);
            }
        }
        
        private void ToNetwork(HandSkeleton left, HandSkeleton right, Pose[] net)
        {
            var i = 0;
            var c = (int)Joint.Count;
            for (var joint = Joint.BeginMarker; joint < Joint.EndMarker; joint++)
            {
                net[i] = left.TryGetPose(joint,out var leftPose)
                    ? leftPose
                    : GetInvalidPose();
                net[i+c] = right.TryGetPose(joint,out var rightPose)
                    ? rightPose
                    : GetInvalidPose();
                i++;
            }
        }
        
        private void ToNetworkInvalid(Pose[] net)
        {
            for (var i = 0; i < net.Length; i++)
            {
                net[i++] = GetInvalidPose();
            }
        }
        
        private void FromNetwork (Pose[] net, WritableSkeleton left, WritableSkeleton right)
        {
            var i = 0;
            var c = (int)Joint.Count;
            for (var joint = Joint.BeginMarker; joint < Joint.EndMarker; joint++)
            {
                left.joints[joint.Idx()] = !IsInvalid(net[i]) 
                    ? new InputVar<Pose>(net[i])
                    : InputVar<Pose>.invalid;
                right.joints[joint.Idx()] = !IsInvalid(net[i+c])
                    ? new InputVar<Pose>(net[i+c])
                    : InputVar<Pose>.invalid;
                i++;
            }
        }
        
        private void Send()
        {
            var transformBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<Pose>(netSkeleton));

            var message = ReferenceCountedSceneGraphMessage.Rent(transformBytes.Length);
            transformBytes.CopyTo(new Span<byte>(message.bytes, message.start, message.length));

            context.Send(message);
        }
        
        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            MemoryMarshal.Cast<byte, Pose>(
                    new ReadOnlySpan<byte>(message.bytes, message.start, message.length))
                .CopyTo(new Span<Pose>(netSkeleton));
            OnStateChange();
        }
        
        // State has been set either remotely or locally so update listeners
        private void OnStateChange ()
        {
            FromNetwork(netSkeleton,leftSkeleton,rightSkeleton);
            OnHandUpdate.Invoke(leftSkeleton.hand);
            OnLeftHandUpdate.Invoke(leftSkeleton.hand);
            OnHandUpdate.Invoke(rightSkeleton.hand);
            OnRightHandUpdate.Invoke(rightSkeleton.hand);
        }
        
        private static Pose GetInvalidPose()
        {
            return new Pose(new Vector3{x = float.NaN},Quaternion.identity);
        }
        
        private static bool IsInvalid(Pose p)
        {
            return float.IsNaN(p.position.x); 
        }
        
        private static void InvalidateJoints(List<InputVar<Pose>> joints)
        {
            if (joints == null)
            {
                return;
            }
            
            for (var i = 0; i < joints.Count; i++)
            {
                joints[i] = InputVar<Pose>.invalid;
            }
        }
    }
}
