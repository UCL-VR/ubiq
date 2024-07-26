using System;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;
using Ubiq.Geometry;
using Handedness = Ubiq.HandSkeleton.Handedness; 
using Joint = Ubiq.HandSkeleton.Joint;

#if XRCOREUTILS_2_2_0_OR_NEWER
using Unity.XR.CoreUtils;
#endif

#if XRHANDS_0_0_0_OR_NEWER
using UnityEngine.XR.Hands;
using UpdateSuccessFlags = UnityEngine.XR.Hands.XRHandSubsystem.UpdateSuccessFlags;
using UpdateType = UnityEngine.XR.Hands.XRHandSubsystem.UpdateType;
#endif

namespace Ubiq.XRHands
{
    public class HandSkeletonAvatarInputXRHands : MonoBehaviour
    {
        [Tooltip("The AvatarManager to provide input to. If null, will try to find an AvatarManager in the scene at start.")]
        [SerializeField] private AvatarManager avatarManager;
        [Tooltip("The GameObject containing the XROrigin. If null, will try to find an XROrigin in the scene at Start. Note we use a GameObject reference here rather than a direct reference to avoid serialization issues should XR CoreUtils not be present.")]
        [SerializeField] private GameObject xrOriginGameObject;
        [Tooltip("Higher priority inputs will override lower priority inputs of the same type if multiple exist.")]
        public int priority = 0;
        
#if XRHANDS_0_0_0_OR_NEWER && XRCOREUTILS_2_2_0_OR_NEWER
        
        private class HandSkeletonInput : IHandSkeletonInput
        {
            public int priority => owner.priority;
            public bool active => owner.isActiveAndEnabled;
            
            public HandSkeleton leftHandSkeleton => owner.Left();
            public HandSkeleton rightHandSkeleton => owner.Right();
            
            private HandSkeletonAvatarInputXRHands owner;
            
            public HandSkeletonInput(HandSkeletonAvatarInputXRHands owner)
            {
                this.owner = owner;
            }
        }
        
        private class WritableSkeleton
        {
            public HandSkeleton hand;
            public List<InputVar<Pose>> joints;
            public bool isDirty;
        }

        private WritableSkeleton leftSkeleton;
        private WritableSkeleton rightSkeleton;
        
        private static readonly List<XRHandSubsystem> k_SubsystemsReuse = new ();
        private XRHandSubsystem subsystem;
        private XROrigin origin;
        
        private HandSkeletonInput input;
        
