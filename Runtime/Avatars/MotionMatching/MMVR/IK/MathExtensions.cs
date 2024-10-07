using Unity.Mathematics;

namespace Ubiq.MotionMatching.MMVR
{
    public static class MathExtensions
    {
        /// <summary>
        /// Returns the rotation between two vectors, from and to are ASSUMED to be normalized
        /// </summary>
        public static quaternion FromToRotation(this float3 from, float3 to)
        {
            float dotFT = math.dot(from, to);
            if (dotFT > 0.99999f) // cross(from, to) is zero
            {
                return quaternion.identity;
            }
            else if (dotFT < -0.99999f) // cross(from, to) is zero
            {
                float3 axis;
                if (math.abs(from.z) > 0.001f)
                {
                    axis = new float3(-from.z, 0.0f, from.x);
                }
                else
                {
                    axis = new float3(-from.y, from.x, 0.0f);
                }
                return quaternion.AxisAngle(
                    angle: math.PI,
                    axis: axis
                );
            }
            return quaternion.AxisAngle(
                angle: math.acos(math.clamp(dotFT, -1f, 1f)),
                axis: math.normalize(math.cross(from, to))
            );
        }

        /// <summary>
        /// Converts a quaternion to its axis-angle representation.
        /// </summary>
        /// <param name="q">The input quaternion.</param>
        /// <param name="axis">The axis of rotation.</param>
        /// <param name="angle">The angle of rotation.</param>
        public static (float3 axis, float angle) ToAxisAngle(this quaternion q)
        {
            float angle = 2.0f * math.acos(q.value.w);
            float s = math.sqrt(1.0f - q.value.w * q.value.w);

            float3 axis;
            if (s < 0.001f)
            {
                // If s is close to zero, this means angle is close to zero
                // In this case, axis does not matter; setting to (1, 0, 0)
                axis = new float3(1, 0, 0);
            }
            else
            {
                axis = new float3(q.value.x / s, q.value.y / s, q.value.z / s);
            }
            return (axis, angle);
        }


        /// <summary>
        /// Scales the rotation angle of a quaternion by a given weight.
        /// </summary>
        /// <param name="rot">The original quaternion.</param>
        /// <param name="weight">The weight to scale the rotation angle by.</param>
        /// <returns>The scaled quaternion.</returns>
        public static quaternion ScaleRotation(this quaternion rot, float weight)
        {
            // Convert quaternion to axis-angle representation
            (float3 axis, float angle) = rot.ToAxisAngle();

            // Scale the angle
            angle *= weight;

            // Convert back to quaternion
            quaternion scaledRot = quaternion.AxisAngle(axis, angle);
            return scaledRot;
        }

    }
}