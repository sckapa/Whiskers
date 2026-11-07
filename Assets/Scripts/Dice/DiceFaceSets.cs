using System.Collections.Generic;

namespace Whiskers.Dice
{
    // Factory for commonly used face layouts
    public static class DiceFaceSets
    {
        private const int LowestStandardValue = 1;

        // Plain numbered die: 1..N, where N is the face count of the size
        public static List<DiceFace> CreateStandardNumbers(DiceSize size)
        {
            int faceCount = size.GetFaceCount();
            List<DiceFace> faces = new List<DiceFace>(faceCount);

            for (int value = LowestStandardValue; value <= faceCount; value++)
            {
                faces.Add(DiceFace.CreateNumber(value));
            }

            return faces;
        }
    }
}
