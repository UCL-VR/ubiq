using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;

namespace Ubiq.Samples
{
    public class FollowGraspable : MonoBehaviour, IGraspable
    {
        private Vector3 localGrabPoint;
        private Quaternion localGrabRotation;
        private Quaternion grabHandRotation;
        private Transform follow;

        public bool IsGrapsed { get; protected set; }

        public void Grasp(Hand controller)
        {
            var handTransform = controller.transform;
            localGrabPoint = handTransform.InverseTransformPoint(transform.position); //transform.InverseTransformPoint(handTransform.position);
            localGrabRotation = Quaternion.Inverse(handTransform.rotation) * transform.rotation;
            grabHandRotation = handTransform.rotation;
            follow = handTransform;
            IsGrapsed = true;
        }

        public void Release(Hand controller)
        {
            follow = null;
            IsGrapsed = false;
        }

        private void Update()
        {
            if (follow)
            {
                transform.rotation = follow.rotation * localGrabRotation;
                transform.position = follow.TransformPoint(localGrabPoint);
            }
        }
    }
}
