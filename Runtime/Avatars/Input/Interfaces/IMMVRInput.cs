using UnityEngine;
using Ubiq.Avatars;
using Ubiq.MotionMatching;

public interface IMMVRInput : AvatarInput.IInput
{
    /// <summary>
    /// Neck position and rotation in world space. Processed to ensure safe
    /// poses for a motion matching/IK rig.
    /// </summary>
    Pose neck { get; } 
        
    /// <summary>
    /// Left hand position and rotation in world space. Processed to ensure
    /// safe poses for a motion matching/IK rig.
    /// </summary>
    Pose leftHand { get; }

    /// <summary>
    /// Right hand position and rotation in world space.  Processed to ensure
    /// safe poses for a motion matching/IK rig.
    /// </summary>
    Pose rightHand { get; }
        
    // /// <summary>
    // /// Hip position and rotation in world space. Processed to ensure safe
    // /// poses for a motion matching/IK rig.
    // /// </summary>
    // Pose hips { get; }
        
    /// <summary>
    /// Representation of left leg pose. Can be used to reconstruct joint
    /// poses.
    /// </summary>
    LegPose leftLeg { get; }
        
    /// <summary>
    /// Representation of right leg pose. Can be used to reconstruct joint
    /// poses.
    /// </summary>
    LegPose rightLeg { get; }
}