using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using MathExt = Ubiq.MotionMatching.MathExtensions;

using TrajectoryFeature = Ubiq.MotionMatching.MotionMatchingData.TrajectoryFeature;

namespace Ubiq.MotionMatching.MMVR
{
    public class VRCharacterController : MotionMatchingCharacterController
    {
        [Header("Input Devices")]
        public Transform HMDDevice;

        // General ----------------------------------------------------------
        [Header("General")]
        [Range(0.0f, 1.0f)] public float ResponsivenessPositions = 0.75f;
        [Range(0.0f, 1.0f)] public float ResponsivenessDirections = 0.75f;
        [Range(0.0f, 1.0f)] public float ThresholdNotifyVelocityChange = 0.1f;
        [Header("DEBUG")]
        public bool DebugCurrent = true;
        public bool DebugPrediction = true;
        public bool DebugClamping = true;
        // Adjustment & Clamping --------------------------------------------
        [Header("Adjustment")] // Move Simulation Bone towards the Simulation Object (motion matching towards character controller)
        public bool DoAdjustment = true;
        [Range(0.0f, 2.0f)] public float PositionAdjustmentHalflife = 0.1f; // Time needed to move half of the distance between MotionMatching and SimulationObject
        [Range(0.0f, 2.0f)] public float RotationAdjustmentHalflife = 0.1f;
        [Range(0.0f, 2.0f)] public float PosMaximumAdjustmentRatio = 0.1f; // Ratio between the adjustment and the character's velocity to clamp the adjustment
        [Range(0.0f, 2.0f)] public float RotMaximumAdjustmentRatio = 0.1f; // Ratio between the adjustment and the character's velocity to clamp the adjustment
        public bool DoClamping = true;
        [Range(0.0f, 2.0f)] public float MaxDistanceSimulationBoneAndObject = 0.1f; // Max distance between MotionMatching and SimulationObject
        // --------------------------------------------------------------------------

        private Tracker HMDTracker;
        private float3 PositionHMD; // Position of the Simulation Object (controller) for HMD
        private quaternion RotationHMD; // Rotation of the Simulation Object (controller) for HMD
        private float PreviousHMDDesiredSpeedSq;


        // FUNCTIONS ---------------------------------------------------------------
        private void Start()
        {
            HMDTracker = new Tracker(HMDDevice, this);

            PositionHMD = new float3();
            RotationHMD = new quaternion();
        }

        protected override void OnUpdate()
        {
            Tracker tracker = HMDTracker;
            float3 currentPos = HMDTracker.Device.position;
            quaternion currentRot = HMDTracker.Device.rotation;

            // Input
            float3 desiredVelocity = tracker.GetSmoothedVelocity();
            float sqDesiredVelocity = math.lengthsq(desiredVelocity);
            if (sqDesiredVelocity - PreviousHMDDesiredSpeedSq > ThresholdNotifyVelocityChange * ThresholdNotifyVelocityChange)
            {
                NotifyInputChangedQuickly();
            }
            PreviousHMDDesiredSpeedSq = sqDesiredVelocity;
            tracker.DesiredRotation = HMDDevice.rotation;
            quaternion desiredRotation = tracker.DesiredRotation;

            // Rotations
            tracker.PredictRotations(currentRot, desiredRotation);

            // Positions
            tracker.PredictPositions(currentPos, desiredVelocity);

            // Update Character Controller
            PositionHMD = tracker.ComputeNewPos(currentPos, desiredVelocity);
            RotationHMD = tracker.ComputeNewRot(currentRot, desiredRotation);

            // Adjust MotionMatching to pull the character (moving MotionMatching) towards the Simulation Object (character controller)
            if (DoAdjustment) AdjustSimulationBone();
            if (DoClamping) ClampSimulationBone();
        }

        private void AdjustSimulationBone()
        {
            AdjustCharacterPosition();
            AdjustCharacterRotation();
        }

        private void ClampSimulationBone()
        {
            // Clamp Position
            float3 simulationObject = HMDTracker.Device.position;
            simulationObject.y = 0.0f;
            float3 simulationBone = MotionMatching.GetSkeletonTransforms()[0].position;
            simulationBone.y = 0.0f;
            if (math.distance(simulationObject, simulationBone) > MaxDistanceSimulationBoneAndObject)
            {
                float3 newSimulationBonePos = MaxDistanceSimulationBoneAndObject * math.normalize(simulationBone - simulationObject) + simulationObject;
                MotionMatching.SetPosAdjustment(newSimulationBonePos - simulationBone);
            }
        }

