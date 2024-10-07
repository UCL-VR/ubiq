using System;
using UnityEngine;
using UnityEngine.Events;

namespace Ubiq.MotionMatching
{
    /// <summary>
    /// A LowerBody destination designed to work with a Unity Humanoid enabled
    /// avatar.
    /// </summary>
    public class LowerBodyAvatar : MonoBehaviour, IHipSpace
    {
        [Tooltip("The source of the information to use for reconstructing the lower body pose. If null, will try to find among parents at start.")]
        public MMVRAvatar mmvrAvatar;
        
        public SkeletonUpdatedEvent OnSkeletonUpdated; 
        [Serializable] public class SkeletonUpdatedEvent : UnityEvent { }
        
        public bool UpdateRootTransform = true;
        public bool UpdateLegTransforms = true;

        public LowerBodyAvatarCalibration Calibration;

        [Header("Debug")]

        public bool DrawLegs = false;
        public bool DrawKnee = false;
        public bool UpdateCalibration = false;

        private Transform hips;
        private Leg left;
        private LegPose leftPose;
        private Leg right;
        private LegPose rightPose;

        private Quaternion hipsToLocal = Quaternion.identity;
        private Quaternion localToHips => Quaternion.Inverse(hipsToLocal);
        private Quaternion localToLeg = Quaternion.identity;

        private void Start()
        {
            if (!mmvrAvatar)
            {
                mmvrAvatar = GetComponentInParent<MMVRAvatar>();
                if (!mmvrAvatar)
                {
                    Debug.LogWarning("No MMVRAvatar could be found among parents. This script will be disabled.");
                    enabled = false;
                    return;
                }
            }
            
            InitialiseBindPose();
            
            mmvrAvatar.OnPosesUpdated.AddListener(MMVRAvatar_OnPosesUpdated);
        }

        private void OnDestroy()
        {
            if (mmvrAvatar)
            {
                mmvrAvatar.OnPosesUpdated.RemoveListener(MMVRAvatar_OnPosesUpdated);
            }
        }

        private void MMVRAvatar_OnPosesUpdated()
        {
            Refresh();
        }

        public Vector3 InverseTransformPoint(Vector3 world)
        {
            return localToHips * hips.InverseTransformPoint(world);
        }

        private void InitialiseBindPose()
        {
            if (Calibration)
            {
                hipsToLocal = Quaternion.Euler(Calibration.HipsToLocal);
                localToLeg = Quaternion.Euler(Calibration.LocalToLeg);
            }

            var animator = GetComponent<Animator>();
            hips = animator.GetBoneTransform(HumanBodyBones.Hips);

            left = new Leg(
                this,
                animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg),
                animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg),
                animator.GetBoneTransform(HumanBodyBones.LeftFoot),
                animator.GetBoneTransform(HumanBodyBones.LeftToes)
            );

