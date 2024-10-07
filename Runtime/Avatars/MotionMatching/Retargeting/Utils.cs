using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.MotionMatching
{
    public static class Utils
    {
        public static Circle SphereSphereIntersection(Vector3 A, Vector3 B, float R, float r)
        {
            // This is the problem of a sphere sphere intersection, which we solve
            // as a distance along the vector b-a, and radius of a circle normal
            // to the vector at that point.

            Circle circle;

            var AB = B - A;
            var d = AB.magnitude;
            var x = ((d * d) - (r * r) + (R * R)) / (2 * d);

            circle.normal = AB.normalized;
            circle.d = x;

            circle.radius = 0;

            var b = 4 * d * d * R * R - Mathf.Pow(d * d - r * r + R * R, 2);
            if (b > 0)
            {
                var a = (1 / (2 * d)) * Mathf.Sqrt(b);
                circle.radius = a;
            }

            return circle;
        }
    }
}