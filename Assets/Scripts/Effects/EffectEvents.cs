using Whiskers.Combatants;
using Whiskers.Combatants.Status;
using Whiskers.Core.Events;

namespace Whiskers.Effects
{
    // An effect phase begins: Landing = phase 1, CombatStart / BeforeInteractions = phase 2, EndOfRoll = phase 4
    public readonly struct EffectPhaseStartedEvent : IGameEvent
    {
        public EffectTrigger Trigger { get; }
        public int RollNumber { get; }

        public EffectPhaseStartedEvent(EffectTrigger trigger, int rollNumber)
        {
            Trigger = trigger;
            RollNumber = rollNumber;
        }
    }

    // Published after an effect was applied, for visuals, sounds and equipment reacting to effects
    public readonly struct EffectAppliedEvent : IGameEvent
    {
        public EffectDefinition Effect { get; }
        public EffectContext Context { get; }

        // False for dice colors and symbols, true for combatant passives and equipment
        public bool FromPassive { get; }

        public EffectAppliedEvent(EffectDefinition effect, EffectContext context, bool fromPassive)
        {
            Effect = effect;
            Context = context;
            FromPassive = fromPassive;
        }
    }

    // Poison and similar statuses ticked before the interactions
    public readonly struct StatusTickedEvent : IGameEvent
    {
        public Combatant Combatant { get; }
        public StatusType Status { get; }
        public int Damage { get; }

        public StatusTickedEvent(Combatant combatant, StatusType status, int damage)
        {
            Combatant = combatant;
            Status = status;
            Damage = damage;
        }
    }
}
