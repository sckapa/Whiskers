using UnityEngine;
using Whiskers.Combatants.Status;

namespace Whiskers.Effects.Types
{
    // Adds status stacks, e.g. Shield to the owner or Poison to the opponent
    [CreateAssetMenu(fileName = "ApplyStatusEffect", menuName = "Whiskers/Effects/Apply Status")]
    public sealed class ApplyStatusEffect : EffectDefinition
    {
        private const int MinimumStacks = 1;

        [SerializeField] private EffectTarget target = EffectTarget.Owner;
        [SerializeField] private StatusType status = StatusType.Shield;
        [SerializeField, Min(MinimumStacks)] private int stacks = 1;

        public override void Apply(EffectContext context)
        {
            context.GetTarget(target).Statuses.Add(status, stacks);
        }
    }
}
