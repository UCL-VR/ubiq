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

        private class HandAnimation
        {
            public enum Side
            {
                Left,
                Right
            }

            private Transform indexProximal;
            private Transform indexIntermediate;
            private Transform thumbProximal;
            private Transform thumbIntermediate;

            // private Vector3 indexProximalOriginal;
            // private Vector3 indexIntermediateOriginal;
            // private Vector3 thumbProximalOriginal;
            // private Vector3 thumbIntermediateOriginal;
            private Quaternion indexProximalOriginal;
            private Quaternion indexIntermediateOriginal;
            private Quaternion thumbProximalOriginal;
            private Quaternion thumbIntermediateOriginal;

            private Quaternion indexProximalTarget;
            private Quaternion indexIntermediateTarget;
            private Quaternion thumbProximalTarget;
            private Quaternion thumbIntermediateTarget;

            public HandAnimation (Animator animator, Side side,
                Vector3 indexProximalTarget, Vector3 indexIntermediateTarget,
                Vector3 thumbProximalTarget, Vector3 thumbIntermediateTarget)
            {
                if (side == Side.Left)
                {
                    indexProximal = animator.GetBoneTransform(HumanBodyBones.LeftIndexProximal);
                    indexIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftIndexIntermediate);
                    thumbProximal = animator.GetBoneTransform(HumanBodyBones.LeftThumbProximal);
                    thumbIntermediate = animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
                }
                else
                {
                    indexProximal = animator.GetBoneTransform(HumanBodyBones.RightIndexProximal);
                    indexIntermediate = animator.GetBoneTransform(HumanBodyBones.RightIndexIntermediate);
                    thumbProximal = animator.GetBoneTransform(HumanBodyBones.RightThumbProximal);
                    thumbIntermediate = animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
                }

                if (indexProximal)
                {
                    indexProximalOriginal = Quaternion.Euler(indexProximal.localEulerAngles);
                }
                if (indexIntermediate)
                {
                    indexIntermediateOriginal = Quaternion.Euler(indexIntermediate.localEulerAngles);
                }
                if (thumbProximal)
                {
                    thumbProximalOriginal = Quaternion.Euler(thumbProximal.localEulerAngles);
                }
                if (thumbIntermediate)
                {
                    thumbIntermediateOriginal = Quaternion.Euler(thumbIntermediate.localEulerAngles);
                }

                this.indexProximalTarget = Quaternion.Euler(indexProximalTarget);
                this.indexIntermediateTarget = Quaternion.Euler(indexIntermediateTarget);
                this.thumbProximalTarget = Quaternion.Euler(thumbProximalTarget);
                this.thumbIntermediateTarget = Quaternion.Euler(thumbIntermediateTarget);
            }

            public void UpdateAnim (float value)
            {
                if (indexProximal)
                {
                    indexProximal.localRotation = Quaternion.Lerp(
                        indexProximalOriginal,indexProximalTarget,value);
                }
                if (indexIntermediate)
                {
                    indexIntermediate.localRotation = Quaternion.Lerp(
                        indexIntermediateOriginal,indexIntermediateTarget,value);
                }
                if (thumbProximal)
                {
                    thumbProximal.localRotation = Quaternion.Lerp(
                        thumbProximalOriginal,thumbProximalTarget,value);
                }
                if (thumbIntermediate)
                {
                    thumbIntermediate.localRotation = Quaternion.Lerp(
                        thumbIntermediateOriginal,thumbIntermediateTarget,value);
                }
            }
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

        public Vector3 indexProximalTarget;
        public Vector3 indexIntermediateTarget;
        public Vector3 thumbProximalTarget;
        public Vector3 thumbIntermediateTarget;

        public Vector3 leftIndexProximalTarget;
        public Vector3 leftIndexIntermediateTarget;
        public Vector3 leftThumbProximalTarget;
        public Vector3 leftThumbIntermediateTarget;

        public Vector3 rightIndexProximalTarget;
        public Vector3 rightIndexIntermediateTarget;
        public Vector3 rightThumbProximalTarget;
        public Vector3 rightThumbIntermediateTarget;

        private Animator animator;
        private ThreePointTrackedAvatar trackedAvatar;

        private Transform head;
        private Transform leftUpper;
        private Transform rightUpper;
        private Transform leftHand;
        private Transform rightHand;
        private HandAnimation leftHandAnim;
        private HandAnimation rightHandAnim;

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

            leftHandAnim = new HandAnimation(animator,HandAnimation.Side.Left,
                leftIndexProximalTarget,leftIndexIntermediateTarget,
                leftThumbProximalTarget,leftThumbIntermediateTarget);
            rightHandAnim = new HandAnimation(animator,HandAnimation.Side.Right,
                rightIndexProximalTarget,rightIndexIntermediateTarget,
                rightThumbProximalTarget,rightThumbIntermediateTarget);

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
            // if (leftHandOffsetHint)
            // {
                // leftHandPos = (rot * leftHandOffsetHint.localPosition) + pos;
                // leftHandRot = rot * leftHandOffsetHint.localRotation;
            // }
            // else
            // {
                leftHandPos = pos;
                leftHandRot = rot;
            // }
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
                // var leftTRS = Matrix4x4.TRS(leftHandPos,leftHandRot,Vector3.one);
                leftHand.position = leftHandPos;// (leftTRS * leftHandTRS).MultiplyPoint3x4(Vector3.zero);
                // leftHand.rotation = leftHandRot; //(leftTRS * leftHandTRS).rotation;
                // leftHand.localPosition += leftHandLocalPos;
                // leftHand.position = leftHand.TransformPoint(leftHandOffsetHint.localPosition);
                leftHand.rotation = leftHandRot * leftHandOffsetHint.localRotation;
                // leftHand.localPosition += leftHandOffsetHint.localPosition;
                // leftHand.localRotation = leftHandOffsetHint.localRotation * leftHand.localRotation;
                // leftHand.localRotation = leftHandLocalRot * leftHand.localRotation;
                var rightTRS = Matrix4x4.TRS(rightHandPos,rightHandRot,Vector3.one);
                rightHand.position = (rightTRS * rightHandTRS).MultiplyPoint3x4(Vector3.zero);
                rightHand.rotation = (rightTRS * rightHandTRS).rotation;
            }

            leftHandAnim.UpdateAnim(leftGrip);
            rightHandAnim.UpdateAnim(rightGrip);
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