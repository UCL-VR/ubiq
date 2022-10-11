using UnityEngine;
using Ubiq.Avatars;

namespace Ubiq.Samples
{
    // Assumptions about avatar model hierarchy, where > means 'child of':
    // CenterEye > Head > Neck > Root(Torso)
    [RequireComponent(typeof(Animator))]
    public class FloatingBodyAnimator : MonoBehaviour {

        public enum HandPositionMethod
        {
            IKHint,
            Exact
        }

        public Transform centerEye;
        public Transform baseOfNeck;

        public HandPositionMethod handPositionMethod;

        public float maxArmLength = 1.0f;
        public float elbowIKHintCutoff = 0.1f;
        public float elbowIKHintScale = 3.0f;
        public float elbowIKHintMinOffset = 0.0005f;
        public float leanContribution = .2f;

        public AnimationCurve torsoFootCurve;
        public AnimationCurve torsoFacingCurve;

        public Transform headOffsetHint;
        public Transform leftHandOffsetHint;
        public Transform rightHandOffsetHint;

        private Animator animator;
        private ThreePointTrackedAvatar trackedAvatar;

        private Transform head;
        private Transform leftUpper;
        private Transform rightUpper;
        private Transform leftHand;
        private Transform rightHand;

        private Vector3 headPos;
        private Quaternion headRot;
        private Vector3 leftHandPos;
        private Quaternion leftHandRot = Quaternion.identity;
        private Vector3 rightHandPos;
        private Quaternion rightHandRot = Quaternion.identity;
        private float leftGrip;
        private float rightGrip;

        private Vector3 footPosition;
        private Quaternion torsoFacing;

        private Vector3 headPosEyeSpace;
        private Quaternion headRotEyeSpace;
        private Vector3 neckPosEyeSpace;
        private Quaternion neckRotEyeSpace;
        private Vector3 rootPosNeckSpace;

        private Matrix4x4 leftHandTRS;
        private Matrix4x4 rightHandTRS;

        private Vector3 leftHandLocalPos;
        private Quaternion leftHandLocalRot;

        private void Start ()
        {
            animator = GetComponent<Animator>();
            trackedAvatar = GetComponentInParent<ThreePointTrackedAvatar>();
            trackedAvatar.OnHeadUpdate.AddListener(OnHeadUpdate);
            trackedAvatar.OnLeftHandUpdate.AddListener(OnLeftHandUpdate);
            trackedAvatar.OnRightHandUpdate.AddListener(OnRightHandUpdate);
            trackedAvatar.OnLeftGripUpdate.AddListener(OnLeftGripUpdate);
            trackedAvatar.OnRightGripUpdate.AddListener(OnRightGripUpdate);

            head = animator.GetBoneTransform(HumanBodyBones.Head);
            leftUpper = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            rightUpper = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

            leftHandTRS = Matrix4x4.TRS(leftHand.localPosition,leftHand.localRotation,leftHand.localScale);
            rightHandTRS = Matrix4x4.TRS(rightHand.localPosition,rightHand.localRotation,rightHand.localScale);
            leftHandLocalPos = leftHand.localPosition;
            leftHandLocalRot = leftHand.localRotation;

            headPosEyeSpace = centerEye.InverseTransformPoint(head.position);
            headRotEyeSpace = Quaternion.Inverse(centerEye.rotation) * head.rotation;
            neckPosEyeSpace = centerEye.InverseTransformPoint(baseOfNeck.position);
            neckRotEyeSpace = Quaternion.Inverse(centerEye.rotation) * baseOfNeck.rotation;
            rootPosNeckSpace = baseOfNeck.InverseTransformPoint(transform.position);
        }

        private void OnDestroy ()
        {
            if (trackedAvatar)
            {
                trackedAvatar.OnHeadUpdate.RemoveListener(OnHeadUpdate);
                trackedAvatar.OnLeftHandUpdate.RemoveListener(OnLeftHandUpdate);
                trackedAvatar.OnRightHandUpdate.RemoveListener(OnRightHandUpdate);
                trackedAvatar.OnLeftGripUpdate.RemoveListener(OnLeftGripUpdate);
                trackedAvatar.OnRightGripUpdate.RemoveListener(OnRightGripUpdate);
            }
        }

        private void OnHeadUpdate (Vector3 pos, Quaternion rot)
        {
            if (headOffsetHint)
            {
                headPos = (rot * headOffsetHint.localPosition) + pos;
                headRot = rot * headOffsetHint.localRotation;
            }
            else
            {
                headPos = pos;
                headRot = rot;
            }
        }

        private void OnLeftHandUpdate (Vector3 pos, Quaternion rot)
        {
            if (leftHandOffsetHint)
            {
                leftHandPos = (rot * leftHandOffsetHint.localPosition) + pos;
                leftHandRot = rot * leftHandOffsetHint.localRotation;
            }
            else
            {
                leftHandPos = pos;
                leftHandRot = rot;
            }
        }

        private void OnLeftGripUpdate (float leftGrip)
        {
            this.leftGrip = leftGrip;
        }

        private void OnRightHandUpdate (Vector3 pos, Quaternion rot)
        {
            if (rightHandOffsetHint)
            {
                rightHandPos = (rot * rightHandOffsetHint.localPosition) + pos;
                rightHandRot = rot * rightHandOffsetHint.localRotation;
            }
            else
            {
                rightHandPos = pos;
                rightHandRot = rot;
            }
        }

