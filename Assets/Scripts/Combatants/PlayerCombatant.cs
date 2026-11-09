using System.Collections.Generic;
using Whiskers.Core;
using Whiskers.Dice;
using Whiskers.Effects;

namespace Whiskers.Combatants
{
    // TODO: gold
    public sealed class PlayerCombatant : Combatant
    {
        public override CombatSide Side => CombatSide.Player;

        public PlayerCombatant(
            string displayName,
            int maxHealth,
            int dicePerRoll,
            IEnumerable<DiceInstance> startingDice,
            IEnumerable<EffectDefinition> passives = null)
            : base(displayName, maxHealth, dicePerRoll, startingDice, passives)
        {
        }
    }
}
