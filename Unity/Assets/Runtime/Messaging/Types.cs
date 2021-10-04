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

        public TransformMessage(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        public TransformMessage(Vector3 position, Vector3 eulerAngles)
        {
            this.position = position;
            this.rotation = Quaternion.Euler(eulerAngles);
        }
    }


}