using System.Collections.Generic;

namespace Whiskers.Dice
{
    public static class DiceFaceSets
    {
        private const int LowestStandardValue = 1;

        // D9 landing on a cap
        public const int DefaultCapValue = 0;

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

        public static DiceFace CreateDefaultCapFace()
        {
            return DiceFace.CreateNumber(DefaultCapValue);
        }
    }
}
