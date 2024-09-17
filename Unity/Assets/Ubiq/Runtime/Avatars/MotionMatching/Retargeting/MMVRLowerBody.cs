using Ubiq.MotionMatching;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.MotionMatching
{
    /// <summary>
    /// A LowerBody source designed for use with the MMVR Humanoid Rig.
    /// </summary>
    public class MMVRLowerBody : LowerBody, IHipSpace
    {
        [Header("Calibration")]
        public float angleOffset = 0f;
        public float inclinationOffset = 0f;

        private Transform hips;
        private Leg left;
        private Leg right;

        // These calibration properties are fixed for the MMVR rig. To adapt
        // for other rigs, create a new subclass of LowerBody based on this
        // implementation.

        private Quaternion HipsToLocal => Quaternion.Euler(0, 180, 0);
        private Quaternion LocalToHips => Quaternion.Inverse(HipsToLocal);

        private void Start()
        {
            var transforms = new Dictionary<string, Transform>();

            // This Component works best with an MMVR Motion Matching Controller,
            // however it can also work with a design-time rig of the same
            // hierarchy. 

            // It is not meant to work with general purpose rigs however - for that
            // create a new subclass.

            var controller = GetComponent<MotionMatchingController>();
            if (controller)
            {
                var data = controller.MMData;
                var animationdata = data.AnimationDataTPose;
                var animation = animationdata.GetAnimation();
                var skeleton = animation.Skeleton;

                foreach (var transform in controller.GetSkeletonTransforms())
                {
                    transforms.Add(transform.name, transform);
                }
            }
            else
            {
                foreach (var bone in GetComponentsInChildren<Transform>())
                {
                    transforms.Add(bone.name, bone);
                }
            }

            hips = transforms["Hips"];

            left = new Leg(
                this,
                transforms["LeftHip"],
                transforms["LeftKnee"],
                transforms["LeftAnkle"],
                transforms["LeftToe"]
            );

            right = new Leg(
                this,
                transforms["RightHip"],
                transforms["RightKnee"],
                transforms["RightAnkle"],
                transforms["RightToe"]
            );
        }

        private void Update()
        {
            UpdatePose(left, ref LeftPose);
            UpdatePose(right, ref RightPose);
        }

        private void UpdatePose(Leg leg, ref LegPose pose)
        {
            GetAnklePose(leg, ref pose.ankle); // Do this before getting the knee pose
            GetKneePose(leg, ref pose);
        }

        /// <summary>
        /// Transforms from World Space into Local (Hip) Space, including any
        /// corrective transforms.
        /// </summary>
        public Vector3 InverseTransformPoint(Vector3 world)
        {
            return LocalToHips * hips.InverseTransformPoint(world);
        }

        private Vector3 InverseTransformDirection(Vector3 world)
        {
            return -(LocalToHips * hips.InverseTransformDirection(world));
        }

        private Vector3 TransformPoint(Vector3 local)
        {
            return  hips.TransformPoint(HipsToLocal * local);
        }

        private void GetAnklePose(Leg leg, ref PolarCoordinate parms)
        {
            var p = InverseTransformPoint(leg.ankle.position) - leg.offset;

            parms.radius = p.magnitude / leg.length;

            Vector3 yz = new Vector3(0, p.y, p.z);
            yz.Normalize();
            parms.position = -Mathf.Atan2(yz.z, -yz.y) * Mathf.Rad2Deg;

            Vector3 xy = new Vector3(p.x, p.y, 0);
            xy.Normalize();
            parms.spread = Mathf.Atan2(xy.x, -xy.y) * Mathf.Rad2Deg;

            parms.position += angleOffset;
        }

        private void GetKneePose(Leg leg, ref LegPose pose)
        {
            // Get the parameter for the knee. This can be acquired directly from
            // the transform of the knee bone.

            // Get the reference vector in the same way it would be recovered
            // in the destination component.

            var ankle = InverseTransformPoint(leg.ankle.position) - leg.offset;

            // Get the cirlcle describing the possible positions of the knee
            var kp = Utils.SphereSphereIntersection(Vector3.zero, ankle, leg.upperLength, leg.lowerLength);

            // knee plane origin & plane
            var o = (kp.normal * kp.d);
            var p = new Plane(kp.normal, o);

            // The hips forward vector transformed by the ankle orientation -
            // this will provide the reference vector for the knee rotation
            var forward = GetOrientation(pose.ankle) * Vector3.forward;

            // Get the reference vector in the plane
            forward = (p.ClosestPointOnPlane(o + forward) - o).normalized;

            // Get the knee forward in the plane. We use the orientation of the
            // transform here to ensure robustness to noise in the position that
            // is expected from real motion capture, when the leg is fully
            // extended.

            var knee = (p.ClosestPointOnPlane(o + InverseTransformDirection(leg.knee.forward)) - o).normalized;

            var angle = Vector3.Dot(forward, knee.normalized);
            if (angle < 1 - Mathf.Epsilon) // Acos has a limited input range
            {
                pose.knee = Mathf.Acos(angle) * Mathf.Rad2Deg * Mathf.Sign(Vector3.Dot(Vector3.Cross(forward, knee.normalized), -kp.normal));
            }
        }

        private Quaternion GetOrientation(PolarCoordinate coord)
        {
            return Quaternion.AngleAxis(coord.position, Vector3.right) * Quaternion.AngleAxis(coord.spread, Vector3.forward);
        }
    }
}