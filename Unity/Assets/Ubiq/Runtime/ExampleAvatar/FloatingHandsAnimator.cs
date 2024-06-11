using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.Avatars;

namespace Ubiq.Samples
{
    public class FloatingHandsAnimator : MonoBehaviour
    {
        public enum Side
        {
            Left,
            Right
        }

        public Side side;

        private ThreePointTrackedAvatar trackedAvatar;
        private Animator animator;

        private void Start()
        {
            trackedAvatar = GetComponentInParent<ThreePointTrackedAvatar>();
            animator = GetComponent<Animator>();

            if (!trackedAvatar)
            {
                Debug.LogWarning("No ThreePointTrackedAvatar found among parents");
                enabled = false;
                return;
            }

            if (!animator)
            {
                Debug.LogWarning("No Animator found on this GameObject");
                enabled = false;
                return;
            }

            if (side == Side.Left)
            {
                trackedAvatar.OnLeftGripUpdate.AddListener(OnGripUpdate);
            }
            else
            {
                trackedAvatar.OnRightGripUpdate.AddListener(OnGripUpdate);
            }
        }

        private void OnGripUpdate(float grip)
        {
            animator.SetFloat("Grip",grip);
        }
    }
}