        private void AdjustCharacterPosition()
        {
            float3 simulationObject = HMDTracker.Device.position;
            float3 simulationBone = MotionMatching.GetSkeletonTransforms()[0].position;
            float3 differencePosition = simulationObject - simulationBone;
            differencePosition.y = 0; // No vertical Axis
            // Damp the difference using the adjustment halflife and dt
            float3 adjustmentPosition = Spring.DampAdjustmentImplicit(differencePosition, PositionAdjustmentHalflife, Time.deltaTime);
            // Clamp adjustment if the length is greater than the character velocity
            // multiplied by the ratio
            float maxLength = PosMaximumAdjustmentRatio * math.length(MotionMatching.Velocity) * Time.deltaTime;
            if (math.length(adjustmentPosition) > maxLength)
            {
                adjustmentPosition = maxLength * math.normalize(adjustmentPosition);
            }
            // Move the simulation bone towards the simulation object
            MotionMatching.SetPosAdjustment(adjustmentPosition);
        }

        private void AdjustCharacterRotation()
        {
            float3 simulationObject = HMDTracker.Device.TransformDirection(MotionMatching.MMData.GetLocalForward(0));
            float3 simulationBone = MotionMatching.GetSkeletonTransforms()[0].forward;
            // Only Y Axis rotation
            simulationObject.y = 0;
            simulationObject = math.normalize(simulationObject);
            simulationBone.y = 0;
            simulationBone = math.normalize(simulationBone);
            // Find the difference in rotation (from character to simulation object)
            quaternion differenceRotation = MathExt.FromToRotation(simulationBone, simulationObject, math.up());
            // Damp the difference using the adjustment halflife and dt
            quaternion adjustmentRotation = Spring.DampAdjustmentImplicit(differenceRotation, RotationAdjustmentHalflife, Time.deltaTime);
            // Clamp adjustment if the length is greater than the character angular velocity
            // multiplied by the ratio
            float maxLength = RotMaximumAdjustmentRatio * math.length(MotionMatching.AngularVelocity) * Time.deltaTime;
            if (math.length(MathExt.QuaternionToScaledAngleAxis(adjustmentRotation)) > maxLength)
            {
                adjustmentRotation = MathExt.QuaternionFromScaledAngleAxis(maxLength * math.normalize(MathExt.QuaternionToScaledAngleAxis(adjustmentRotation)));
            }
            // Rotate the simulation bone towards the simulation object
            MotionMatching.SetRotAdjustment(adjustmentRotation);
        }

        private float3 GetCurrentHMDPosition()
        {
            return PositionHMD;
        }
        private quaternion GetCurrentHMDRotation()
        {
            return RotationHMD;
        }

        private float3 GetWorldSpacePosition(int predictionIndex)
        {
            Tracker tracker = HMDTracker;
            return tracker.PredictedPosition[predictionIndex];
        }

        private float3 GetWorldSpaceDirectionPrediction(int index)
        {
            Tracker tracker = HMDTracker;
            float3 dir = math.mul(tracker.PredictedRotations[index], math.forward());
            return math.normalize(dir);
        }

        public override float3 GetWorldInitPosition()
        {
            return transform.position;
        }
        public override float3 GetWorldInitDirection()
        {
            return transform.forward;
        }

        public override void GetTrajectoryFeature(TrajectoryFeature feature, int index, Transform character, NativeArray<float> output)
        {
            Debug.Assert(feature.ZeroY == true, "Project must be true");
            switch (feature.Bone)
            {
                case HumanBodyBones.Head:
                    break;
                case HumanBodyBones.LeftHand:
                    Debug.Assert(false, "LeftHand is not supported");
                    break;
                case HumanBodyBones.RightHand:
                    Debug.Assert(false, "RightHand is not supported");
                    break;
                default:
                    Debug.Assert(false, "Unknown Bone: " + feature.Bone);
                    break;
            }
            switch (feature.FeatureType)
            {
                case TrajectoryFeature.Type.Position:
                    float3 world = GetWorldSpacePosition(index);
                    // Projected to the ground
                    float3 local = character.InverseTransformPoint(new float3(world.x, 0.0f, world.z));
                    output[0] = local.x;
                    output[1] = local.z;
                    break;
                case TrajectoryFeature.Type.Direction:
                    float3 worldDir = GetWorldSpaceDirectionPrediction(index);
                    // Projected to the ground
                    float3 localDir = character.InverseTransformDirection(new Vector3(worldDir.x, 0.0f, worldDir.z));
                    localDir = math.normalize(localDir);
                    output[0] = localDir.x;
                    output[1] = localDir.z;
                    break;
                default:
                    Debug.Assert(false, "Unknown feature type: " + feature.FeatureType);
                    break;
            }
        }

