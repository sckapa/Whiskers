using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Dice.Geometry;

namespace Whiskers.Dice.Visuals
{
    // Anchor forward points into the die
    [Serializable]
    public sealed class DieGlyphSlot
    {
        [SerializeField] private Transform anchor;
        [SerializeField] private float size;

        public Transform Anchor => anchor;

        public float Size => size;

        public DieGlyphSlot(Transform anchor, float size)
        {
            this.anchor = anchor;
            this.size = size;
        }
    }

    // Index matches DiceInstance.Faces
    [Serializable]
    public sealed class DieFaceLayout
    {
        [SerializeField] private Vector3 localNormal;
        [SerializeField] private List<DieGlyphSlot> glyphSlots = new List<DieGlyphSlot>();

        public Vector3 LocalNormal => localNormal;
        public IReadOnlyList<DieGlyphSlot> GlyphSlots => glyphSlots;

        public DieFaceLayout(Vector3 localNormal, List<DieGlyphSlot> glyphSlots)
        {
            this.localNormal = localNormal;
            this.glyphSlots = glyphSlots;
        }
    }

    // Filled by Whiskers > Dice > Generate Dice Prefabs.
    [DisallowMultipleComponent]
    public sealed class DieModel : MonoBehaviour
    {
        [SerializeField] private DiceSize size;
        [SerializeField] private DieResultSide resultSide;
        [SerializeField] private List<DieFaceLayout> faces = new List<DieFaceLayout>();
        [SerializeField] private List<Vector3> capNormals = new List<Vector3>();

        public DiceSize Size => size;
        public DieResultSide ResultSide => resultSide;
        public IReadOnlyList<DieFaceLayout> Faces => faces;

        public DieLanding EvaluateLanding()
        {
            List<Vector3> worldFaceNormals = new List<Vector3>(faces.Count);
            List<Vector3> worldCapNormals = new List<Vector3>(capNormals.Count);

            foreach (DieFaceLayout face in faces)
            {
                worldFaceNormals.Add(transform.TransformDirection(face.LocalNormal));
            }

            foreach (Vector3 capNormal in capNormals)
            {
                worldCapNormals.Add(transform.TransformDirection(capNormal));
            }

            return DieLanding.Evaluate(worldFaceNormals, worldCapNormals, resultSide, Vector3.up);
        }

        // Used by the prefab generator
        public void Configure(DiceSize dieSize, DieResultSide side, List<DieFaceLayout> faceLayouts, List<Vector3> caps)
        {
            size = dieSize;
            resultSide = side;
            faces = faceLayouts;
            capNormals = caps;
        }
    }
}
