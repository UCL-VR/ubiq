using UnityEngine;

namespace Ubiq.Geometry
{
    public static class Transforms
    {
        public static Pose ToLocal(Transform world, Transform relativeTo)
        {
            return ToLocal(world.position,world.rotation,relativeTo);
        }

        public static Pose ToLocal(Pose world, Transform relativeTo)
        {
            return ToLocal(world.position,world.rotation,relativeTo);
        }

        public static Pose ToLocal(Vector3 worldPos, Quaternion worldRot, Transform relativeTo)
        {
            return new Pose (
                position: relativeTo.InverseTransformPoint(worldPos),
                rotation: Quaternion.Inverse(relativeTo.rotation) * worldRot
            );
        }

        public static Pose ToWorld(Transform local,Transform relativeTo)
        {
            return ToWorld(local.position,local.rotation,relativeTo);
        }

        public static Pose ToWorld(Pose local,Transform relativeTo)
        {
            return ToWorld(local.position,local.rotation,relativeTo);
        }

        public static Pose ToWorld(Vector3 localPos, Quaternion localRot, Transform relativeTo)
        {
            return new Pose (
                position: relativeTo.TransformPoint(localPos),
                rotation: relativeTo.rotation * localRot
            );
        }
    }

}