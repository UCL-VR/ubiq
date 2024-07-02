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

        private ThreePointTrackedAvatar trackedAvatar;
        private Animator animator;
        private float targetGrip;
        private float currentGrip;

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
            animator.SetFloat("Grip",currentGrip);
        }
    }
}
