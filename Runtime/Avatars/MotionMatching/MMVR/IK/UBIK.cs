using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

namespace Ubiq.MotionMatching.MMVR
{
    public class UBIK // Upper Body IK
    {
        private quaternion InitHips;
        private Transform Hips;
        private quaternion InitHead;
        private Transform Head;
        private quaternion[] InitSpineChain;
        private Transform[] SpineChain;
        private float[] SpineWeights;
        private quaternion[] InitLeftArmChain;
        private Transform[] LeftArmChain;
        private quaternion[] InitRightArmChain;
        private Transform[] RightArmChain;

        private bool EnabledLeftArmIK;
        private bool EnabledRightArmIK;

        private float SpineLength;
        private float NeckHeadLength;
        private float LeftArmLength;
        private float RightArmLength;
        private float HeadToLeftArmLength;
        private float HeadToRightArmLength;

        public void Init(Transform[] skeleton, quaternion[] initRotations)
        {

            InitRoot(skeleton, initRotations);
            InitSpine(skeleton, initRotations);
            InitArms(skeleton, initRotations);
            const float percentageHeadToArmLength = 1.00f;
            HeadToLeftArmLength = math.sqrt(NeckHeadLength * NeckHeadLength + LeftArmLength * LeftArmLength) * percentageHeadToArmLength;
            HeadToRightArmLength = math.sqrt(NeckHeadLength * NeckHeadLength + RightArmLength * RightArmLength) * percentageHeadToArmLength;
        }

        public void Solve(Target headTarget,
                          Target currentHips)
        {
            Target hipsTarget = currentHips;
            hipsTarget.Rotation = math.mul(hipsTarget.Rotation, math.inverse(InitHips));

            SolveSpine(hipsTarget, headTarget);
        }

        public void Solve(Target headTarget,
                          Target currentHips,
                          Target leftHandTarget,
                          Target rightHandTarget,
                          bool solveRoot = false)
        {
            Target hipsTarget = currentHips;
            hipsTarget.Rotation = math.mul(hipsTarget.Rotation, math.inverse(InitHips));

            if (solveRoot)
            {
                SolveRoot(hipsTarget);
            }
            SolveSpine(hipsTarget, headTarget, leftHandTarget, rightHandTarget);
            SolveArms(hipsTarget, leftHandTarget, rightHandTarget);
        }

        private void InitRoot(Transform[] skeleton, quaternion[] initRotations)
        {
            const int hipsJoint = 0;
            Debug.Assert(skeleton[hipsJoint] != null, "Hips joint must be set");
            InitHips = initRotations[hipsJoint];
            Hips = skeleton[hipsJoint];
        }
        private void SolveRoot(Target hipsTarget)
        {
            // Translate and Rotate Hips
            Hips.SetPositionAndRotation(hipsTarget.Position, math.mul(hipsTarget.Rotation, InitHips));
        }

        private void InitSpine(Transform[] skeleton, quaternion[] initRotations)
        {
            const int headJoint = 13;
            Debug.Assert(skeleton[headJoint] != null, "Head joint must be set");
            Head = skeleton[headJoint];
            InitHead = initRotations[headJoint];
            const int initSpineChainJoint = 9;
            const int upperChestChainJoint = 11;
            const int endSpineChainJoint = 13;
            List<Transform> spineChain = new List<Transform>();
            List<quaternion> initSpineChain = new List<quaternion>();
            SpineLength = 0.0f;
            for (int i = initSpineChainJoint; i <= endSpineChainJoint; ++i)
            {
                if (skeleton[i] != null)
                {
                    spineChain.Add(skeleton[i]);
                    initSpineChain.Add(initRotations[i]);
                    if (spineChain.Count > 1)
                    {
                        SpineLength += math.distance(spineChain[spineChain.Count - 2].position, spineChain[spineChain.Count - 1].position);
                    }
                    if (i > upperChestChainJoint)
                    {
                        NeckHeadLength += math.distance(skeleton[i - 1].position, skeleton[i].position);
                    }
                }
            }
            Debug.Assert(spineChain.Count >= 2, "Spine chain must have at least 2 joints");
            SpineChain = spineChain.ToArray();
            InitSpineChain = initSpineChain.ToArray();
            if (SpineChain.Length == 2)
            {
                SpineWeights = new float[2] { 1.0f, 0.0f };
            }
            else
            {
                SpineWeights = new float[endSpineChainJoint - initSpineChainJoint + 1] { 1.0f, 0.2f, 0.1f, 0.05f, 0.0f };
            }
        }

