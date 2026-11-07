using System.Collections.Generic;
using Whiskers.Core;
using Whiskers.Dice;

namespace Whiskers.Combatants
{
    // Whisker the cat. Persists through the whole run; run-specific data (gold, map progress) will live here.
    public sealed class PlayerCombatant : Combatant
    {
        public override CombatSide Side => CombatSide.Player;

        public PlayerCombatant(string displayName, int maxHealth, int dicePerRoll, IEnumerable<DiceInstance> startingDice)
            : base(displayName, maxHealth, dicePerRoll, startingDice)
        {
        }
    }
}
