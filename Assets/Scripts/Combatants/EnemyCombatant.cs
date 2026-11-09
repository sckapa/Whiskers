using System.Collections.Generic;
using Whiskers.Combatants.Definitions;
using Whiskers.Core;
using Whiskers.Dice;
using Whiskers.Effects;

namespace Whiskers.Combatants
{
    public sealed class EnemyCombatant : Combatant
    {
        public override CombatSide Side => CombatSide.Enemy;

        public EnemyTier Tier { get; }

        public EnemyCombatant(
            string displayName,
            EnemyTier tier,
            int maxHealth,
            int dicePerRoll,
            IEnumerable<DiceInstance> startingDice,
            IEnumerable<EffectDefinition> passives = null)
            : base(displayName, maxHealth, dicePerRoll, startingDice, passives)
        {
            Tier = tier;
        }
    }
}
