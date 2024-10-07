using System;
using Ubiq;
using Ubiq.Avatars;
using Ubiq.MotionMatching;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ubiq
{
    public class MMVRAvatarInput : MonoBehaviour
    {
        [Tooltip("The AvatarManager to provide input to. If null, will try to find an AvatarManager in the scene at start.")]
        [SerializeField] private AvatarManager avatarManager;
        [Tooltip("Higher priority inputs will override lower priority inputs of the same type if multiple exist.")]
        public int priority;
        [Tooltip("The lower body source to use for input. If null, will try to find a lower body source among child objects at start.")]
        [SerializeField] private MMVRLowerBody lowerBodySource;
        [Tooltip("The transform to use as an offset for the neck from the head.")]
        [SerializeField] private Transform neck;
        [Tooltip("The transform to use as world pose for the left hand in case no left hand input is found. This may happen in case of tracking failure for controllers or hands.")]
        [SerializeField] private Transform leftHandFallback;
        [Tooltip("The transform to use as world pose for the right hand in case no right hand input is found. This may happen in case of tracking failure for controllers or hands.")]
        [SerializeField] private Transform rightHandFallback;
        [Tooltip("The transform containing the latest head pose. Will be frequently overwritten.")]
        [SerializeField] private Transform head;
        [Tooltip("The transform containing the latest leftHand pose. Will be frequently overwritten.")]
        [SerializeField] private Transform leftHand;
        [Tooltip("The transform containing the latest rightHand pose. Will be frequently overwritten.")]
        [SerializeField] private Transform rightHand;
        
        private class MMVRInput : IMMVRInput
        {
            public int priority => owner.priority;
            public bool active => owner.isActiveAndEnabled;
            
            public Pose neck => owner.Neck();
            public Pose leftHand => owner.LeftHand();
            public Pose rightHand => owner.RightHand();
            public LegPose leftLeg => owner.LeftLeg();
            public LegPose rightLeg => owner.RightLeg();
            
            private MMVRAvatarInput owner;
            
            public MMVRInput(MMVRAvatarInput owner)
            {
                this.owner = owner;
            }
            
            private static InputVar<Pose> GetVar(Transform transform)
            {
                return transform 
                    ? new InputVar<Pose>(
                        new Pose(transform.position,transform.rotation))
                    : InputVar<Pose>.invalid;
            }
        }
        
        private MMVRInput input;
        
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
            
            if (!lowerBodySource)
            {
                lowerBodySource = GetComponentInChildren<MMVRLowerBody>();
                
                if (!lowerBodySource)
                {
                    Debug.LogWarning("No LowerBodySource could be found among child objects. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }
            
            input = new MMVRInput(this);
            avatarManager.input.Add((IMMVRInput)input);
        }

        private Pose Neck()
        {
            RefreshHierarchy();
            return new Pose(neck.position,neck.rotation);
        }
        
        private Pose LeftHand()
        {
            RefreshHierarchy();
            return new Pose(leftHand.position,leftHand.rotation);
        }
        
        private Pose RightHand()
        {
            RefreshHierarchy();
            return new Pose(rightHand.position,rightHand.rotation);
        }
        
        private LegPose LeftLeg()
        {
            return lowerBodySource.LeftPose;
        }
        
        private LegPose RightLeg()
        {
            return lowerBodySource.RightPose;
        }
        
        private void RefreshHierarchy()
        {
            if (!avatarManager.input.TryGet(out IHeadAndHandsInput hhInput))
            {
                Debug.LogWarning("Missing IHeadAndHandsInput. Ensure there is an input of this type in the scene.");
                return;
            }
            
            var inputHead = hhInput.head.value;
            if (!hhInput.head.valid)
            {
                // TODO
                Debug.LogWarning("Invalid head pose. Not currently handled.");
            }
            
            if (inputHead.position == head.position 
                && inputHead.rotation == head.rotation)
            {
                return;
            }
            
            var forward = inputHead.forward;
            forward.y = 0;
            forward.Normalize();
            transform.SetPositionAndRotation(
                position: new Vector3(inputHead.position.x, 0, inputHead.position.z),
                rotation: Quaternion.LookRotation(forward, Vector3.up));

            head.position = inputHead.position;
            head.rotation = inputHead.rotation;
            
            var inputLeftHand = hhInput.leftHand.value;
            if (!hhInput.leftHand.valid)
            {
                inputLeftHand = new Pose(leftHandFallback.position,leftHandFallback.rotation);
            }

            //TODO processing to ensure good IK
            
            leftHand.position = inputLeftHand.position;
            leftHand.rotation = inputLeftHand.rotation;
            
            var inputRightHand = hhInput.rightHand.value;
            if (!hhInput.rightHand.valid)
            {
                inputRightHand = new Pose(rightHandFallback.position,rightHandFallback.rotation);
            }

            //TODO processing to ensure good IK
            
            rightHand.position = inputRightHand.position;
            rightHand.rotation = inputRightHand.rotation;
        }
        
        private void OnDestroy()
        {
            if (avatarManager)
            {
                avatarManager.input?.Remove((IMMVRInput)input);
            }
        }
    }
}