        private float3 CommonSolveSpine(Target hipsTarget, Target headTarget)
        {
            float3 headTargetPos = headTarget.Position;
            if (math.distance(SpineChain[0].transform.position, headTargetPos) < SpineLength)
            {
                headTargetPos = (float3)SpineChain[0].transform.position + math.normalize(headTargetPos - (float3)SpineChain[0].transform.position) * SpineLength;
            }
            // Restore transforms to init state
            for (int i = 0; i < SpineChain.Length; ++i)
            {
                SpineChain[i].rotation = math.mul(hipsTarget.Rotation, InitSpineChain[i]);
            }
            return headTargetPos;
        }

        private void SolveSpine(Target hipsTarget, Target headTarget)
        {
            float3 headTargetPos = CommonSolveSpine(hipsTarget, headTarget);
            // Rotate Spine with the Head
            //RotationChainIK.Solve(hipsTarget.Rotation, headTarget.Rotation, SpineChain, InitSpineChain, false, true);
            // Translate Spine
            float3 hipsTargetRight = math.mul(hipsTarget.Rotation, math.right());
            float3 hipsTargetForward = math.mul(hipsTarget.Rotation, math.forward());
            CCD.Solve(headTargetPos, headTarget.Position, 0.0f, SpineChain, SpineWeights, hipsTargetRight, SpineLength, minDegrees: -15.0f, maxDegrees: 15.0f);
            CCD.Solve(headTargetPos, headTarget.Position, 0.0f, SpineChain, SpineWeights, hipsTargetForward, SpineLength, minDegrees: -8.0f, maxDegrees: 8.0f);
            // Rotate Head (force always look at the target head)
            Head.rotation = math.mul(headTarget.Rotation, InitHead);
        }

        private void SolveSpine(Target hipsTarget, Target headTarget, Target leftHandTarget, Target rightHandTarget)
        {
            const float maxPercentageHeadToArmLength = 0.25f;
            float3 headTargetPos = CommonSolveSpine(hipsTarget, headTarget);
            float headToLeftHand = math.distance((float3)Head.position, leftHandTarget.Position);
            float headToRightHand = math.distance((float3)Head.position, rightHandTarget.Position);
            float leftFactor = math.clamp((headToLeftHand - HeadToLeftArmLength) / HeadToLeftArmLength, 0.0f, 1.0f) * maxPercentageHeadToArmLength;
            float rightFactor = math.clamp((headToRightHand - HeadToRightArmLength) / HeadToRightArmLength, 0.0f, 1.0f) * maxPercentageHeadToArmLength;
            float3 targetPos = headTargetPos * (1.0f - leftFactor - rightFactor) + leftHandTarget.Position * leftFactor + rightHandTarget.Position * rightFactor;
            float3 hipsTargetRight = math.mul(hipsTarget.Rotation, math.right());
            float3 hipsTargetUp = math.mul(hipsTarget.Rotation, math.up());
            float3 hipsTargetForward = math.mul(hipsTarget.Rotation, math.forward());
            RotateSpineToHands(hipsTarget, leftHandTarget.Position, rightHandTarget.Position, hipsTargetRight, hipsTargetUp, hipsTargetForward);
            // Rotate Spine with the Head
            // RotationChainIK.Solve(hipsTarget.Rotation, headTarget.Rotation, SpineChain, InitSpineChain, false, true);
            // Translate Spine
            CCD.Solve(targetPos, headTarget.Position, (leftFactor + rightFactor) / (maxPercentageHeadToArmLength * 2), SpineChain, SpineWeights, hipsTargetRight, SpineLength, minDegrees: -15.0f, maxDegrees: 15.0f);
            CCD.Solve(targetPos, headTarget.Position, (leftFactor + rightFactor) / (maxPercentageHeadToArmLength * 2), SpineChain, SpineWeights, hipsTargetForward, SpineLength, minDegrees: -8.0f, maxDegrees: 8.0f);
            // Rotate Head (force always look at the target head)
            Head.rotation = math.mul(headTarget.Rotation, InitHead);
        }

