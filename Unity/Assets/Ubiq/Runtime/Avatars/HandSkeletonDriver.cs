using UnityEngine;
using Joint = Ubiq.HandSkeleton.Joint;

namespace Ubiq
{
    /// <summary>
    /// Manipulates the Transform hierarchy representing the bones of a hand
    /// into the poses specified by a <see cref="HandSkeleton"/>. 
    /// </summary>
    public class HandSkeletonDriver : MonoBehaviour
    {

        [SerializeField] private Transform[] bones = new Transform[(int)Joint.EndMarker];
        [SerializeField] private Pose[] offsets = new Pose[(int)Joint.EndMarker];
        
        /// <summary>
        /// Define which transform to animate for a given joint.
        /// </summary>
        /// <param name="joint">The joint of the hand this targets.</param>
        /// <param name="transform">The transform representing the bone. May be null to clear a mapping.</param>
        public void SetBone(Joint joint, Transform transform)
        {
            SetBone(joint,transform,Pose.identity);
        }

        /// <inheritdoc cref="SetBone"/>
        /// <param name="localOffset">Offset to apply to the joint in local space.</param>
        public void SetBone(Joint joint, Transform transform, Pose localOffset)
        {
            if (joint < Joint.BeginMarker || joint >= Joint.EndMarker)
            {
                return;
            } 
            
            bones[joint.Idx()] = transform;
            offsets[joint.Idx()] = localOffset;
        }
        
        /// <summary>
        /// Clear the mapping between all joints and transforms.
        /// </summary>
        public void ClearBones()
        {
            for (var i = 0; i < bones.Length; i++)
            {
                bones[i] = null;
                offsets[i] = Pose.identity;
            }
        }
        
        /// <summary>
        /// Immediately sets the transform hierarchy into the poses specified by
        /// a <see cref="HandSkeleton"/>. Poses are interpreted as world space.
        /// Will not cache or re-use the <see cref="HandSkeleton"/>, so needs to
        /// be called whenever you want the hand position updated.
        /// </summary>
        /// <param name="skeleton">The hand itself. Will not be cached.</param>
        public void SetPoses(HandSkeleton skeleton)
        {
            for(var joint = Joint.BeginMarker; joint < Joint.EndMarker; joint++)
            {
                if (!skeleton.TryGetPose(joint, out var pose))
                {
                    continue;
                }
                
                var jointTransform = bones[joint.Idx()];
                
                if (!jointTransform)
                {
                    continue;
                }
                
                var offset = offsets[joint.Idx()]; 
                
                if (offset.rotation != Quaternion.identity)
                {
                    pose.rotation *= offset.rotation;
                }
                
                if (offset.position != Vector3.zero)
                {
                    pose.position += 
                        pose.right * offset.position.x +
                        pose.up * offset.position.y +
                        pose.forward * offset.position.z;
                }
                
                jointTransform.SetPositionAndRotation(pose.position,pose.rotation);
            }
        }
    }
}