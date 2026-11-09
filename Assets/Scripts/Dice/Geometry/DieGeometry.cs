using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Dice.Geometry
{
    public enum DieResultSide
    {
        // Face pointing up (d6, d8, d12, d20)
        Top,
        // Face on the table (d4, d9)
        Bottom
    }

    // Forward points into the die
    public readonly struct GlyphSlotGeometry
    {
        public Vector3 Position { get; }
        public Vector3 Forward { get; }
        public Vector3 Up { get; }

        public float Size { get; }

        public GlyphSlotGeometry(Vector3 position, Vector3 forward, Vector3 up, float size)
        {
            Position = position;
            Forward = forward;
            Up = up;
            Size = size;
        }
    }

    // Index matches DiceInstance.Faces
    public sealed class DieFaceGeometry
    {
        public Vector3 Normal { get; }
        public List<GlyphSlotGeometry> GlyphSlots { get; } = new List<GlyphSlotGeometry>();

        public DieFaceGeometry(Vector3 normal)
        {
            Normal = normal;
        }
    }

    public sealed class DieGeometry
    {
        public DiceSize Size { get; }
        public DieResultSide ResultSide { get; }
        public DieMeshData Mesh { get; } = new DieMeshData();
        public List<DieFaceGeometry> Faces { get; } = new List<DieFaceGeometry>();

        public List<Vector3> CapNormals { get; } = new List<Vector3>();

        public DieGeometry(DiceSize size, DieResultSide resultSide)
        {
            Size = size;
            ResultSide = resultSide;
        }
    }
}
