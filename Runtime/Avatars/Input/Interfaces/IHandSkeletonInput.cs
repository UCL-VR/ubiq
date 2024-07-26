using System.Collections.ObjectModel;
using Ubiq.Avatars;
using UnityEngine;

namespace Ubiq
{
    public interface IHandSkeletonInput : AvatarInput.IInput
    {
        /// <summary>
        /// A collection of joints representing the skeleton of the left hand.
        /// </summary>
        HandSkeleton leftHandSkeleton { get; }
        
        /// <summary>
        /// A collection of joints representing the skeleton of the right hand.
        /// </summary>
        HandSkeleton rightHandSkeleton { get; }
    }
    
    public readonly struct HandSkeleton
    {
        public enum Joint
        {
            BeginMarker = 0,
            Wrist = BeginMarker,
            Palm,
            ThumbMetacarpal,
            ThumbProximal,
            ThumbDistal,
            ThumbTip,
            IndexMetacarpal,
            IndexProximal,
            IndexIntermediate,
            IndexDistal,
            IndexTip,
            MiddleMetacarpal,
            MiddleProximal,
            MiddleIntermediate,
            MiddleDistal,
            MiddleTip,
            RingMetacarpal,
            RingProximal,
            RingIntermediate,
            RingDistal,
            RingTip,
            LittleMetacarpal,
            LittleProximal,
            LittleIntermediate,
            LittleDistal,
            LittleTip,
            EndMarker,
            Count = EndMarker,
            Invalid = EndMarker
        }
        
        public enum Finger
        {
            BeginMarker = 0,
            Thumb = BeginMarker,
            Index,
            Middle,
            Ring,
            Little,
            EndMarker,
            Count = EndMarker,
            Invalid = EndMarker
        }
        
        public enum Handedness
        {
            Left = 0,
            Right,
            Invalid
        }
        
        /// <summary>
        /// Convert an index to a joint. Named this way to avoid confusion with
        /// the index finger.
        /// </summary>
        /// <param name="idx">The index itself.</param>
        /// <returns>The joint.</returns>
        public static Joint JointByIdx(int idx) => (Joint)idx;
        
        /// <summary>
        /// Whether left or right hand.
        /// </summary>
        public Handedness handedness { get; }
        
        /// <summary>
        /// Collection of poses for the joints. To find the index of particular
        /// joint, use <see cref="HandExtensions.Idx"/>. This collection
        /// wraps an underlying array and will change as new tracking
        /// information is received. Clone it if you need a snapshot of the hand
        /// skeleton at a specific time.
        /// </summary>
        public ReadOnlyCollection<InputVar<Pose>> poses { get; }
        
        /// <summary>
        /// Attempt to get the current pose for a given joint. Will fail if
        /// the joint is invalid, or if the pose for this joint is currently
        /// invalid. This may happen if tracking is not available, for example. 
        /// </summary>
        /// <param name="joint">The joint itself.</param>
        /// <param name="pose">The pose, if found.</param>
        /// <returns>Whether a pose could be found.</returns>
        public bool TryGetPose(Joint joint, out Pose pose)
        {
            if (joint < Joint.EndMarker 
                && joint >= Joint.BeginMarker 
                && poses != null)
            {
                var inputVar = poses[joint.Idx()];
                if (inputVar.valid)
                {
                    pose = inputVar.value;
                    return true;
                }
            }
            pose = default;
            return false;
        }
        
        /// <summary>
        /// Create a new hand skeleton.
        /// </summary>
        /// <param name="handedness">Whether left or right hand.</param>
        /// <param name="poses">Collection of poses for the joints.</param>
        public HandSkeleton(Handedness handedness, ReadOnlyCollection<InputVar<Pose>> poses)
        {
            this.handedness = handedness;
            this.poses = poses;
        }
    }
    
    public static class HandExtensions
    {
        /// <summary>
        /// Get the base (metacarpal) joint of a finger. Use
        /// <see cref="Idx"/> to find the index in the
        /// <see cref="HandSkeleton.poses"/> collection. To
        /// iterate through all joints for a finger, use
        /// <see cref="Metacarpal"/> as the start point and
        /// <see cref="Tip"/> as the end point (inclusive).
        /// </summary>
        /// <param name="finger">The finger itself.</param>
        /// <returns>The metacarpal joint for the finger.</returns>
        public static HandSkeleton.Joint Metacarpal(this HandSkeleton.Finger finger)
        {
            return finger switch
            {
                HandSkeleton.Finger.Thumb => HandSkeleton.Joint.ThumbMetacarpal,
                HandSkeleton.Finger.Index => HandSkeleton.Joint.IndexMetacarpal,
                HandSkeleton.Finger.Middle => HandSkeleton.Joint.MiddleMetacarpal,
                HandSkeleton.Finger.Ring => HandSkeleton.Joint.RingMetacarpal,
                HandSkeleton.Finger.Little => HandSkeleton.Joint.LittleMetacarpal,
                _ => HandSkeleton.Joint.Invalid
            };
        }
        
        /// <summary>
        /// Get the tip joint of a finger. Use <see cref="Idx"/> to find the
        /// index in the <see cref="HandSkeleton.poses"/> collection. To
        /// iterate through all joints for a finger, use
        /// <see cref="Metacarpal"/> as the start point and
        /// <see cref="Tip"/> as the end point (inclusive).
        /// </summary>
        /// <param name="finger"></param>
        /// <returns>The tip joint for the finger</returns>
        public static HandSkeleton.Joint Tip(this HandSkeleton.Finger finger)
        {
            return finger switch
            {
                HandSkeleton.Finger.Thumb => HandSkeleton.Joint.ThumbTip,
                HandSkeleton.Finger.Index => HandSkeleton.Joint.IndexTip,
                HandSkeleton.Finger.Middle => HandSkeleton.Joint.MiddleTip,
                HandSkeleton.Finger.Ring => HandSkeleton.Joint.RingTip,
                HandSkeleton.Finger.Little => HandSkeleton.Joint.LittleTip,
                _ => HandSkeleton.Joint.Invalid
            };
        }
        
        /// <summary>
        /// Convert a joint to an index that can be used with
        /// <see cref="HandSkeleton.poses"/>. Named this way to avoid confusion
        /// with the index finger.
        /// </summary>
        /// <param name="joint">The joint itself.</param>
        /// <returns>An index into the <see cref="HandSkeleton.poses"/> collection.</returns>
        public static int Idx(this HandSkeleton.Joint joint) => (int)joint;
    }
}