using UnityEngine;
using Ubiq.Avatars;

namespace Ubiq
{
    public class HeadAndHandsAvatarInput : MonoBehaviour
    {
        [Tooltip("The AvatarManager to provide input to. If null, will try to find an AvatarManager in the scene at start.")]
        [SerializeField] private AvatarManager avatarManager;
        [Tooltip("The transform to use as the source of the head. If null or not active, will mark provided pose invalid.")]
        public Transform head;
        [Tooltip("The transform to use as the source of the left hand. If null or not active, will mark provided pose invalid.")]
        public Transform leftHand;
        [Tooltip("The transform to use as the source of the right hand. If null or not active, will mark provided pose invalid.")]
        public Transform rightHand;
        [Tooltip("Higher priority inputs will override lower priority inputs of the same type if multiple exist.")]
        public int priority;
        
        private class HeadAndHandsInput : IHeadAndHandsInput
        {
            public int priority => owner.priority;
            public bool active => owner.isActiveAndEnabled;
            
            public InputVar<Pose> head => GetVar(owner.head);
            public InputVar<Pose> leftHand => GetVar(owner.leftHand);
            public InputVar<Pose> rightHand => GetVar(owner.rightHand);
            public InputVar<float> leftGrip => InputVar<float>.invalid;
            public InputVar<float> rightGrip => InputVar<float>.invalid;
            
            private HeadAndHandsAvatarInput owner;
            
            public HeadAndHandsInput(HeadAndHandsAvatarInput owner)
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
        
        private HeadAndHandsInput input;
        
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
            
            input = new HeadAndHandsInput(this);
            avatarManager.input.Add((IHeadAndHandsInput)input);
        }

        private void OnDestroy()
        {
            if (avatarManager)
            {
                avatarManager.input?.Remove((IHeadAndHandsInput)input);
            }
        }
    }
}