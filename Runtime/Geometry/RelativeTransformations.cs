using UnityEngine;

namespace Ubiq.Geometry
{
    [System.Serializable]
    public struct PositionRotation
    {
        public Vector3 position;
        public Quaternion rotation;

        public static PositionRotation identity
        {
            get
            {
                return new PositionRotation
                {
                    position = Vector3.zero,
                    rotation = Quaternion.identity
                };
            }
        }

        public PositionRotation(Transform transform)
        {
            this.position = transform.localPosition;
            this.rotation = transform.localRotation;
        }
    }

    public static class Transforms
    {
        public static PositionRotation ToLocal(Transform world, Transform relativeTo)
        {
            return ToLocal(world.position,world.rotation,relativeTo);
        }

        public static PositionRotation ToLocal(PositionRotation world, Transform relativeTo)
        {
            return ToLocal(world.position,world.rotation,relativeTo);
        }

        public static PositionRotation ToLocal(Vector3 worldPos, Quaternion worldRot, Transform relativeTo)
        {
            return new PositionRotation {
                position = relativeTo.InverseTransformPoint(worldPos),
                rotation = Quaternion.Inverse(relativeTo.rotation) * worldRot
            };
        }

        public static PositionRotation ToWorld(Transform local,Transform relativeTo)
        {
            return ToWorld(local.position,local.rotation,relativeTo);
        }

        public static PositionRotation ToWorld(PositionRotation local,Transform relativeTo)
        {
            return ToWorld(local.position,local.rotation,relativeTo);
        }

        public static PositionRotation ToWorld(Vector3 localPos, Quaternion localRot, Transform relativeTo)
        {
            return new PositionRotation {
                position = relativeTo.TransformPoint(localPos),
                rotation = relativeTo.rotation * localRot
            };
        }
    }

}