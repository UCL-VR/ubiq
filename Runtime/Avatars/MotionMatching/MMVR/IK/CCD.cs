using System;
using Unity.Mathematics;
using UnityEngine;

namespace Ubiq.MotionMatching.MMVR
{
    public static class CCD
    {
        /// <summary>
        /// Solve CCD for the last joint of the chain (joints array)
        /// minDegrees and maxDegrees are only provided as an intuitive way to limit the rotation of the joints
        /// but do not expect the joint to be exactly within these limits after solving
        /// </summary>
        public static void Solve(float3 target, float3 headTargetPos, float leftRightFactor, Span<Transform> joints, Span<float> weights, float3 rotAxis, float spineLength,
                                 int numberIterations = 5, float minDegrees = -180.0f, float maxDegrees = 180.0f)
        {
            if (joints.Length != weights.Length)
                throw new ArgumentException("joints and weights must have the same length.");

            Transform endEffector = joints[joints.Length - 1];

            if (math.distancesq(endEffector.position, target) < 0.001f)
                return;

            // The closer the target to the spine vector, the less rotation should be applied
            float3 headTargetSpine = headTargetPos - (float3)joints[0].position;
            float3 spineVector = math.normalize(endEffector.position - joints[0].position);
            float3 projectedHeadTargetSpine = math.dot(headTargetSpine, spineVector) * spineVector;
            float headToSpineDistance = math.length(headTargetSpine - projectedHeadTargetSpine);
            float targetSpineFactor = math.clamp((headToSpineDistance / spineLength) - spineLength * 0.1f, 0.0f, 1.0f);
            targetSpineFactor = math.pow(targetSpineFactor, 2.0f);
            targetSpineFactor = math.clamp(targetSpineFactor + leftRightFactor * 2, 0.0f, 1.0f);

            float minRotDeg = math.radians(minDegrees);
            float maxRotDeg = math.radians(maxDegrees);

            for (int it = 0; it < numberIterations; ++it)
            {
                for (int i = joints.Length - 2; i >= 0; --i)
                {
                    Transform joint = joints[i];
                    float weight = weights[i] * targetSpineFactor;
                    float3 eeJoint = endEffector.position - joint.position;
                    float3 targetJoint = target - (float3)joint.position;
                    float3 eeJointUnit = math.normalize(eeJoint);
                    float3 targetJointUnit = math.normalize(targetJoint);
                    quaternion rot = MathExtensions.FromToRotation(eeJointUnit, targetJointUnit);
                    // constain rot by a maximum angle
                    (float3 axis, float angle) = rot.ToAxisAngle();
                    angle = math.clamp(angle, minRotDeg, maxRotDeg);
                    rot = quaternion.AxisAngle(axis, angle);
                    // apply rotation
                    rot = MathExtensions.ScaleRotation(rot, weight);  // Assuming ScaleRotation scales the quaternion angle by weight
                    joint.rotation = math.mul(rot, joint.rotation);
                    float3 rotatedAxis = math.mul(rot, rotAxis);
                    quaternion rotBack = MathExtensions.FromToRotation(rotatedAxis, rotAxis);
                    joint.rotation = math.mul(rotBack, joint.rotation);
                }
            }
        }
    }
}