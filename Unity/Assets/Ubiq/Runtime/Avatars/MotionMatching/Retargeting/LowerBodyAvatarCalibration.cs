using UnityEngine;

namespace Ubiq.MotionMatching
{
    [CreateAssetMenu(fileName = "Generic", menuName = "MotionMatching/LowerBodyCalibration")]
    public class LowerBodyAvatarCalibration : ScriptableObject
    {
        /// <summary>
        /// Rotates from the rig's hip bind pose orientation frame to the local
        /// hip orientation frame, where forwards faces outwards.
        /// </summary>
        public Vector3 HipsToLocal;

        /// <summary>
        /// Rotates from the local leg orientation frame (i.e. forward down) to
        /// the bind pose orientation frame of the rig.
        /// </summary>
        public Vector3 LocalToLeg;
    }
}