        private void OnRightGripUpdate (float rightGrip)
        {
            this.rightGrip = rightGrip;
        }

        private void OnAnimatorIKHand(Vector3 handPos, Quaternion handRot,
            Transform upper, AvatarIKHint elbow, AvatarIKGoal hand)
        {
            handPos = ApproachHandPos(upper.position,handPos);
            var relPos = upper.InverseTransformPoint(handPos);
            var y = relPos.y;
            relPos.y = Mathf.Abs(relPos.y);
            relPos.z = Mathf.Abs(relPos.z);
            var hintPos = upper.TransformPoint(relPos);
            hintPos.y = Mathf.Lerp(0,upper.position.y,
                elbowIKHintScale*(-y + elbowIKHintCutoff));

            animator.SetIKHintPosition(elbow,hintPos);
            animator.SetIKHintPositionWeight(elbow,1);
            animator.SetIKPosition(hand,handPos);
            animator.SetIKRotation(hand,handRot);
            animator.SetIKPositionWeight(hand,1);
            animator.SetIKRotationWeight(hand,1);
        }

        // A callback for calculating IK
        private void OnAnimatorIK()
        {
            OnAnimatorIKHand(leftHandPos,leftHandRot,leftUpper,
                AvatarIKHint.LeftElbow,AvatarIKGoal.LeftHand);
            OnAnimatorIKHand(rightHandPos,rightHandRot,rightUpper,
                AvatarIKHint.RightElbow,AvatarIKGoal.RightHand);
        }

        private void LateUpdate()
        {
            centerEye.position = headPos;
            centerEye.rotation = headRot;

            var neckPos = centerEye.TransformPoint(neckPosEyeSpace);
            var neckRot = centerEye.rotation * neckRotEyeSpace;

            baseOfNeck.position = neckPos;
            baseOfNeck.rotation = neckRot;

            // Reset center-eye position, because it's probably a child of neck
            centerEye.position = headPos;
            centerEye.rotation = headRot;

            // Calculate virtual 'foot' pos, just for anim, wildly inaccurate :)
            var downFootPos = baseOfNeck.position;
            downFootPos.y = 0;
            var t = baseOfNeck.position.y / baseOfNeck.up.y;
            var leanFootPos = baseOfNeck.position + t * -baseOfNeck.up;

            var targetFootPos = Vector3.Lerp(downFootPos,leanFootPos,leanContribution);
            footPosition.x += (targetFootPos.x - footPosition.x) * Time.deltaTime * torsoFootCurve.Evaluate(Mathf.Abs(targetFootPos.x - footPosition.x));
            footPosition.z += (targetFootPos.z - footPosition.z) * Time.deltaTime * torsoFootCurve.Evaluate(Mathf.Abs(targetFootPos.z - footPosition.z));
            footPosition.y = 0;

            // Forward direction of torso is vector in the transverse plane
            // Determined by head direction primarily
            var eyeFwd = new Vector3(centerEye.forward.x,0,centerEye.forward.z);
            var bodyRot = Quaternion.LookRotation(eyeFwd,Vector3.up);
            var angle = Quaternion.Angle(torsoFacing, bodyRot);
            var rotateAngle = Mathf.Clamp(Time.deltaTime * torsoFacingCurve.Evaluate(Mathf.Abs(angle)), 0, angle);
            torsoFacing = Quaternion.RotateTowards(torsoFacing, bodyRot, rotateAngle);

            // Place torso so it makes a straight line between neck and feet
            transform.position = baseOfNeck.TransformPoint(rootPosNeckSpace);
            transform.rotation = Quaternion.FromToRotation(
                Vector3.down, footPosition - baseOfNeck.position) * torsoFacing;

            // Re-set neck and head position/rotations
            // They are likely children of torso so will have been moved
            baseOfNeck.position = neckPos;
            baseOfNeck.rotation = neckRot;
            head.position = centerEye.TransformPoint(headPosEyeSpace);
            head.rotation = centerEye.rotation * headRotEyeSpace;

            if (handPositionMethod == HandPositionMethod.Exact)
            {
                var leftTRS = Matrix4x4.TRS(leftHandPos,leftHandRot,Vector3.one);
                leftHand.position = (leftTRS * leftHandTRS).MultiplyPoint3x4(Vector3.zero);
                leftHand.rotation = (leftTRS * leftHandTRS).rotation;
                var rightTRS = Matrix4x4.TRS(rightHandPos,rightHandRot,Vector3.one);
                rightHand.position = (rightTRS * rightHandTRS).MultiplyPoint3x4(Vector3.zero);
                rightHand.rotation = (rightTRS * rightHandTRS).rotation;
            }
        }

        private Vector3 ApproachHandPos (Vector3 shoulderPos, Vector3 handPos)
        {
            // A bit of magic to make hands approach their real position
            // but never quite reach it. Helps to animate the model better
            // with Unity's builtin simple 2-joint IK
            var arm = handPos - shoulderPos;
            var len = maxArmLength * Mathf.Log10(5*arm.magnitude + 1);
            return shoulderPos + arm.normalized * len;
        }
    }
}