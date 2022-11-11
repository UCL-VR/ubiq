using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Messaging
{
    [Serializable]
    public struct TransformMessage
    {
        public Vector3 position;
        public Quaternion rotation;

        public TransformMessage(Transform transform)
        {
            this.position = transform.localPosition;
            this.rotation = transform.localRotation;
        }
    }


}