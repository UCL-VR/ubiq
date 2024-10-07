using System;
using UnityEngine;
using Unity.Mathematics;

namespace Ubiq.MotionMatching.MMVR
{
    public static class RotationChainIK
    {
        /// <summary>
        /// Orients a chain based on the target orientations of the initial and end joints of a chain.
        /// </summary>
        public static void Solve(quaternion initialTarget, quaternion lastTarget, Span<Transform> joints, Span<quaternion> initialJoints, bool initialTargetInChain, bool lastTargetInChain, float exponentialDecay = 1.0f)
        {
            Debug.Assert(joints.Length == initialJoints.Length, "The number of joints and initial joints must be the same.");

            // Apply the rotation to the joints
            for (int i = 0; i < joints.Length; ++i)
            {
                float t;
                if (initialTargetInChain && lastTargetInChain)
                {
                    t = (float)i / (joints.Length - 1);
                }
                else if (!initialTargetInChain && lastTargetInChain)
                {
                    t = (float)(i + 1) / joints.Length;
                }
                else
                {
                    t = (float)(i + 1) / (joints.Length + 1);
                }
                quaternion targetRotation = math.slerp(initialTarget, lastTarget, math.pow(t, exponentialDecay));
                joints[i].rotation = math.mul(targetRotation, initialJoints[i]);
            }
        }

        /// <summary>
        /// Orients a chain based on the target orientation of the end joints of a chain.
        /// </summary>
        public static void Solve(quaternion lastTarget, Span<Transform> joints, Span<quaternion> initialJoints, bool lastTargetInChain, float exponentialDecay = 1.0f)
        {
            Debug.Assert(joints.Length == initialJoints.Length, "The number of joints and initial joints must be the same.");

            quaternion initialTarget = joints[0].rotation;

            // Apply the rotation to the joints
            for (int i = 0; i < joints.Length; ++i)
            {
                float t;
                if (lastTargetInChain)
                {
                    t = (float)i / (joints.Length - 1);
                }
                else
                {
                    t = (float)i / joints.Length;
                }
                quaternion targetRotation = math.slerp(initialTarget, lastTarget, math.pow(t, exponentialDecay));
                joints[i].rotation = math.mul(targetRotation, initialJoints[i]);
            }
        }
    }
}