            right = new Leg(
                this,
                animator.GetBoneTransform(HumanBodyBones.RightUpperLeg),
                animator.GetBoneTransform(HumanBodyBones.RightLowerLeg),
                animator.GetBoneTransform(HumanBodyBones.RightFoot),
                animator.GetBoneTransform(HumanBodyBones.RightToes)
            );
        }

        private void Refresh()
        {
            if(UpdateCalibration && Calibration)
            {
                hipsToLocal = Quaternion.Euler(Calibration.HipsToLocal);
                localToLeg = Quaternion.Euler(Calibration.LocalToLeg);
            }

            if (UpdateRootTransform && mmvrAvatar && mmvrAvatar.enabled 
                && mmvrAvatar.hasInput)
            {
                var pos = mmvrAvatar.neck.position;
                pos.y = 0;
                transform.position = pos;
                
                var fwd = mmvrAvatar.neck.forward;
                fwd.y = 0;
                transform.forward = fwd;
            }

            if (UpdateLegTransforms && mmvrAvatar && mmvrAvatar.enabled 
                && mmvrAvatar.hasInput)
            {
                leftPose = mmvrAvatar.leftLeg;
                rightPose = mmvrAvatar.rightLeg;
            }

            ApplyTransforms(right, rightPose);
            ApplyTransforms(left, leftPose);
            
            OnSkeletonUpdated.Invoke();
        }

        private void ApplyTransforms(Leg leg, LegPose pose)
        {
            // Transforms are always applied as rotations - we first work out
            // the desired transform in local hip space, and then transform it
            // into the world coordinate system of the avatar, before applying
            // it to the transform in the scene.

            // Get the direction of the upper leg in hip-space

            var ankle = GetAnklePosition(leg, pose);
            var knee = GetKneePosition(leg, pose);

            // Move relative to the pivot point

            knee = knee - leg.offset;
            ankle = ankle - leg.offset;

            // For the purposes of computing the orientation using LookRotation,
            // the local hip space considers bones normals to be pointing forward
            // and the direction to be pointing towards the descendant.

            // The normal vector of the upper leg, at the hips

            var up1 = Vector3.Cross(knee.normalized, Vector3.right);

            // The normal vector of the upper leg, at the knee

            var up2 = Quaternion.AngleAxis(-90, knee.normalized) * Vector3.Cross(ankle.normalized, knee.normalized).normalized;

            // Depending on the skinnng, we can apply the roll to the upper leg
            // bone or knee bone.
            // (It would be applied at the knee if a linear blend is performed
            // in the shader, for example. If not, it should be applied at the
            // pivot.)

            // This version applies it to the pivot/hips.

            ApplyRotation(Quaternion.LookRotation(knee, up2), leg.pivot);

            // Next, update the knee rotation. Get the position of the ankle
            // relative to the knee.

            var ak = ankle - knee;

            // Even when there is a linear blend along the upper leg, the normal
            // vector at the start of the lower leg is always the same. This is
            // the vector at the end of the upper leg, rotated by the actual knee
            // joint's rotation.

            // We can use the dot product here because the knee should never rotate
            // the other way.

            var kneeHingeAxis = Quaternion.AngleAxis(-90, knee.normalized) * up2;
            var kneeAngle = Mathf.Acos(Vector3.Dot(knee.normalized, ak.normalized));
            var kneeUp = Quaternion.AngleAxis(kneeAngle * Mathf.Rad2Deg, kneeHingeAxis) * up2;

            ApplyRotation(Quaternion.LookRotation(ak, kneeUp), leg.knee);
        }

        /// <summary>
        /// Applies a rotation defined in hip-space to the skeletal rig in world
        /// space, considering any rotational offsets that should be applied
        /// due to the rigging.
        /// </summary>
        private void ApplyRotation(Quaternion rotation, Transform bone)
        {
            // Apply any corrective rotation as per the original rig - this is
            // done in hip space.
            rotation = rotation * localToLeg;

            // Counteract the rotation that will be applied by the hips
            rotation = hipsToLocal * rotation;

            // Apply the hips rotation to get in world space
            rotation = hips.rotation * rotation;

            // Finally apply the rotation to the scene graph
            bone.rotation = rotation;
        }

        private Quaternion GetOrientation(PolarCoordinate coord)
        {
            return Quaternion.AngleAxis(coord.position, Vector3.right) * Quaternion.AngleAxis(coord.spread, Vector3.forward);
        }

        private Vector3 GetAnklePosition(Leg leg, LegPose pose)
        {
            return leg.offset + GetOrientation(pose.ankle) * Vector3.down * pose.ankle.radius * leg.length;
        }

        private struct KneePlane
        {
            public Plane p;
            public Vector3 o;
            public Vector3 normal;
            public Vector3 forward; // The reference vector
        }

        private KneePlane GetKneePlane(Leg leg, LegPose pose)
        {
            var ankle = GetAnklePosition(leg, pose);

            // Get the cirlcle describing the possible positions of the knee
            var kp = GetKneeIntersection(leg, ankle);

            // knee plane origin & plane
            var o = leg.offset + (kp.normal * kp.d);
            var p = new Plane(kp.normal, o);

            // The hips forward vector transformed by the ankle orientation -
            // this will provide the reference vector for the knee rotation
            var forward = GetOrientation(pose.ankle) * Vector3.forward;

            // Get the reference vector in the plane
            forward = (p.ClosestPointOnPlane(o + forward) - o).normalized * kp.radius;

            return new KneePlane()
            {
                o = o,
                normal = kp.normal,
                forward = forward,
                p = p
            };
        }

        private Vector3 GetKneePosition(Leg leg, LegPose pose)
        {
            var s = GetKneePlane(leg, pose);
            return s.o + Quaternion.AngleAxis(-pose.knee, s.normal) * s.forward;
        }

        private Circle GetKneeIntersection(Leg leg, Vector3 anklePosition)
        {
            return Utils.SphereSphereIntersection(leg.offset, anklePosition, leg.upperLength, leg.lowerLength);
        }

        private void OnDrawGizmos()
        {
            if (!enabled)
            {
                return;
            }

            if (!mmvrAvatar || !mmvrAvatar.enabled || !mmvrAvatar.hasInput)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                InitialiseBindPose();
            }

            Gizmos.color = Color.yellow;
            Gizmos.matrix = hips.localToWorldMatrix * Matrix4x4.Rotate(hipsToLocal);

            DrawGizmos(left, leftPose);
            DrawGizmos(right, rightPose);
        }

        private void DrawGizmos(Leg leg, LegPose pose)
        {
            var ankle = GetAnklePosition(leg, pose);
            var kp = GetKneePosition(leg, pose);

            if (DrawKnee)
            {
                var c = GetKneeIntersection(leg, ankle);
                var q = Quaternion.FromToRotation(Vector3.forward, c.normal);
                var v0 = Vector3.zero;
                for (var i = 0f; i <= Mathf.PI * 2; i += (Mathf.PI / 10f))
                {
                    var x = Mathf.Cos(i) * c.radius;
                    var y = Mathf.Sin(i) * c.radius;
                    var z = c.d;

                    var v = new Vector3(x, y, z);

                    v = leg.offset + (q * v);

                    if (i > 0)
                    {
                        Gizmos.DrawLine(v0, v);
                    }
                    v0 = v;
                }

                var s = GetKneePlane(leg, pose);
                Gizmos.DrawLine(s.o, s.o + s.forward);
            }

            if (DrawLegs)
            {
                Gizmos.DrawWireSphere(Vector3.zero, 0.01f);
                Gizmos.DrawWireSphere(leg.offset, 0.01f);
                Gizmos.DrawWireSphere(ankle, 0.01f);
                Gizmos.DrawLine(leg.offset, kp);
                Gizmos.DrawLine(kp, ankle);
            }
        }
    }
}