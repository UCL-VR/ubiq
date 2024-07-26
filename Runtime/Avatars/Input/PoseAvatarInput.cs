using UnityEngine;
using Ubiq.Avatars;

namespace Ubiq
{
    public class PoseAvatarInput : MonoBehaviour
    {
        [Tooltip("The AvatarManager to provide input to. If null, will try to find an AvatarManager in the scene at start.")]
        [SerializeField] private AvatarManager avatarManager;
        [Tooltip("The transform to use as the source of the pose. If or not active, will mark provided pose invalid.")]
        public Transform sourceTransform;
        [Tooltip("Higher priority inputs will override lower priority inputs of the same type if multiple exist.")]
        public int priority;
        
        private class PoseInput : IPoseInput
        {
            public int priority => owner.priority;
            public bool active => owner.isActiveAndEnabled;
            public InputVar<Pose> pose => owner.sourceTransform 
                ? new InputVar<Pose>(
                    new Pose(
                        owner.sourceTransform.position,
                        owner.sourceTransform.rotation))
                : InputVar<Pose>.invalid;
            
            private PoseAvatarInput owner;
            
            public PoseInput(PoseAvatarInput owner)
            {
                this.owner = owner;
            }
        }
        
        private PoseInput poseInput;
        
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
            
            poseInput = new PoseInput(this);
            avatarManager.input.Add((IPoseInput)poseInput);
        }

        private void OnDestroy()
        {
            if (avatarManager)
            {
                avatarManager.input.Remove((IPoseInput)poseInput);
            }
        }
    }
}