        private class Tracker
        {
            public Transform Device;
            public VRCharacterController Controller;
            // Rotation and Predicted Rotation ------------------------------------------
            public quaternion DesiredRotation;
            public quaternion[] PredictedRotations;
            public float3 AngularVelocity;
            public float3[] PredictedAngularVelocities;
            // Position and Predicted Position ------------------------------------------
            public float3[] PredictedPosition;
            public float3 Velocity;
            public float3 Acceleration;
            public float3[] PredictedVelocity;
            public float3[] PredictedAcceleration;
            // Features -----------------------------------------------------------------
            public int[] TrajectoryPosPredictionFrames;
            public int[] TrajectoryRotPredictionFrames;
            public int NumberPredictionPos { get { return TrajectoryPosPredictionFrames.Length; } }
            public int NumberPredictionRot { get { return TrajectoryRotPredictionFrames.Length; } }
            // Previous -----------------------------------------------------------------
            public float3 PrevInputPos;
            public quaternion PrevInputRot;
            public float3[] PreviousVelocities;
            public int PreviousVelocitiesIndex;
            public float3[] PreviousAngularVelocities;
            public int PreviousAngularVelocitiesIndex;
            public int NumberPastFrames = 1;
            // --------------------------------------------------------------------------

            public Tracker(Transform device, VRCharacterController controller)
            {
                Device = device;
                Controller = controller;
                PrevInputPos = (float3)Device.position;
                PreviousVelocities = new float3[NumberPastFrames]; // HARDCODED
                PreviousAngularVelocities = new float3[NumberPastFrames]; // HARDCODED

                TrajectoryPosPredictionFrames = new int[] { 20, 40, 60 }; // HARDCODED
                TrajectoryRotPredictionFrames = new int[] { 20, 40, 60 }; // HARDCODED
                                                                          // TODO: generalize this... allow different number of prediction frames for different features
                Debug.Assert(TrajectoryPosPredictionFrames.Length == TrajectoryRotPredictionFrames.Length, "Trajectory Position and Trajectory Direction Prediction Frames must be the same for SpringCharacterController");
                for (int i = 0; i < TrajectoryPosPredictionFrames.Length; ++i)
                {
                    Debug.Assert(TrajectoryPosPredictionFrames[i] == TrajectoryRotPredictionFrames[i], "Trajectory Position and Trajectory Direction Prediction Frames must be the same for SpringCharacterController");
                }
                //if (Controller.AverageFPS != TrajectoryPosPredictionFrames[TrajectoryPosPredictionFrames.Length - 1]) Debug.LogWarning("AverageFPS is not the same as the last Prediction Frame... maybe you forgot changing the hardcoded value?");
                //if (Controller.AverageFPS != TrajectoryRotPredictionFrames[TrajectoryRotPredictionFrames.Length - 1]) Debug.LogWarning("AverageFPS is not the same as the last Prediction Frame... maybe you forgot changing the hardcoded value?");


                PredictedPosition = new float3[NumberPredictionPos];
                PredictedVelocity = new float3[NumberPredictionPos];
                PredictedAcceleration = new float3[NumberPredictionPos];
                PredictedRotations = new quaternion[NumberPredictionRot];
                PredictedAngularVelocities = new float3[NumberPredictionRot];
            }

