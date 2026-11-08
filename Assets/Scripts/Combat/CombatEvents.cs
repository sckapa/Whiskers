using System.Collections.Generic;
using Whiskers.Combatants;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Dice.Rolling;

namespace Whiskers.Combat
{
    public enum CombatPhase
    {
        None,
        Setup,
        Rolling,
        // Phase 1
        LandingEffects,
        // Phase 2
        StartEffects,
        // Phase 3
        Resolving,
        // Phase 4: leftover objects hit the opponent, end-of-roll effects
        DamageResolution,
        Finished
    }

    public readonly struct CombatStartedEvent : IGameEvent
    {
        public PlayerCombatant Player { get; }
        public EnemyCombatant Enemy { get; }

        public CombatStartedEvent(PlayerCombatant player, EnemyCombatant enemy)
        {
            Player = player;
            Enemy = enemy;
        }
    }

    public readonly struct CombatPhaseChangedEvent : IGameEvent
    {
        public CombatPhase Phase { get; }

        public CombatPhaseChangedEvent(CombatPhase phase)
        {
            Phase = phase;
        }
    }

    // All dice of the roll are on the board
    public readonly struct CombatRollsCompletedEvent : IGameEvent
    {
        public int RollNumber { get; }
        public IReadOnlyList<DieRollResult> Results { get; }

        public CombatRollsCompletedEvent(int rollNumber, IReadOnlyList<DieRollResult> results)
        {
            RollNumber = rollNumber;
            Results = results;
        }
    }

    public readonly struct CombatEndedEvent : IGameEvent
    {
        public CombatSide Winner { get; }

        public CombatEndedEvent(CombatSide winner)
        {
            Winner = winner;
        }
    }
}
