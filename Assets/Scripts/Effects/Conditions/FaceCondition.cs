using UnityEngine;
using Whiskers.Dice;

namespace Whiskers.Effects.Conditions
{
    // Serialized, add new values at the end
    public enum FaceCheck
    {
        AnyNumber,
        ValueEquals,
        ValueAtLeast,
        Even,
        Odd,
        // Highest number on that die
        MaxValue,
        Symbol,
        GlyphColor,
        BodyColor
    }

    // Checks the die that triggered the effect and its rolled face, e.g. "rolled a 9" or "a Tree appeared"
    [CreateAssetMenu(fileName = "FaceCondition", menuName = "Whiskers/Effects/Conditions/Face")]
    public sealed class FaceCondition : EffectCondition
    {
        private const int Two = 2;

        [SerializeField] private FaceCheck check = FaceCheck.ValueEquals;
        [Tooltip("ValueEquals / ValueAtLeast")]
        [SerializeField] private int value = 9;
        [Tooltip("Symbol")]
        [SerializeField] private FaceSymbol symbol = FaceSymbol.None;
        [Tooltip("GlyphColor / BodyColor")]
        [SerializeField] private DiceColor color = DiceColor.None;

        public override bool IsMet(EffectContext context)
        {
            if (!context.HasDie || context.RolledFace == null)
            {
                return false;
            }

            return Matches(context.Die, context.RolledFace);
        }

        public bool Matches(DiceInstance die, DiceFace face)
        {
            switch (check)
            {
                case FaceCheck.AnyNumber:
                    return face.IsNumber;
                case FaceCheck.ValueEquals:
                    return face.IsNumber && face.Value == value;
                case FaceCheck.ValueAtLeast:
                    return face.IsNumber && face.Value >= value;
                case FaceCheck.Even:
                    return face.IsNumber && face.Value % Two == 0;
                case FaceCheck.Odd:
                    return face.IsNumber && face.Value % Two != 0;
                case FaceCheck.MaxValue:
                    return face.IsNumber && face.Value == GetMaxNumber(die);
                case FaceCheck.Symbol:
                    return face.IsSymbol && face.Symbol == symbol;
                case FaceCheck.GlyphColor:
                    return face.GlyphColor == color;
                case FaceCheck.BodyColor:
                    return die.BodyColor == color;
                default:
                    return false;
            }
        }

        private static int GetMaxNumber(DiceInstance die)
        {
            int max = int.MinValue;

            foreach (DiceFace face in die.Faces)
            {
                if (face.IsNumber && face.Value > max)
                {
                    max = face.Value;
                }
            }

            return max;
        }
    }
}
