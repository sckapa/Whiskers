using System;

namespace Whiskers.Combatants.Stats
{
    // Serialized in effect assets, add new values at the end
    public enum CombatantStatType
    {
        DicePerRoll
    }

    // Combat-related stats of a combatant. New stats are added here as mechanics require them.
    public sealed class CombatantStats
    {
        private const int MinimumDicePerRoll = 1;

        // How many dice are drawn from the bag and rolled at once
        public Stat DicePerRoll { get; }

        public CombatantStats(int baseDicePerRoll)
        {
            DicePerRoll = new Stat(baseDicePerRoll, MinimumDicePerRoll);
        }

        public Stat Get(CombatantStatType type)
        {
            switch (type)
            {
                case CombatantStatType.DicePerRoll:
                    return DicePerRoll;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown stat.");
            }
        }

        // Clears everything an item, passive or effect has granted, e.g. when it is unequipped
        public void RemoveModifiersFrom(object source)
        {
            DicePerRoll.RemoveModifiersFrom(source);
        }
    }
}
