using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Dice.Visuals
{
    // Pip patterns on a 3x3 grid
    public static class DicePipLayout
    {
        public const int MinPipValue = 1;
        public const int MaxPipValue = 9;

        private static readonly Vector2 TopLeft = new Vector2(-1f, 1f);
        private static readonly Vector2 TopCenter = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 MiddleLeft = new Vector2(-1f, 0f);
        private static readonly Vector2 Center = Vector2.zero;
        private static readonly Vector2 MiddleRight = new Vector2(1f, 0f);
        private static readonly Vector2 BottomLeft = new Vector2(-1f, -1f);
        private static readonly Vector2 BottomCenter = new Vector2(0f, -1f);
        private static readonly Vector2 BottomRight = new Vector2(1f, -1f);

        private static readonly Vector2[][] Patterns =
        {
            new[] { Center },
            new[] { TopLeft, BottomRight },
            new[] { TopLeft, Center, BottomRight },
            new[] { TopLeft, TopRight, BottomLeft, BottomRight },
            new[] { TopLeft, TopRight, Center, BottomLeft, BottomRight },
            new[] { TopLeft, TopRight, MiddleLeft, MiddleRight, BottomLeft, BottomRight },
            new[] { TopLeft, TopRight, MiddleLeft, Center, MiddleRight, BottomLeft, BottomRight },
            new[] { TopLeft, TopCenter, TopRight, MiddleLeft, MiddleRight, BottomLeft, BottomCenter, BottomRight },
            new[] { TopLeft, TopCenter, TopRight, MiddleLeft, Center, MiddleRight, BottomLeft, BottomCenter, BottomRight }
        };

        public static bool CanShow(int value)
        {
            return value >= MinPipValue && value <= MaxPipValue;
        }

        public static IReadOnlyList<Vector2> GetCells(int value)
        {
            return Patterns[value - MinPipValue];
        }
    }
}