        private void Start()
        {
            if (!avatarManager)
            {
                avatarManager = FindAnyObjectByType<AvatarManager>();

                if (!avatarManager)
                {
                    Debug.LogWarning("No AvatarManager could be found in this Unity scene. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }
            
            if (xrOriginGameObject)
            {
                origin = xrOriginGameObject.GetComponent<XROrigin>();
                
                if (!origin)
                {
                    Debug.LogWarning("XROriginGameObject supplied but no " +
                                     "XROrigin component could be found. Will" +
                                     " attempt to find an XROrigin in scene.");
                }
            }
            
            if (!origin)
            {
                origin = FindObjectOfType<XROrigin>();
                
                if (!origin)
                {
                    Debug.LogWarning("No XROrigin found. The local avatar" +
                                     " will not be have its input driven by " +
                                     "XRI");
                    return;
                }
            }
            
            input = new HandSkeletonInput(this);
            avatarManager.input.Add((IHandSkeletonInput)input);
        }
        
        private void Update()
        {
            if (subsystem != null && subsystem.running)
            {
                return;
            }
        
            SubsystemManager.GetSubsystems(k_SubsystemsReuse);
            for (var i = 0; i < k_SubsystemsReuse.Count; ++i)
            {
                var handSubsystem = k_SubsystemsReuse[i];
                if (handSubsystem.running)
                {
                    SetSubsystem(handSubsystem);
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            if (avatarManager)
            {
                avatarManager.input?.Remove((IHandSkeletonInput)input);
            }
            
            ClearSubsystem();
            Invalidate(leftSkeleton?.joints);
            Invalidate(rightSkeleton?.joints);
        }
        
        private HandSkeleton Left()
        {
            Require(ref leftSkeleton, Handedness.Left);
            PollLeft();
            return leftSkeleton.hand;
        }
        
        private HandSkeleton Right()
        {
            Require(ref rightSkeleton, Handedness.Right);
            PollRight();
            return rightSkeleton.hand;
        }
        
        private void Require(ref WritableSkeleton skeleton, Handedness handedness)
        {
            if (skeleton != null)
            {
                return;
            }
            
            var joints = new List<InputVar<Pose>>();
            for (var i = Joint.BeginMarker.Idx(); i < Joint.EndMarker.Idx(); i++)
            {
                joints.Add(InputVar<Pose>.invalid);
            }
            skeleton = new WritableSkeleton
            {
                joints = joints,
                hand = new HandSkeleton(handedness,joints.AsReadOnly()),
                isDirty = true
            };
        }
        
        private void SetSubsystem(XRHandSubsystem subsystem)
        {
            if (subsystem != null)
            {
                subsystem.updatedHands -= Subsystem_Updated;
                subsystem.trackingAcquired -= Subsystem_Changed;
                subsystem.trackingLost -= Subsystem_Changed;
            }
            
            this.subsystem = subsystem;
            
            SetDirty(leftSkeleton, true);
            SetDirty(rightSkeleton, true);
            
            if (subsystem != null)
            {
                subsystem.updatedHands += Subsystem_Updated;
                subsystem.trackingAcquired += Subsystem_Changed;
                subsystem.trackingLost += Subsystem_Changed;
            }
        }
        
        private void ClearSubsystem()
        {
            SetSubsystem(null);
        }
        
        private void Subsystem_Updated(XRHandSubsystem handSubsystem, 
            UpdateSuccessFlags flags, UpdateType updateType)
        {
            SetDirty(leftSkeleton,true);
            SetDirty(rightSkeleton,true);
        }
        
        private void Subsystem_Changed(XRHand hand)
        {
            if (hand.handedness == UnityEngine.XR.Hands.Handedness.Left)
            {
                SetDirty(leftSkeleton,true);
            }
            else if (hand.handedness == UnityEngine.XR.Hands.Handedness.Right)
            {
                SetDirty(rightSkeleton,true);
            }
        }

        private static XRHandJointID ToUnityJointID(Joint ubiqJoint)
        {
            return ubiqJoint switch
            {
                Joint.Wrist => XRHandJointID.Wrist,
                Joint.Palm => XRHandJointID.Palm,
                Joint.ThumbMetacarpal => XRHandJointID.ThumbMetacarpal,
                Joint.ThumbProximal => XRHandJointID.ThumbProximal,
                Joint.ThumbDistal => XRHandJointID.ThumbDistal,
                Joint.ThumbTip => XRHandJointID.ThumbTip,
                Joint.IndexMetacarpal => XRHandJointID.IndexMetacarpal,
                Joint.IndexProximal => XRHandJointID.IndexProximal,
                Joint.IndexIntermediate => XRHandJointID.IndexIntermediate,
                Joint.IndexDistal => XRHandJointID.IndexDistal,
                Joint.IndexTip => XRHandJointID.IndexTip,
                Joint.MiddleMetacarpal => XRHandJointID.MiddleMetacarpal,
                Joint.MiddleProximal => XRHandJointID.MiddleProximal,
                Joint.MiddleIntermediate => XRHandJointID.MiddleIntermediate,
                Joint.MiddleDistal => XRHandJointID.MiddleDistal,
                Joint.MiddleTip => XRHandJointID.MiddleTip,
                Joint.RingMetacarpal => XRHandJointID.RingMetacarpal,
                Joint.RingProximal => XRHandJointID.RingProximal,
                Joint.RingIntermediate => XRHandJointID.RingIntermediate,
                Joint.RingDistal => XRHandJointID.RingDistal,
                Joint.RingTip => XRHandJointID.RingTip,
                Joint.LittleMetacarpal => XRHandJointID.LittleMetacarpal,
                Joint.LittleProximal => XRHandJointID.LittleProximal,
                Joint.LittleIntermediate => XRHandJointID.LittleIntermediate,
                Joint.LittleDistal => XRHandJointID.LittleDistal,
                Joint.LittleTip => XRHandJointID.LittleTip,
                _ => XRHandJointID.Invalid
            };
        }
        
        private void PollLeft() => Poll(leftSkeleton, GetHandLeft);
        private void PollRight() => Poll(rightSkeleton, GetHandRight);
        private XRHand GetHandLeft() => subsystem.leftHand;
        private XRHand GetHandRight() => subsystem.rightHand;
        
        private void Poll(WritableSkeleton skeleton, Func<XRHand> getHand)
        {
            if (!skeleton.isDirty)
            {
                return;
            }
            SetDirty(skeleton,false);
            
            if (subsystem == null || !subsystem.running)
            {
                Invalidate(skeleton.joints);
                return;
            }
            
            var unityHand = getHand();
            if (!unityHand.isTracked)
            {
                Invalidate(skeleton.joints);
                return;
            }
            
            var i = 0;
            for (var joint = Joint.BeginMarker; joint < Joint.EndMarker; joint++)
            {
                var jointID = ToUnityJointID(joint);
                skeleton.joints[i++] = unityHand.GetJoint(jointID).TryGetPose(out var pose)
                    ? new InputVar<Pose>(Transforms.ToWorld(pose,origin.transform))
                    : InputVar<Pose>.invalid;
            }
        }
        
        private static void Invalidate(List<InputVar<Pose>> joints)
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
        
        private static void SetDirty(WritableSkeleton skeleton, bool isDirty)
        {
            if (skeleton == null)
            {
                return;
            }
            
            skeleton.isDirty = isDirty;
        }
#endif
    }
}
