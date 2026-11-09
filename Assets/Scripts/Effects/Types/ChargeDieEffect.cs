using System.Collections.Generic;
using UnityEngine;
using Whiskers.Dice;

namespace Whiskers.Effects.Types
{
    // Serialized, add new values at the end
    public enum ChargeTarget
    {
        // The die that triggered the effect
        ThisDie,
        // A random other die of the owner that is in play this roll (e.g. Hourglass)
        AnotherDieInPlay
    }

    // Adds charges to a die. What charges do is up to the effects that consume them.
    [CreateAssetMenu(fileName = "ChargeDieEffect", menuName = "Whiskers/Effects/Charge Die")]
    public sealed class ChargeDieEffect : EffectDefinition
    {
        private const int MinimumAmount = 1;

        [SerializeField] private ChargeTarget target = ChargeTarget.ThisDie;
        [SerializeField, Min(MinimumAmount)] private int amount = 1;

        public override void Apply(EffectContext context)
        {
            DiceInstance charged = target == ChargeTarget.ThisDie ? context.Die : PickAnotherDie(context);

            if (charged != null)
            {
                charged.AddCharges(amount);
            }
        }

        protected override bool IsApplicable(EffectContext context)
        {
            return target != ChargeTarget.ThisDie || context.HasDie;
        }

        private static DiceInstance PickAnotherDie(EffectContext context)
        {
            if (context.Owner.CombatBag == null)
            {
                return null;
            }

            List<DiceInstance> candidates = new List<DiceInstance>(context.Owner.CombatBag.InPlay);
            candidates.Remove(context.Die);

            return candidates.Count > 0 ? candidates[context.Random.Next(candidates.Count)] : null;
        }
    }
}
