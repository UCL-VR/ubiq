using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ubiq.XR.IK
{
    [RequireComponent(typeof(Animator))]
    public class PlayerIKController : MonoBehaviour
    {
        public XRPlayerController player;
        public Transform avatarHead;
        protected Animator animator;

        protected Transform left;
        protected Transform right;
        protected Transform head;

        void Start()
        {
            animator = GetComponent<Animator>();
            left = player.handControllers.Where(hc => hc.Left).First().transform;
            right = player.handControllers.Where(hc => hc.Right).First().transform;
            head = player.headCamera.transform;
        }

        private void Update()
        {
            transform.position += head.position - avatarHead.position;
        }

        //a callback for calculating IK
        void OnAnimatorIK()
        {
            animator.SetLookAtWeight(1);
            animator.SetLookAtPosition(head.position + head.forward);

            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.RightHand, 1);
            animator.SetIKPosition(AvatarIKGoal.RightHand, right.position);
            animator.SetIKRotation(AvatarIKGoal.RightHand, right.rotation);

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, left.position);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, left.rotation);
        }
    }
}