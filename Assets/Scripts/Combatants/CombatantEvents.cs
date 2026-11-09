using Whiskers.Core.Events;

namespace Whiskers.Combatants
{
    public readonly struct CombatantHealthChangedEvent : IGameEvent
    {
        public Combatant Combatant { get; }
        public int PreviousHealth { get; }
        public int CurrentHealth { get; }

        // Negative for damage, positive for healing
        public int Delta => CurrentHealth - PreviousHealth;

        public CombatantHealthChangedEvent(Combatant combatant, int previousHealth, int currentHealth)
        {
            Combatant = combatant;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
        }
    }

    public readonly struct CombatantDiedEvent : IGameEvent
    {
        public Combatant Combatant { get; }

        public CombatantDiedEvent(Combatant combatant)
        {
            Combatant = combatant;
        }
    }

    public readonly struct CombatantDiceCollectionChangedEvent : IGameEvent
    {
        public Combatant Combatant { get; }

        public CombatantDiceCollectionChangedEvent(Combatant combatant)
        {
            Combatant = combatant;
        }
    }

    public readonly struct CombatantPassivesChangedEvent : IGameEvent
    {
        public Combatant Combatant { get; }

        public CombatantPassivesChangedEvent(Combatant combatant)
        {
            Combatant = combatant;
        }
    }
}
