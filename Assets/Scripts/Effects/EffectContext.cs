using System;
using System.Collections.Generic;
using Whiskers.Battlefield;
using Whiskers.Combatants;
using Whiskers.Combatants.Stats;
using Whiskers.Dice;
using Whiskers.Dice.Rolling;
using Whiskers.Effects.Types;

namespace Whiskers.Effects
{
    // When an effect is checked. Serialized in effect assets, add new values at the end.
    public enum EffectTrigger
    {
        // Phase 1: a die of this roll has landed (once per die, extra rolls included)
        Landing,
        // Phase 2, only before the first interactions of a combat
        CombatStart,
        // Phase 2, before the interactions of every roll
        BeforeInteractions,
        // Phase 4, after the leftover dice dealt their damage, before the next roll
        EndOfRoll
    }

    // How long a granted modifier lasts. Serialized, add new values at the end.
    public enum EffectDuration
    {
        // Removed after the next roll has drawn its dice, so a DicePerRoll bonus affects exactly one draw
        NextRoll,
        // Removed when the combat ends
        Combat
    }

    // What effects can ask the combat for. Implemented by the combat's effect runner.
    public interface IEffectServices
    {
        // The dice are drawn from the bag and thrown after the current landing effects
        void RequestAdditionalRolls(Combatant roller, int count);

        // The modifier is removed automatically when the duration runs out
        void AddTimedModifier(Stat stat, int amount, EffectDuration duration);

        // Opens a domain on the board, centred on the context's die (or the board centre)
        void ActivateAura(AuraEffect aura, EffectContext context);
    }

    // Everything an effect needs to know about the moment it is checked.
    public sealed class EffectContext
    {
        public EffectTrigger Trigger { get; }
        public Combatant Owner { get; }
        public Combatant Opponent { get; }
        public BattlefieldBoard Board { get; }

        // Die that triggered the check, null for passives in phase 2
        public DiceInstance Die { get; }
        public DiceFace RolledFace { get; }

        // The die's object on the board, null if the die is not (or no longer) on the board
        public BattlefieldObject SourceObject { get; }

        // Every die that landed this roll, both sides, for combination conditions
        public IReadOnlyList<DieRollResult> RollResults { get; }

        public Random Random { get; }
        public IEffectServices Services { get; }

        public bool HasDie => Die != null;

        public EffectContext(
            EffectTrigger trigger,
            Combatant owner,
            Combatant opponent,
            BattlefieldBoard board,
            DiceInstance die,
            DiceFace rolledFace,
            BattlefieldObject sourceObject,
            IReadOnlyList<DieRollResult> rollResults,
            Random random,
            IEffectServices services)
        {
            Trigger = trigger;
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Opponent = opponent ?? throw new ArgumentNullException(nameof(opponent));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Die = die;
            RolledFace = rolledFace;
            SourceObject = sourceObject;
            RollResults = rollResults ?? Array.Empty<DieRollResult>();
            Random = random ?? throw new ArgumentNullException(nameof(random));
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public Combatant GetTarget(EffectTarget target)
        {
            return target == EffectTarget.Owner ? Owner : Opponent;
        }
    }

    // Serialized, add new values at the end
    public enum EffectTarget
    {
        Owner,
        Opponent
    }
}
