using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ubiq.Geometry
{
    public class Query
    {
        public struct Line
        {
            public Vector3 start;
            public Vector3 end;

            public Line(Vector3 start, Vector3 end)
            {
                this.start = start;
                this.end = end;
            }

            public static Line zero
            {
                get
                {
                    return new Line(Vector3.zero, Vector3.zero);
                }
            }

            public Vector3 d
            {
                get
                {
                    return end - start;
                }
            }

            public float magnitude
            {
                get { return (end - start).magnitude; }
            }

            public Vector3 center
            {
                get
                {
                    return (end + start) / 2f;
                }
            }
        }

        /// <summary>
        /// Returns a line that links the Line and Ray at the closest points between them. The start of the result Line is the point on the Line, the end is the point on the Ray.
        /// </summary>
        public static Line ClosestPointLineRay(Line line, Ray ray)
        {
            // David Eberly, Geometric Tools, Redmond WA 98052
            // Copyright (c) 1998-2018
            // Distributed under the Boost Software License, Version 1.0.
            // http://www.boost.org/LICENSE_1_0.txt
            // http://www.geometrictools.com/License/Boost/LICENSE_1_0.txt
            // File Version: 3.0.0 (2016/06/19)

            // https://www.geometrictools.com/Source/Distance3D.html

            Line result;

            var diff = line.start - ray.origin;
            var d = (line.end - line.start).normalized;
            var a01 = -Vector3.Dot(d, ray.direction);
            var b0 = Vector3.Dot(diff, d);
            var s0 = 0f;
            var s1 = 0f;

            if (Mathf.Abs(a01) < 1f)
            {
                var b1 = -Vector3.Dot(diff, ray.direction);
                s1 = a01 * b0 - b1;

                if (s1 >= 0f)
                {
                    var det = 1f - a01 * a01;
                    s0 = (a01 * b1 - b0) / det;
                    s1 /= det;
                }
                else
                {
                    s0 = -b0;
                    s1 = 0f;
                }
            }
            else
            {
                s0 = -b0;
                s1 = 0f;
            }

            s0 = Mathf.Clamp(s0, 0f, (line.end - line.start).magnitude);

            result.start = line.start + s0 * d;
            result.end = ray.origin + s1 * ray.direction;

            return result;
        }

        /// <summary>
        /// Returns a line that links two Rays at the closest points between them. The start of the result Line is the point on Ray 1, the end is the point on Ray 2.
        /// </summary>
        public static Line ClosestPointRayRay(Ray ray0, Ray ray1)
        {
            // David Eberly, Geometric Tools, Redmond WA 98052
            // Copyright (c) 1998-2018
            // Distributed under the Boost Software License, Version 1.0.
            // http://www.boost.org/LICENSE_1_0.txt
            // http://www.geometrictools.com/License/Boost/LICENSE_1_0.txt
            // File Version: 3.0.0 (2016/06/19)

            // https://www.geometrictools.com/Source/Distance3D.html

            Line result;

            var diff = ray0.origin - ray1.origin;
            var a01 = -Vector3.Dot(ray0.direction, ray1.direction);
            var b0 = Vector3.Dot(diff, ray0.direction);
            var b1 = 0f;
            var s0 = 0f;
            var s1 = 0f;

            if (Mathf.Abs(a01) < 1f)
            {
                // Rays are not parallel.
                b1 = -Vector3.Dot(diff, ray1.direction);
                s0 = a01 * b1 - b0;
                s1 = a01 * b0 - b1;

                if (s0 >= 0f)
                {
                    if (s1 >= 0f)  // region 0 (interior)
                    {
                        // Minimum at two interior points of rays.
                        var det = 1 - a01 * a01;
                        s0 /= det;
                        s1 /= det;
                    }
                    else  // region 3 (side)
                    {
                        s1 = 0f;
                        if (b0 >= 0f)
                        {
                            s0 = 0f;
                        }
                        else
                        {
                            s0 = -b0;
                        }
                    }
                }
                else
                {
                    if (s1 >= 0f)  // region 1 (side)
                    {
                        s0 = 0f;
                        if (b1 >= 0f)
                        {
                            s1 = 0f;
                        }
                        else
                        {
                            s1 = -b1;
                        }
                    }
                    else  // region 2 (corner)
                    {
                        if (b0 < 0f)
                        {
                            s0 = -b0;
                            s1 = 0f;
                        }
                        else
                        {
                            s0 = 0f;
                            if (b1 >= 0f)
                            {
                                s1 = 0f;
                            }
                            else
                            {
                                s1 = -b1;
                            }
                        }
                    }
                }
            }
            else
            {
                // Rays are parallel.
                if (a01 > 0f)
                {
                    // Opposite direction vectors.
                    s1 = 0f;
                    if (b0 >= 0f)
                    {
                        s0 = 0f;
                    }
                    else
                    {
                        s0 = -b0;
                    }
                }
                else
                {
                    // Same direction vectors.
                    if (b0 >= 0f)
                    {
                        b1 = -Vector3.Dot(diff, ray1.direction);
                        s0 = 0f;
                        s1 = -b1;
                    }
                    else
                    {
                        s0 = -b0;
                        s1 = 0f;
                    }
                }
            }

            result.start = ray0.origin + s0 * ray0.direction;
            result.end = ray1.origin + s1 * ray1.direction;

            return result;
        }

        public static Line ClosestPointLineRay(Ray line, Ray ray)
        {
            // David Eberly, Geometric Tools, Redmond WA 98052
            // Copyright (c) 1998-2018
            // Distributed under the Boost Software License, Version 1.0.
            // http://www.boost.org/LICENSE_1_0.txt
            // http://www.geometrictools.com/License/Boost/LICENSE_1_0.txt
            // File Version: 3.0.0 (2016/06/19)

            // https://www.geometrictools.com/Source/Distance3D.html

            Line result;

            var diff = line.origin - ray.origin;
            var a01 = -Vector3.Dot(line.direction, ray.direction);
            var b0 = Vector3.Dot(diff, line.direction);
            var s0 = 0f;
            var s1 = 0f;

            if (Mathf.Abs(a01) < 1f)
            {
                var b1 = -Vector3.Dot(diff, ray.direction);
                s1 = a01 * b0 - b1;

                if (s1 >= 0f)
                {
                    // Two interior points are closest, one on line and one on ray.
                    var det = 1f - a01 * a01;
                    s0 = (a01 * b1 - b0) / det;
                    s1 /= det;
                }
                else
                {
                    // Origin of ray and interior point of line are closest.
                    s0 = -b0;
                    s1 = 0f;
                }
            }
            else
            {
                // Lines are parallel, closest pair with one point at ray origin.
                s0 = -b0;
                s1 = 0f;
            }

            result.start = line.origin + s0 * line.direction;
            result.end = ray.origin + s1 * ray.direction;
            return result;
        }

        public static Line ClosestPointLineLine(Line A, Line B)
        {
            // Ericson, C. (2004). Real-Time Collision Detection. CRC Press.

            var d1 = A.d;
            var d2 = B.d;
            var r = A.start - B.start;
            var a = Vector3.Dot(d1, d1);
            var e = Vector3.Dot(d2, d2);
            var f = Vector3.Dot(d2, r);

            float s = 0;
            float t = 0;

            // check for degenerations
            if (a <= Mathf.Epsilon && e <= Mathf.Epsilon)
            {
                s = 0;
                t = 0;
            }
            else
            {
                if (a <= Mathf.Epsilon)
                {
                    s = 0f;
                    t = Mathf.Clamp(f / e, 0, 1);
                }
                else
                {
                    var c = Vector3.Dot(d1, r);
                    if (e <= Mathf.Epsilon)
                    {
                        t = 0;
                        s = Mathf.Clamp(-c / a, 0, 1);
                    }
                    else
                    {
                        var b = Vector3.Dot(d1, d2);
                        var denom = a * e - b * b;

                        if (denom != 0)
                        {
                            s = Mathf.Clamp((b * f - c * e) / denom, 0, 1);
                        }
                        else
                        {
                            s = 0f;
                        }

                        t = (b * s + f) / e;

                        if (t < 0)
                        {
                            t = 0;
                            s = Mathf.Clamp(-c / a, 0, 1);
                        }
                        else
                        if (t > 1)
                        {
                            t = 1;
                            s = Mathf.Clamp((b - c) / a, 0, 1);
                        }
                    }
                }
            }

            Line result;
            result.start = A.start + d1 * s;
            result.end = B.start + d2 * t;
            return result;
        }

        public static Line ClosestPointPointTriangle(Vector3 p, Triangle T)
        {
            return new Line()
            {
                start = p,
                end = ClosestPointTriangle(p, T)
            };
        }

        public static Vector3 ClosestPointTriangle(Vector3 p, Triangle T)
        {
            // Ericson, C. (2004). Real-Time Collision Detection.CRC Press.

            var ab = T.b - T.a;
            var ac = T.c - T.a;
            var ap = p - T.a;

            var d1 = Vector3.Dot(ab, ap);
            var d2 = Vector3.Dot(ac, ap);

            if (d1 <= 0 && d2 <= 0)
            {
                return T.a;
            }

            var bp = p - T.b;
            var d3 = Vector3.Dot(ab, bp);
            var d4 = Vector3.Dot(ac, bp);

            if (d3 >= 0 && d4 <= d3)
            {
                return T.b;
            }

            var vc = d1 * d4 - d3 * d2;
            if (vc <= 0 && d1 >= 0 && d3 <= 0)
            {
                return T.a + (d1 / (d1 - d3)) * ab;
            }

            var cp = p - T.c;
            var d5 = Vector3.Dot(ab, cp);
            var d6 = Vector3.Dot(ac, cp);

            if (d6 >= 0 && d5 <= d6)
            {
                return T.c;
            }

            var vb = d5 * d2 - d1 * d6;
            if (vb <= 0 && d2 >= 0 && d6 <= 0)
            {
                return T.a + (d2 / (d2 - d6)) * ac;
            }

            var va = d3 * d6 - d5 * d4;
            if (va <= 0 && (d4 - d3) >= 0 && (d5 - d6) >= 0)
            {
                return T.b + (d4 - d3) / ((d4 - d3) + (d5 - d6)) * (T.c - T.b);
            }

            var denom = 1 / (va + vb + vc);
            var v = vb * denom;
            var w = vc * denom;
            return T.a + ab * v + ac * w;
        }

        public static Nullable<Ray> PlanePlaneIntersection(Plane plane0, Plane plane1)
        {
            // David Eberly, Geometric Tools, Redmond WA 98052
            // Copyright (c) 1998-2018
            // Distributed under the Boost Software License, Version 1.0.
            // http://www.boost.org/LICENSE_1_0.txt
            // http://www.geometrictools.com/License/Boost/LICENSE_1_0.txt
            // File Version: 3.0.0 (2016/06/19)

            // https://www.geometrictools.com/GTEngine/Include/Mathematics/GteIntrPlane3Plane3.h

            // remember unity defines the constant as the distance *to* the origin alonge the normal (opposite of everyone else)
            plane0.distance = -plane0.distance;
            plane1.distance = -plane1.distance;

            float dot = Vector3.Dot(plane0.normal, plane1.normal);
            if (Mathf.Abs(dot) >= 1f)
            {
                // The planes are parallel.  Check if they are coplanar.
                float cDiff;
                if (dot >= 0f)
                {
                    // Normals are in same direction, need to look at c0-c1.
                    cDiff = plane0.distance - plane1.distance;
                }
                else
                {
                    // Normals are in opposite directions, need to look at c0+c1.
                    cDiff = plane0.distance + plane1.distance;
                }

                if (Mathf.Abs(cDiff) == 0f)
                {
                    // The planes are coplanar.
                    return null;
                }

                // The planes are parallel but distinct.
                return null;
            }

            float invDet = (1f) / (1f - dot * dot);
            float c0 = (plane0.distance - dot * plane1.distance) * invDet;
            float c1 = (plane1.distance - dot * plane0.distance) * invDet;
            var origin = c0 * plane0.normal + c1 * plane1.normal;
            var direction = Vector3.Cross(plane0.normal, plane1.normal).normalized;

            return new Ray(origin, direction);
        }

        public static float? RayPlaneIntersection(Ray ray, Plane plane)
        {
            float d = 0f;
            bool flag = plane.Raycast(ray, out d);

            if (flag == false && d == 0f)
            {
                return null;
            }

            return d;
        }

        /// <summary>
        /// Cuts the line with the plane and returns the new line. The line is clipped on the backside of the plane.
        /// </summary>
        /// <param name="line"></param>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static Line LinePlaneIntersection(Line line, Plane plane)
        {
            var v0 = line.start;
            var v1 = line.end;
            var d = -plane.distance;
            var p = plane.normal * d;
            var dir = (v1 - v0).normalized;

            // check if the line is parallel to the plane. if so it can either be above, or clipped entirely

            float vn = Vector3.Dot(dir, plane.normal);

            if (Mathf.Abs(vn) <= Mathf.Epsilon)
            {
                var distance = Vector3.Dot(v0 - p, plane.normal);

                if (distance < Mathf.Epsilon)  // line is parallel, so check which side of the plane its on
                {
                    return Line.zero;   // behind plane
                }
                else
                {
                    return line;
                }
            }

            if (!plane.GetSide(v0) && !plane.GetSide(v1))
            {
                return Line.zero;
            }

            if (!plane.GetSide(v0))
            {
                v0 = v0 + dir * (Vector3.Dot(p - v0, plane.normal) / vn);
            }

            if (!plane.GetSide(v1))
            {
                v1 = v1 + dir * (Vector3.Dot(p - v1, plane.normal) / vn);
            }

            return new Line(v0, v1);
        }

        /// <summary>
        /// Tests if the Ray Intersects the Triangle and returns the Distance along the Ray to the Intersection Point if so, otherwise returns Positive Infinity.
        /// If the Ray is pointing away from the Triangle, the Distance is Negative. The test works on both forward and backward facing triangles.
        /// </summary>
        public static float RayTriangleIntersection(Vector3 a, Vector3 b, Vector3 c, Ray r)
        {
            // Wikipedia contributors. (2020, January 3). Möller–Trumbore intersection algorithm.
            // In Wikipedia, The Free Encyclopedia.
            // Retrieved 18:13, January 20, 2020, from https://en.wikipedia.org/w/index.php?title=M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm&oldid=933902347

            var e0 = b - a;
            var e1 = c - a;

            var h = Vector3.Cross(r.direction, e1);
            var d = Vector3.Dot(e0, h);
            if (d > -Mathf.Epsilon && d < Mathf.Epsilon)
            {
                return float.PositiveInfinity;    // This ray is parallel to this triangle.
            }

            var f = 1.0 / d;
            var s = r.origin - a;
            var u = f * Vector3.Dot(s, h);
            if (u < 0.0 || u > 1.0)
            {
                return float.PositiveInfinity;
            }

            var q = Vector3.Cross(s, e0);
            var v = f * Vector3.Dot(r.direction, q);
            if (v < 0.0 || u + v > 1.0)
            {
                return float.PositiveInfinity;
            }

            // At this stage we can compute t to find out where the intersection point is on the line.
            var t = f * Vector3.Dot(e1, q);

            return (float)t;
        }

        public static bool PointInTriangle(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
        {
            // Point in triangle test. Blackpawn.
            // https://blackpawn.com/texts/pointinpoly/default.html

            // Compute vectors
            var v0 = c - a;
            var v1 = b - a;
            var v2 = p - a;

            // Compute dot products
            var dot00 = Vector3.Dot(v0, v0);
            var dot01 = Vector3.Dot(v0, v1);
            var dot02 = Vector3.Dot(v0, v2);
            var dot11 = Vector3.Dot(v1, v1);
            var dot12 = Vector3.Dot(v1, v2);

            // Compute barycentric coordinates
            var invDenom = 1 / (dot00 * dot11 - dot01 * dot01);
            var u = (dot11 * dot02 - dot01 * dot12) * invDenom;
            var v = (dot00 * dot12 - dot01 * dot02) * invDenom;

            // Check if point is in triangle
            return (u >= 0) && (v >= 0) && (u + v < 1);
        }

        public static float PointLineSide(Vector2 a, Vector2 b, Vector2 x)
        {
            //https://stackoverflow.com/questions/1560492/how-to-tell-whether-a-point-is-to-the-right-or-left-side-of-a-line

            return Mathf.Sign((b.x - a.x) * (x.y - a.y) - (b.y - a.y) * (x.x - a.x));
        }

        public static float TriangleArea(Vector3 v0, Vector3 v1, Vector3 v2, bool verbose = false)
        {
            //http://james-ramsden.com/area-of-a-triangle-in-3d-c-code/
            //https://www.iquilezles.org/blog/?p=1579

            var a = v1 - v0;
            var b = v2 - v1;
            var c = v0 - v2;

            double la = a.sqrMagnitude;
            double lb = b.sqrMagnitude;
            double lc = c.sqrMagnitude;

            var lalb = la * lb;
            var lblc = lb * lc;
            var lcla = lc * la;
            var lala = la * la;
            var lblb = lb * lb;
            var lclc = lc * lc;
            var top = (2 * lalb + 2 * lblc + 2 * lcla - lala - lblb - lclc);

            var A2 = top / 16;

            return Mathf.Sqrt((float)A2);
        }

        public struct Triangle
        {
            public Vector3 a;
            public Vector3 b;
            public Vector3 c;
        }

        public static Line ClosestPointTriangleTriangle(Triangle A, Triangle B)
        {
            // Ericson, C. (2004). Real-Time Collision Detection. CRC Press.

            var Ea1 = new Line(A.a, A.b);
            var Ea2 = new Line(A.b, A.c);
            var Ea3 = new Line(A.c, A.a);
            var Eb1 = new Line(B.a, B.b);
            var Eb2 = new Line(B.b, B.c);
            var Eb3 = new Line(B.c, B.a);

            Line[] results = new Line[15];

            results[0] = ClosestPointLineLine(Ea1, Eb1);
            results[1] = ClosestPointLineLine(Ea1, Eb2);
            results[2] = ClosestPointLineLine(Ea1, Eb3);
            results[3] = ClosestPointLineLine(Ea2, Eb1);
            results[4] = ClosestPointLineLine(Ea2, Eb2);
            results[5] = ClosestPointLineLine(Ea2, Eb3);
            results[6] = ClosestPointLineLine(Ea3, Eb1);
            results[7] = ClosestPointLineLine(Ea3, Eb2);
            results[8] = ClosestPointLineLine(Ea3, Eb3);

            results[9] = ClosestPointPointTriangle(A.a, B);
            results[10] = ClosestPointPointTriangle(A.b, B);
            results[11] = ClosestPointPointTriangle(A.c, B);
            results[12] = ClosestPointPointTriangle(B.a, A);
            results[13] = ClosestPointPointTriangle(B.b, A);
            results[14] = ClosestPointPointTriangle(B.c, A);

            Line min = results[0];
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].magnitude < min.magnitude)
                {
                    min = results[i];
                }
            }

            return min;
        }
    }
}