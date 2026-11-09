using UnityEngine;

namespace Whiskers.Dice.Rolling
{
    internal static class ThrowDirection
    {
        // Counter-clockwise
        public static Vector2 Rotate(Vector2 direction, float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);

            return new Vector2(direction.x * cos - direction.y * sin, direction.x * sin + direction.y * cos);
        }
    }
}
