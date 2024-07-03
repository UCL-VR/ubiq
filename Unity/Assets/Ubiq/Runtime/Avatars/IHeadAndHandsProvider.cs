using Ubiq.Avatars;
using UnityEngine;

namespace Ubiq
{
    public interface IHeadAndHandsProvider : AvatarInput.IProvider
    {
        /// <summary>
        /// Head position and rotation in world space. May be invalid.
        /// </summary>
        InputVar<Pose> head { get; } 
        
        /// <summary>
        /// Left hand position and rotation in world space. May be invalid.
        /// </summary>
        InputVar<Pose> leftHand { get; }

        /// <summary>
        /// Right hand position and rotation in world space. May be invalid.
        /// </summary>
        InputVar<Pose> rightHand { get; }
        
        /// <summary>
        /// How tightly the left hand is making a fist in [0,1] where 0 is released.
        /// </summary>
        InputVar<float> leftGrip { get; }
        
        /// <summary>
        /// How tightly the right hand is making a fist in [0,1] where 0 is released.
        /// </summary>
        InputVar<float> rightGrip { get; }
    }
}