        private void RotateSpineToHands(Target hipsTarget, float3 leftHandPos, float3 rightHandPos,
                                        float3 hipsTargetRight, float3 hipsTargetUp, float3 hipsTargetForward)
        {
            const float maxRotDegrees = 120.0f;
            Plane hipsPlaneZY = new Plane(hipsTargetRight, hipsTarget.Position);
            float leftHandDist = hipsPlaneZY.GetDistanceToPoint(leftHandPos) + LeftArmLength * 0.3f;
            float rightHandDist = -hipsPlaneZY.GetDistanceToPoint(rightHandPos) - RightArmLength * 0.3f;
            float leftHandRot = math.clamp(leftHandDist / (LeftArmLength * 0.75f), 0.0f, 1.0f) * maxRotDegrees;
            float rightHandRot = -(math.clamp(rightHandDist / (RightArmLength * 0.75f), 0.0f, 1.0f) * maxRotDegrees);
            float3 forwardLeftHand = Vector3.ProjectOnPlane(leftHandPos - hipsTarget.Position, hipsTargetUp);
            float3 forwardRightHand = Vector3.ProjectOnPlane(rightHandPos - hipsTarget.Position, hipsTargetUp);
            float rot = leftHandRot * math.sign(math.dot(hipsTargetForward, forwardLeftHand)) +
                        rightHandRot * math.sign(math.dot(hipsTargetForward, forwardRightHand));
            quaternion targetRotation = math.mul(quaternion.AxisAngle(hipsTargetUp, math.radians(rot)), hipsTarget.Rotation);
            RotationChainIK.Solve(hipsTarget.Rotation, targetRotation, new Span<Transform>(SpineChain,2,SpineChain.Length-2), new Span<quaternion>(InitSpineChain,2,InitSpineChain.Length-2), false, true);
        }

        private void InitArms(Transform[] skeleton, quaternion[] initRotations)
        {
            const int initLeftArmChainJoint = 14;
            const int endLeftArmChainJoint = 17;
            const int initRightArmChainJoint = 18;
            const int endRightArmChainJoint = 21;
            // Left
            List<Transform> leftArmChain = new List<Transform>();
            List<quaternion> initLeftArmChain = new List<quaternion>();
            for (int i = initLeftArmChainJoint; i <= endLeftArmChainJoint; ++i)
            {
                if (skeleton[i] != null)
                {
                    leftArmChain.Add(skeleton[i]);
                    initLeftArmChain.Add(initRotations[i]);
                    if (i > initLeftArmChainJoint)
                    {
                        LeftArmLength += math.distance(skeleton[i - 1].position, skeleton[i].position);
                    }
                }
            }
            //Debug.Assert(leftArmChain.Count >= 3, "Left arm chain must have at least 3 joints");
            EnabledLeftArmIK = leftArmChain.Count >= 3;
            if (EnabledLeftArmIK)
            {
                LeftArmChain = leftArmChain.ToArray();
                InitLeftArmChain = initLeftArmChain.ToArray();
            }
            // Right
            List<Transform> rightArmChain = new List<Transform>();
            List<quaternion> initRightArmChain = new List<quaternion>();
            for (int i = initRightArmChainJoint; i <= endRightArmChainJoint; ++i)
            {
                if (skeleton[i] != null)
                {
                    rightArmChain.Add(skeleton[i]);
                    initRightArmChain.Add(initRotations[i]);
                    if (i > initRightArmChainJoint)
                    {
                        RightArmLength += math.distance(skeleton[i - 1].position, skeleton[i].position);
                    }
                }
            }
            //Debug.Assert(rightArmChain.Count >= 3, "Right arm chain must have at least 3 joints");
            EnabledRightArmIK = rightArmChain.Count >= 3;
            if (EnabledRightArmIK)
            {
                RightArmChain = rightArmChain.ToArray();
                InitRightArmChain = initRightArmChain.ToArray();
            }
        }
        private void SolveArms(Target hipsTarget, Target leftHandTarget, Target rightHandTarget)
        {
            Debug.Assert(EnabledLeftArmIK && EnabledRightArmIK, "Trying to solve arms IK for an avatar with not all necessary arm joints.");
            static void SolveArm(Target hipsTarget, Target target, quaternion[] init, Transform[] chain)
            {
                // Restore transforms to init state
                for (int i = 0; i < chain.Length; ++i)
                {
                    chain[i].rotation = math.mul(hipsTarget.Rotation, init[i]);
                }
                // Solve Arm
                float3 targetForward = math.mul(target.Rotation, math.forward());
                float3 hipsTargetForward = math.mul(hipsTarget.Rotation, math.forward());
                float3 elbowForward = -targetForward * 0.25f - hipsTargetForward * 0.75f;
                if (chain.Length == 3)
                {
                    ThreeJointIK.Solve(target.Position, chain[0], chain[1], chain[2], elbowForward);
                    // Rotate Hand
                    chain[2].rotation = target.Rotation;
                }
                else
                {
                    ThreeJointIK.Solve(target.Position, chain[1], chain[2], chain[3], elbowForward);
                    // Rotate Hand
                    chain[3].rotation = target.Rotation;
                }
            }
            SolveArm(hipsTarget, leftHandTarget, InitLeftArmChain, LeftArmChain);
            SolveArm(hipsTarget, rightHandTarget, InitRightArmChain, RightArmChain);
        }

        public struct Target
        {
            public float3 Position;
            public quaternion Rotation;

            public Target(float3 position, quaternion rotation)
            {
                Position = position;
                Rotation = rotation;
            }
        }
    }
}