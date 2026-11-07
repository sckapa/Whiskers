using System;

namespace Whiskers.Dice
{
    public enum DiceSize
    {
        D4,
        D6,
        D8,
        D9,
        D12,
        D20
    }

    public static class DiceSizeExtensions
    {
        private const int D4FaceCount = 4;
        private const int D6FaceCount = 6;
        private const int D8FaceCount = 8;
        private const int D9FaceCount = 9;
        private const int D12FaceCount = 12;
        private const int D20FaceCount = 20;

        public static int GetFaceCount(this DiceSize size)
        {
            switch (size)
            {
                case DiceSize.D4: return D4FaceCount;
                case DiceSize.D6: return D6FaceCount;
                case DiceSize.D8: return D8FaceCount;
                case DiceSize.D9: return D9FaceCount;
                case DiceSize.D12: return D12FaceCount;
                case DiceSize.D20: return D20FaceCount;
                default: throw new ArgumentOutOfRangeException(nameof(size), size, "Unknown dice size.");
            }
        }

        public static bool HasCaps(this DiceSize size)
        {
            return size == DiceSize.D9;
        }
    }
}
