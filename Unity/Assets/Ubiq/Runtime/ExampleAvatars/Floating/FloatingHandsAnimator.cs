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
        [Tooltip("Speed at which the hand model will change grip strength in units/second. A speed of 2 will change from 0 (no grip) to 1 (full grip) in 0.5 seconds, for example. Set to 0 to disable smoothing")]
        public float smoothingSpeed = 4;

        private HeadAndHandsAvatar headAndHandsAvatar;
        private Animator animator;
        private float targetGrip;
        private float currentGrip;
        private static readonly int gripProperty = Animator.StringToHash("Grip");

        private void Start()
        {
            headAndHandsAvatar = GetComponentInParent<HeadAndHandsAvatar>();
            animator = GetComponent<Animator>();

            if (!headAndHandsAvatar)
            {
                Debug.LogWarning("No HeadAndHandsAvatar found among parents");
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
                headAndHandsAvatar.OnLeftGripUpdate.AddListener(OnGripUpdate);
            }
            else
            {
                headAndHandsAvatar.OnRightGripUpdate.AddListener(OnGripUpdate);
            }
        }

        private void OnGripUpdate(InputVar<float> grip)
        {
            if (!grip.valid)
            {
                targetGrip = 0;
                return;
            }
            
            targetGrip = grip.value;
        }
        
        private void Update()
        {
            if (Mathf.Approximately(currentGrip,targetGrip))
            {
                return;
            }
            var delta = smoothingSpeed * Time.deltaTime;
            currentGrip = Mathf.MoveTowards(currentGrip,targetGrip,delta);
            animator.SetFloat(gripProperty,currentGrip);
        }
    }
}
