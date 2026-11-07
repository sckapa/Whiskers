using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Dice.Geometry
{
    internal static class DieGeometryMath
    {
        public static Vector3 Average(IReadOnlyList<Vector3> points)
        {
            Vector3 sum = Vector3.zero;

            foreach (Vector3 point in points)
            {
                sum += point;
            }

            return sum / points.Count;
        }

        public static float MaxDistance(IReadOnlyList<Vector3> points, Vector3 origin)
        {
            float maxDistance = 0f;

            foreach (Vector3 point in points)
            {
                maxDistance = Mathf.Max(maxDistance, Vector3.Distance(point, origin));
            }

            return maxDistance;
        }

        // Projects a direction onto the plane with the given normal
        public static Vector3 OnPlane(Vector3 direction, Vector3 planeNormal)
        {
            return (direction - planeNormal * Vector3.Dot(direction, planeNormal)).normalized;
        }
    }
}
