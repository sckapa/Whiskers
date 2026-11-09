using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Dice.Geometry
{
    // Face index or prism cap
    public readonly struct DieLanding
    {
        private const int NoFace = -1;
        private const float PerfectAlignment = 1f;

        public int FaceIndex { get; }
        public bool IsCap { get; }

        // 1 = flat
        public float Alignment { get; }

        private DieLanding(int faceIndex, bool isCap, float alignment)
        {
            FaceIndex = faceIndex;
            IsCap = isCap;
            Alignment = alignment;
        }

        public static DieLanding OnFace(int faceIndex, float alignment = PerfectAlignment)
        {
            return new DieLanding(faceIndex, false, alignment);
        }

        public static DieLanding OnCap(float alignment = PerfectAlignment)
        {
            return new DieLanding(NoFace, true, alignment);
        }

        public DiceFace Resolve(DiceInstance die)
        {
            return IsCap ? die.CapFace : die.GetFace(FaceIndex);
        }

        // World space normals
        public static DieLanding Evaluate(
            IReadOnlyList<Vector3> faceNormals,
            IReadOnlyList<Vector3> capNormals,
            DieResultSide resultSide,
            Vector3 worldUp)
        {
            Vector3 readDirection = resultSide == DieResultSide.Top ? worldUp : -worldUp;
            float bestAlignment = float.MinValue;
            int bestFace = NoFace;

            for (int i = 0; i < faceNormals.Count; i++)
            {
                float alignment = Vector3.Dot(faceNormals[i], readDirection);

                if (alignment > bestAlignment)
                {
                    bestAlignment = alignment;
                    bestFace = i;
                }
            }

            foreach (Vector3 capNormal in capNormals)
            {
                float alignment = Vector3.Dot(capNormal, readDirection);

                if (alignment > bestAlignment)
                {
                    return OnCap(alignment);
                }
            }

            return OnFace(bestFace, bestAlignment);
        }
    }
}
