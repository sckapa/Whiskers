using UnityEngine;

namespace Whiskers.Effects.Types
{
    // Restores HP, never above max (Health clamps it)
    [CreateAssetMenu(fileName = "HealEffect", menuName = "Whiskers/Effects/Heal")]
    public sealed class HealEffect : EffectDefinition
    {
        private const int MinimumAmount = 0;

        [SerializeField] private EffectTarget target = EffectTarget.Owner;
        [SerializeField, Min(MinimumAmount)] private int amount = 3;
        [Tooltip("Heal by the rolled number instead of Amount (symbols heal nothing)")]
        [SerializeField] private bool useRolledValue = false;

        public override void Apply(EffectContext context)
        {
            int healing = useRolledValue ? GetRolledValue(context) : amount;

            if (healing > MinimumAmount)
            {
                context.GetTarget(target).Heal(healing);
            }
        }

        private static int GetRolledValue(EffectContext context)
        {
            return context.RolledFace != null && context.RolledFace.IsNumber ? context.RolledFace.Value : MinimumAmount;
        }
    }
}