            public void PredictRotations(quaternion currentRotation, quaternion desiredRotation)
            {
                for (int i = 0; i < NumberPredictionRot; i++)
                {
                    // Init Predicted values
                    PredictedRotations[i] = currentRotation;
                    PredictedAngularVelocities[i] = AngularVelocity;
                    // Predict
                    Spring.SimpleSpringDamperImplicit(ref PredictedRotations[i], ref PredictedAngularVelocities[i],
                                                      desiredRotation, 1.0f - Controller.ResponsivenessDirections, TrajectoryRotPredictionFrames[i] * Controller.DatabaseDeltaTime);
                }
            }

            /* https://theorangeduck.com/page/spring-roll-call#controllers */
            public void PredictPositions(float3 currentPos, float3 desiredVelocity)
            {
                for (int i = 0; i < NumberPredictionPos; ++i)
                {
                    PredictedPosition[i] = currentPos;
                    PredictedVelocity[i] = Velocity;
                    PredictedAcceleration[i] = Acceleration;
                    Spring.CharacterPositionUpdate(ref PredictedPosition[i], ref PredictedVelocity[i], ref PredictedAcceleration[i],
                                                   desiredVelocity, 1.0f - Controller.ResponsivenessPositions, TrajectoryPosPredictionFrames[i] * Controller.DatabaseDeltaTime);
                }
            }

            public quaternion ComputeNewRot(quaternion currentRotation, quaternion desiredRotation)
            {
                quaternion newRotation = currentRotation;
                Spring.SimpleSpringDamperImplicit(ref newRotation, ref AngularVelocity, desiredRotation, 1.0f - Controller.ResponsivenessDirections, Time.deltaTime);
                return newRotation;
            }
            
            public float3 ComputeNewPos(float3 currentPos, float3 desiredSpeed)
            {
                float3 newPos = currentPos;
                Spring.CharacterPositionUpdate(ref newPos, ref Velocity, ref Acceleration, desiredSpeed, 1.0f - Controller.ResponsivenessPositions, Time.deltaTime);
                return newPos;
            }

            public float3 GetSmoothedVelocity()
            {
                float3 currentInputPos = (float3)Device.position;
                float3 currentSpeed = (currentInputPos - PrevInputPos) / Time.deltaTime;
                PrevInputPos = currentInputPos;

                PreviousVelocities[PreviousVelocitiesIndex] = currentSpeed;
                PreviousVelocitiesIndex = (PreviousVelocitiesIndex + 1) % PreviousVelocities.Length;

                float3 sum = float3.zero;
                for (int i = 0; i < PreviousVelocities.Length; ++i)
                {
                    sum += PreviousVelocities[i];
                }
                currentSpeed = sum / NumberPastFrames;
                return currentSpeed;
            }
        }

    #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;

            const float radius = 0.05f;
            const float vectorReduction = 0.5f;

            Tracker tracker = HMDTracker;

            Vector3 transformPos = (Vector3)GetCurrentHMDPosition();
            transformPos.y = 0.0f;
            if (DebugCurrent)
            {
                // Draw Current Position & Velocity
                Gizmos.color = new Color(1.0f, 0.3f, 0.1f, 1.0f);
                Gizmos.DrawSphere(transformPos, radius);
                GizmosExtensions.DrawLine(transformPos, transformPos + ((Quaternion)GetCurrentHMDRotation() * Vector3.forward) * vectorReduction, 3);
            }

            if (DebugPrediction)
            {
                // Draw Predicted Position & Velocity
                Gizmos.color = new Color(0.6f, 0.3f, 0.8f, 1.0f);
                for (int i = 0; i < tracker.PredictedPosition.Length; ++i)
                {
                    float3 predictedPos = tracker.PredictedPosition[i];
                    predictedPos.y = 0.0f;
                    float3 predictedDir3D = GetWorldSpaceDirectionPrediction(i);
                    predictedDir3D.y = 0.0f;
                    predictedDir3D = math.normalize(predictedDir3D);
                    Gizmos.DrawSphere(predictedPos, radius);
                    GizmosExtensions.DrawLine(predictedPos, predictedPos + predictedDir3D * vectorReduction, 3);
                }
            }

            if (DebugClamping)
            {
                // Draw Clamp Circle
                if (DoClamping)
                {
                    Gizmos.color = new Color(0.1f, 1.0f, 0.1f, 1.0f);
                    GizmosExtensions.DrawWireCircle(transformPos, MaxDistanceSimulationBoneAndObject, quaternion.identity);
                }
            }
        }
    #endif
    }
}
