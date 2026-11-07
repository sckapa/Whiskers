using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Dice.Geometry
{
    // Vertices aren't shared, flat shading
    public sealed class DieMeshData
    {
        private const float UvCenter = 0.5f;
        private const float UvDiameterScale = 2f;

        public List<Vector3> Vertices { get; } = new List<Vector3>();
        public List<Vector3> Normals { get; } = new List<Vector3>();
        public List<Vector2> Uvs { get; } = new List<Vector2>();
        public List<int> Triangles { get; } = new List<int>();

        public void AddPolygon(IReadOnlyList<Vector3> polygon, Vector3 outwardNormal)
        {
            Vector3 normal = outwardNormal.normalized;
            List<Vector3> ordered = new List<Vector3>(polygon);

            Vector3 winding = Vector3.Cross(ordered[1] - ordered[0], ordered[2] - ordered[0]);

            if (Vector3.Dot(winding, normal) < 0f)
            {
                ordered.Reverse();
            }

            Vector3 center = DieGeometryMath.Average(ordered);
            Vector3 tangent = (ordered[0] - center).normalized;
            Vector3 bitangent = Vector3.Cross(normal, tangent);
            float extent = DieGeometryMath.MaxDistance(ordered, center);
            int firstIndex = Vertices.Count;

            foreach (Vector3 point in ordered)
            {
                Vector3 offset = point - center;
                Vector2 planar = new Vector2(Vector3.Dot(offset, tangent), Vector3.Dot(offset, bitangent));

                Vertices.Add(point);
                Normals.Add(normal);
                Uvs.Add(planar / (extent * UvDiameterScale) + Vector2.one * UvCenter);
            }

            for (int i = 1; i < ordered.Count - 1; i++)
            {
                Triangles.Add(firstIndex);
                Triangles.Add(firstIndex + i);
                Triangles.Add(firstIndex + i + 1);
            }
        }
    }
}
