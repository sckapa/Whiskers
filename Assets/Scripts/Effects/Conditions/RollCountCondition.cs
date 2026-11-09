using UnityEngine;
using Whiskers.Dice.Rolling;

namespace Whiskers.Effects.Conditions
{
    // Enough of the owner's dice this roll match a face check, e.g. "three 9s" (Golden Ring)
    [CreateAssetMenu(fileName = "RollCountCondition", menuName = "Whiskers/Effects/Conditions/Roll Count")]
    public sealed class RollCountCondition : EffectCondition
    {
        private const int MinimumCount = 1;

        [SerializeField] private FaceCondition face = null;
        [SerializeField, Min(MinimumCount)] private int atLeast = 3;

        public override bool IsMet(EffectContext context)
        {
            if (face == null)
            {
                return false;
            }

            int count = 0;

            foreach (DieRollResult result in context.RollResults)
            {
                if (result.Owner == context.Owner.Side && face.Matches(result.Die, result.Face))
                {
                    count++;
                }
            }

            return count >= atLeast;
        }
    }
}
