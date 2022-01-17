using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Ubiq.Rooms;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Ubiq.Avatars;
using Ubiq.XR;

namespace Ubiq.Samples
{
    public class WristMenuInvoker : MonoBehaviour, IUseable
    {
        public MenuRequestSource source;

        public enum Wrist
        {
            Left,
            Right
        }
        public Wrist wrist;

        public void Use(Hand controller)
        {
            source.Request(gameObject);
        }

        public void UnUse(Hand controller) { }

        private void Update()
        {
            UpdatePositionAndRotation();
        }

        private void LateUpdate()
        {
            UpdatePositionAndRotation();
        }

        private void UpdatePositionAndRotation()
        {
            var node = wrist == Wrist.Left
                ? AvatarHints.NodePosRot.LeftWrist
                : AvatarHints.NodePosRot.RightWrist;
            if (AvatarHints.TryGet(node, out var positionRotation))
            {
                transform.position = positionRotation.position;
                transform.rotation = positionRotation.rotation;
            }
        }
    }
}
