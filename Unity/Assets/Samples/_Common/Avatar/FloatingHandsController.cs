using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Avatars
{
    public class FloatingHandsController : MonoBehaviour
    {
        public Transform leftHand;
        public Transform rightHand;

        public Transform leftHandOffsetHint;
        public Transform rightHandOffsetHint;

        private ThreePointTrackedAvatar trackedAvatar;

        private void Start ()
        {
            trackedAvatar = GetComponentInParent<ThreePointTrackedAvatar>();
            trackedAvatar.OnLeftHandUpdate.AddListener(OnLeftHandUpdate);
            trackedAvatar.OnRightHandUpdate.AddListener(OnRightHandUpdate);
        }

        private void OnDestroy ()
        {
            if (trackedAvatar)
            {
                trackedAvatar.OnLeftHandUpdate.RemoveListener(OnLeftHandUpdate);
                trackedAvatar.OnRightHandUpdate.RemoveListener(OnRightHandUpdate);
            }
        }

        private void OnLeftHandUpdate (Vector3 pos, Quaternion rot)
        {
            if (leftHandOffsetHint)
            {
                leftHand.position = (rot * leftHandOffsetHint.localPosition) + pos;
                leftHand.rotation = rot * leftHandOffsetHint.localRotation;
            }
            else
            {
                leftHand.position = pos;
                leftHand.rotation = rot;
            }
        }

        private void OnRightHandUpdate (Vector3 pos, Quaternion rot)
        {
            if (rightHandOffsetHint)
            {
                rightHand.position = (rot * rightHandOffsetHint.localPosition) + pos;
                rightHand.rotation = rot * rightHandOffsetHint.localRotation;
            }
            else
            {
                rightHand.position = pos;
                rightHand.rotation = rot;
            }
        }
    }
}