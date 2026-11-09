using UnityEngine;

namespace Whiskers.Effects.Conditions
{
    // HP of the owner or the opponent is at or below a threshold, e.g. Nine Lives at 9 HP
    [CreateAssetMenu(fileName = "HealthCondition", menuName = "Whiskers/Effects/Conditions/Health")]
    public sealed class HealthCondition : EffectCondition
    {
        [SerializeField] private EffectTarget whose = EffectTarget.Owner;
        [SerializeField, Min(0)] private int atOrBelow = 9;

        public override bool IsMet(EffectContext context)
        {
            return context.GetTarget(whose).Health.Current <= atOrBelow;
        }
    }
}
