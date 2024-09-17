using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.MotionMatching
{
    [Serializable]
    public struct PolarCoordinate
    {
        /// <summary>
        /// Angular component of the coordinate, relative to the T Pose
        /// </summary>
        public float position;

        /// <summary>
        /// Distance from the Hips to the Position, in Percent, relative to the T Pose
        /// </summary>
        public float radius;

        /// <summary>
        /// Lateral distance from the Hips to the Position, relative to the T Pose
        /// </summary>
        public float spread;
    }

    public struct Circle
    {
        public Vector3 normal;
        public float d;
        public float radius;
    }

    [Serializable]
    public struct LegPose
    {
        /// <summary>
        /// The position relative to the hip pivot point in polar coordinates
        /// </summary>
        public PolarCoordinate ankle;

        /// <summary>
        /// The orientation of the Knee relative to the pivot point, expressed
        /// as an Euler angle to the desired position from the reference vector
        /// </summary>
        public float knee;
    }


    [Serializable]
    public struct LowerBodyParams
    {
        public PolarCoordinate left;
        public PolarCoordinate right;
    }

    [Serializable]
    public class Leg
    {
        public Transform pivot;
        public Transform knee;
        public Transform ankle;
        public Transform toes;

        public float upperLength;
        public float lowerLength;

        public float length;
        public Vector3 offset; // From the avatar hips to the root of the leg

        public Leg(IHipSpace hips, Transform pivot, Transform knee, Transform ankle, Transform toes)
        {
            this.pivot = pivot;
            this.knee = knee;
            this.ankle = ankle;
            this.toes = toes;
            this.length = (pivot.position - ankle.position).magnitude;
            this.offset = hips.InverseTransformPoint(pivot.position);
            this.upperLength = (pivot.position - knee.position).magnitude;
            this.lowerLength = (knee.position - ankle.position).magnitude;
        }
    }

    public interface IHipSpace
    {
        Vector3 InverseTransformPoint(Vector3 position);
    }
}