using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Dice.Geometry
{
    // Builds dice meshes and glyph slots.
    // Face directions are the vertices of the dual solid.
    public static class DieGeometryBuilder
    {
        // All dice fit in a 1 unit sphere
        private const float DieCircumradius = 0.5f;

        // Avoids z-fighting
        private const float GlyphSurfaceOffset = 0.002f;

        private const float SameFaceEpsilon = 1e-4f;
        private const float OppositeDotThreshold = -0.999f;
        private const int SortKeyDecimals = 4;

        // Glyph size relative to the face inradius
        private const float CenterGlyphSizeToInradius = 1.2f;
        private const float D4GlyphSizeToInradius = 0.75f;

        // 0 = face center, 1 = vertex
        private const float D4GlyphTowardVertex = 0.55f;

        // Short "pencil" prism for the d9
        private const float PrismRadius = 0.38f;
        private const float PrismHalfLength = 0.5f;
        // D9 notch sizes
        private const float NotchSideFraction = 0.35f;
        private const float NotchLengthToWidth = 1.25f;
        private const float NotchMaxHalfLengthShare = 0.8f;
        private const float PrismGlyphSizeToNotchWidth = 0.85f;

        private static readonly float GoldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;

        public static DieGeometry Build(DiceSize size)
        {
            switch (size)
            {
                case DiceSize.D4: return BuildTetrahedron();
                case DiceSize.D6: return BuildCenteredPolyhedron(size, CubeVertices(), AxisDirections());
                case DiceSize.D8: return BuildCenteredPolyhedron(size, AxisDirections(), CubeVertices());
                case DiceSize.D9: return BuildPrism();
                case DiceSize.D12: return BuildCenteredPolyhedron(size, DodecahedronVertices(), IcosahedronVertices());
                case DiceSize.D20: return BuildCenteredPolyhedron(size, IcosahedronVertices(), DodecahedronVertices());
                default: throw new ArgumentOutOfRangeException(nameof(size), size, "No geometry for this dice size.");
            }
        }

        // Opposite faces: i and N-1-i
        private static DieGeometry BuildCenteredPolyhedron(DiceSize size, List<Vector3> vertices, List<Vector3> faceDirections)
        {
            DieGeometry geometry = new DieGeometry(size, DieResultSide.Top);
            List<PolyhedronFace> faces = OrderInOppositePairs(ExtractFaces(ScaleToRadius(vertices), faceDirections));

            foreach (PolyhedronFace face in faces)
            {
                geometry.Mesh.AddPolygon(face.Vertices, face.Normal);

                DieFaceGeometry faceGeometry = new DieFaceGeometry(face.Normal);
                faceGeometry.GlyphSlots.Add(new GlyphSlotGeometry(
                    face.Center + face.Normal * GlyphSurfaceOffset,
                    -face.Normal,
                    CenterGlyphUp(face),
                    face.Inradius * CenterGlyphSizeToInradius));

                geometry.Faces.Add(faceGeometry);
            }

            CheckFaceCount(geometry);
            return geometry;
        }

        // Read from the bottom, value printed 3 times near the top vertex
        private static DieGeometry BuildTetrahedron()
        {
            DieGeometry geometry = new DieGeometry(DiceSize.D4, DieResultSide.Bottom);
            List<Vector3> vertices = ScaleToRadius(TetrahedronVertices());
            List<Vector3> faceDirections = new List<Vector3>();

            // Each tetrahedron face lies opposite one vertex
            foreach (Vector3 vertex in vertices)
            {
                faceDirections.Add(-vertex);
            }

            List<PolyhedronFace> faces = SortDeterministically(ExtractFaces(vertices, faceDirections));

            foreach (PolyhedronFace face in faces)
            {
                geometry.Mesh.AddPolygon(face.Vertices, face.Normal);
            }

            foreach (PolyhedronFace resultFace in faces)
            {
                DieFaceGeometry faceGeometry = new DieFaceGeometry(resultFace.Normal);
                Vector3 topVertex = FurthestVertex(vertices, -resultFace.Normal);

                foreach (PolyhedronFace printedFace in faces)
                {
                    if (printedFace == resultFace)
                    {
                        continue;
                    }

                    Vector3 towardVertex = topVertex - printedFace.Center;

                    faceGeometry.GlyphSlots.Add(new GlyphSlotGeometry(
                        printedFace.Center + towardVertex * D4GlyphTowardVertex + printedFace.Normal * GlyphSurfaceOffset,
                        -printedFace.Normal,
                        towardVertex.normalized,
                        printedFace.Inradius * D4GlyphSizeToInradius));
                }

                geometry.Faces.Add(faceGeometry);
            }

            CheckFaceCount(geometry);
            return geometry;
        }

        // Read from the bottom. Each edge has a notch with the opposite side's value.
        private static DieGeometry BuildPrism()
        {
            const float half = 0.5f;

            DieGeometry geometry = new DieGeometry(DiceSize.D9, DieResultSide.Bottom);
            int sideCount = DiceSize.D9.GetFaceCount();
            float sectorAngle = Mathf.PI * 2f / sideCount;
            Vector3 axis = Vector3.forward;

            Vector3[] rim = new Vector3[sideCount];
            PrismNotch[] notches = new PrismNotch[sideCount];

            for (int vertex = 0; vertex < sideCount; vertex++)
            {
                rim[vertex] = PrismRim(vertex * sectorAngle);
            }

            for (int vertex = 0; vertex < sideCount; vertex++)
            {
                notches[vertex] = new PrismNotch(rim[vertex], rim[Wrap(vertex - 1, sideCount)], rim[Wrap(vertex + 1, sideCount)]);
            }

            AddPrismCaps(geometry, rim, axis);

            for (int side = 0; side < sideCount; side++)
            {
                int next = Wrap(side + 1, sideCount);
                Vector3 normal = ((rim[side] + rim[next]) * half).normalized;

                AddNotchedSide(geometry.Mesh, rim[side], rim[next], notches[side].NextCut, notches[next].PreviousCut, notches[side].HalfLength, axis, normal);
                AddNotchPocket(geometry.Mesh, rim[side], notches[side], axis);
            }

            // Edge opposite to side i
            int oppositeVertexOffset = (sideCount + 1) / 2;

            for (int side = 0; side < sideCount; side++)
            {
                Vector3 normal = ((rim[side] + rim[Wrap(side + 1, sideCount)]) * half).normalized;
                PrismNotch notch = notches[Wrap(side + oppositeVertexOffset, sideCount)];

                DieFaceGeometry face = new DieFaceGeometry(normal);
                face.GlyphSlots.Add(new GlyphSlotGeometry(
                    notch.FloorCenter + notch.Normal * GlyphSurfaceOffset,
                    -notch.Normal,
                    axis,
                    notch.Width * PrismGlyphSizeToNotchWidth));

                geometry.Faces.Add(face);
            }

            CheckFaceCount(geometry);
            return geometry;
        }

        private static void AddPrismCaps(DieGeometry geometry, Vector3[] rim, Vector3 axis)
        {
            List<Vector3> bottomCap = new List<Vector3>(rim.Length);
            List<Vector3> topCap = new List<Vector3>(rim.Length);

            foreach (Vector3 point in rim)
            {
                bottomCap.Add(point - axis * PrismHalfLength);
                topCap.Add(point + axis * PrismHalfLength);
            }

            geometry.Mesh.AddPolygon(bottomCap, -axis);
            geometry.Mesh.AddPolygon(topCap, axis);
            geometry.CapNormals.Add(-axis);
            geometry.CapNormals.Add(axis);
        }

        // Side with notches cut out, 3 convex strips
        private static void AddNotchedSide(
            DieMeshData mesh,
            Vector3 start,
            Vector3 end,
            Vector3 startCut,
            Vector3 endCut,
            float notchHalfLength,
            Vector3 axis,
            Vector3 normal)
        {
            Vector3 bottom = -axis * PrismHalfLength;
            Vector3 top = axis * PrismHalfLength;
            Vector3 notchBottom = -axis * notchHalfLength;
            Vector3 notchTop = axis * notchHalfLength;

            mesh.AddPolygon(new[] { start + bottom, end + bottom, end + notchBottom, start + notchBottom }, normal);
            mesh.AddPolygon(new[] { start + notchTop, end + notchTop, end + top, start + top }, normal);
            mesh.AddPolygon(new[] { startCut + notchBottom, endCut + notchBottom, endCut + notchTop, startCut + notchTop }, normal);
        }

        // Notch floor + 2 end walls
        private static void AddNotchPocket(DieMeshData mesh, Vector3 edge, PrismNotch notch, Vector3 axis)
        {
            Vector3 notchBottom = -axis * notch.HalfLength;
            Vector3 notchTop = axis * notch.HalfLength;

            mesh.AddPolygon(
                new[] { notch.PreviousCut + notchBottom, notch.NextCut + notchBottom, notch.NextCut + notchTop, notch.PreviousCut + notchTop },
                notch.Normal);

            mesh.AddPolygon(new[] { notch.PreviousCut + notchBottom, edge + notchBottom, notch.NextCut + notchBottom }, axis);
            mesh.AddPolygon(new[] { notch.PreviousCut + notchTop, edge + notchTop, notch.NextCut + notchTop }, -axis);
        }

        private static int Wrap(int index, int count)
        {
            return (index % count + count) % count;
        }

        private static Vector3 PrismRim(float angle)
        {
            return new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * PrismRadius;
        }

        private static Vector3 CenterGlyphUp(PolyhedronFace face)
        {
            const int evenDivisor = 2;
            const float half = 0.5f;

            Vector3 target = face.Vertices.Count % evenDivisor == 0
                ? (face.Vertices[0] + face.Vertices[1]) * half
                : face.Vertices[0];

            return DieGeometryMath.OnPlane(target - face.Center, face.Normal);
        }

        private static List<PolyhedronFace> ExtractFaces(List<Vector3> vertices, List<Vector3> faceDirections)
        {
            List<PolyhedronFace> faces = new List<PolyhedronFace>(faceDirections.Count);

            foreach (Vector3 direction in faceDirections)
            {
                Vector3 normal = direction.normalized;
                float planeDistance = float.MinValue;

                foreach (Vector3 vertex in vertices)
                {
                    planeDistance = Mathf.Max(planeDistance, Vector3.Dot(vertex, normal));
                }

                List<Vector3> faceVertices = new List<Vector3>();

                foreach (Vector3 vertex in vertices)
                {
                    if (Vector3.Dot(vertex, normal) >= planeDistance - SameFaceEpsilon)
                    {
                        faceVertices.Add(vertex);
                    }
                }

                faces.Add(new PolyhedronFace(normal, SortAroundNormal(faceVertices, normal)));
            }

            return faces;
        }

        private static List<Vector3> SortAroundNormal(List<Vector3> points, Vector3 normal)
        {
            Vector3 center = DieGeometryMath.Average(points);
            Vector3 reference = (points[0] - center).normalized;
            Vector3 side = Vector3.Cross(normal, reference);

            List<Vector3> sorted = new List<Vector3>(points);
            sorted.Sort((a, b) => AngleAround(a).CompareTo(AngleAround(b)));
            return sorted;

            float AngleAround(Vector3 point)
            {
                Vector3 offset = point - center;
                return Mathf.Atan2(Vector3.Dot(offset, side), Vector3.Dot(offset, reference));
            }
        }

        private static List<PolyhedronFace> OrderInOppositePairs(List<PolyhedronFace> faces)
        {
            List<PolyhedronFace> sorted = SortDeterministically(faces);
            PolyhedronFace[] ordered = new PolyhedronFace[sorted.Count];
            HashSet<PolyhedronFace> placed = new HashSet<PolyhedronFace>();
            int nextIndex = 0;

            foreach (PolyhedronFace face in sorted)
            {
                if (placed.Contains(face))
                {
                    continue;
                }

                PolyhedronFace opposite = sorted.Find(other => Vector3.Dot(face.Normal, other.Normal) < OppositeDotThreshold);

                if (opposite == null)
                {
                    throw new InvalidOperationException("Every face of a centered polyhedron die needs an opposite face.");
                }

                ordered[nextIndex] = face;
                ordered[ordered.Length - 1 - nextIndex] = opposite;
                placed.Add(face);
                placed.Add(opposite);
                nextIndex++;
            }

            return new List<PolyhedronFace>(ordered);
        }

        // Rounded to ignore float noise
        private static List<PolyhedronFace> SortDeterministically(List<PolyhedronFace> faces)
        {
            List<PolyhedronFace> sorted = new List<PolyhedronFace>(faces);
            sorted.Sort((a, b) =>
            {
                int byY = Round(b.Normal.y).CompareTo(Round(a.Normal.y));

                if (byY != 0)
                {
                    return byY;
                }

                int byX = Round(b.Normal.x).CompareTo(Round(a.Normal.x));
                return byX != 0 ? byX : Round(b.Normal.z).CompareTo(Round(a.Normal.z));
            });

            return sorted;
        }

        private static double Round(float value)
        {
            return Math.Round(value, SortKeyDecimals);
        }

        private static Vector3 FurthestVertex(List<Vector3> vertices, Vector3 direction)
        {
            Vector3 furthest = vertices[0];

            foreach (Vector3 vertex in vertices)
            {
                if (Vector3.Dot(vertex, direction) > Vector3.Dot(furthest, direction))
                {
                    furthest = vertex;
                }
            }

            return furthest;
        }

        private static List<Vector3> ScaleToRadius(List<Vector3> vertices)
        {
            float scale = DieCircumradius / DieGeometryMath.MaxDistance(vertices, Vector3.zero);
            List<Vector3> scaled = new List<Vector3>(vertices.Count);

            foreach (Vector3 vertex in vertices)
            {
                scaled.Add(vertex * scale);
            }

            return scaled;
        }

        private static void CheckFaceCount(DieGeometry geometry)
        {
            if (geometry.Faces.Count != geometry.Size.GetFaceCount())
            {
                throw new InvalidOperationException(
                    $"{geometry.Size} geometry produced {geometry.Faces.Count} faces instead of {geometry.Size.GetFaceCount()}.");
            }
        }

        private static List<Vector3> TetrahedronVertices()
        {
            return new List<Vector3>
            {
                new Vector3(1f, 1f, 1f),
                new Vector3(1f, -1f, -1f),
                new Vector3(-1f, 1f, -1f),
                new Vector3(-1f, -1f, 1f)
            };
        }

        private static List<Vector3> CubeVertices()
        {
            List<Vector3> vertices = new List<Vector3>();

            foreach (float x in Signs())
            {
                foreach (float y in Signs())
                {
                    foreach (float z in Signs())
                    {
                        vertices.Add(new Vector3(x, y, z));
                    }
                }
            }

            return vertices;
        }

        private static List<Vector3> AxisDirections()
        {
            return new List<Vector3>
            {
                Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back
            };
        }

        // Cyclic permutations of (0, ±1, ±φ)
        private static List<Vector3> IcosahedronVertices()
        {
            List<Vector3> vertices = new List<Vector3>();

            foreach (float a in Signs())
            {
                foreach (float b in Signs())
                {
                    vertices.Add(new Vector3(0f, a, b * GoldenRatio));
                    vertices.Add(new Vector3(a, b * GoldenRatio, 0f));
                    vertices.Add(new Vector3(b * GoldenRatio, 0f, a));
                }
            }

            return vertices;
        }

        // Cube corners + cyclic permutations of (0, ±φ, ±1/φ)
        private static List<Vector3> DodecahedronVertices()
        {
            List<Vector3> vertices = CubeVertices();
            float inverse = 1f / GoldenRatio;

            foreach (float a in Signs())
            {
                foreach (float b in Signs())
                {
                    vertices.Add(new Vector3(0f, a * GoldenRatio, b * inverse));
                    vertices.Add(new Vector3(b * inverse, 0f, a * GoldenRatio));
                    vertices.Add(new Vector3(a * GoldenRatio, b * inverse, 0f));
                }
            }

            return vertices;
        }

        private static float[] Signs()
        {
            return new[] { 1f, -1f };
        }

        private readonly struct PrismNotch
        {
            public Vector3 Normal { get; }

            // Where the cut meets the neighbouring sides
            public Vector3 PreviousCut { get; }
            public Vector3 NextCut { get; }

            public Vector3 FloorCenter { get; }
            public float Width { get; }
            public float HalfLength { get; }

            public PrismNotch(Vector3 edge, Vector3 previousEdge, Vector3 nextEdge)
            {
                const float half = 0.5f;

                Normal = edge.normalized;
                PreviousCut = edge + (previousEdge - edge) * NotchSideFraction;
                NextCut = edge + (nextEdge - edge) * NotchSideFraction;
                FloorCenter = (PreviousCut + NextCut) * half;
                Width = Vector3.Distance(PreviousCut, NextCut);
                HalfLength = Mathf.Min(Width * NotchLengthToWidth * half, PrismHalfLength * NotchMaxHalfLengthShare);
            }
        }

        private sealed class PolyhedronFace
        {
            public Vector3 Normal { get; }
            public List<Vector3> Vertices { get; }
            public Vector3 Center { get; }

            // Distance from the center to the middle of an edge
            public float Inradius { get; }

            public PolyhedronFace(Vector3 normal, List<Vector3> vertices)
            {
                const float half = 0.5f;

                Normal = normal;
                Vertices = vertices;
                Center = DieGeometryMath.Average(vertices);
                Inradius = Vector3.Distance(Center, (vertices[0] + vertices[1]) * half);
            }
        }
    }
}
