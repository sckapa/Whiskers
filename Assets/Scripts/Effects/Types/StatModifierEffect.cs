using UnityEngine;
using Whiskers.Combatants.Stats;

namespace Whiskers.Effects.Types
{
    // Temporary stat bonus or penalty of a combatant (buffs, debuffs, auras), removed when the duration ends
    [CreateAssetMenu(fileName = "StatModifierEffect", menuName = "Whiskers/Effects/Stat Modifier")]
    public sealed class StatModifierEffect : EffectDefinition
    {
        [SerializeField] private EffectTarget target = EffectTarget.Owner;
        [SerializeField] private CombatantStatType stat = CombatantStatType.DicePerRoll;
        [SerializeField] private int amount = 1;
        [SerializeField] private EffectDuration duration = EffectDuration.NextRoll;

        public override void Apply(EffectContext context)
        {
            Stat targetStat = context.GetTarget(target).Stats.Get(stat);
            context.Services.AddTimedModifier(targetStat, amount, duration);
        }
    }
}
