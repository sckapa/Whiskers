using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Core.Events;

namespace Whiskers.Combat.Interactions
{
    public enum InteractionKind
    {
        Combination,
        Collision,
        DirectHit
    }

    public sealed class InteractionResult
    {
        public InteractionKind Kind { get; }
        public BattlefieldObject First { get; }

        public BattlefieldObject Second { get; }

        public BattlefieldObject Combined { get; }

        public CollisionDecision Decision { get; }
        public IReadOnlyList<BattlefieldObject> RemovedObjects { get; }
        public bool WinnerStays { get; }
        public DamageReport Damage { get; }

        // Board units
        public Vector2 Position { get; }

        private InteractionResult(
            InteractionKind kind,
            BattlefieldObject first,
            BattlefieldObject second,
            BattlefieldObject combined,
            CollisionDecision decision,
            IReadOnlyList<BattlefieldObject> removedObjects,
            bool winnerStays,
            DamageReport damage,
            Vector2 position)
        {
            Kind = kind;
            First = first;
            Second = second;
            Combined = combined;
            Decision = decision;
            RemovedObjects = removedObjects;
            WinnerStays = winnerStays;
            Damage = damage;
            Position = position;
        }

        public static InteractionResult ForCombination(BattlefieldObject first, BattlefieldObject second, BattlefieldObject combined)
        {
            return new InteractionResult(
                InteractionKind.Combination,
                first,
                second,
                combined,
                CollisionDecision.BothDestroyed(),
                Array.Empty<BattlefieldObject>(),
                false,
                DamageReport.None(combined.Position),
                combined.Position);
        }

        public static InteractionResult ForCollision(
            BattlefieldObject first,
            BattlefieldObject second,
            CollisionOutcome outcome,
            DamageReport damage,
            Vector2 position)
        {
            return new InteractionResult(
                InteractionKind.Collision,
                first,
                second,
                null,
                outcome.Decision,
                outcome.RemovedObjects,
                outcome.WinnerStays,
                damage,
                position);
        }

        public static InteractionResult ForDirectHit(
            BattlefieldObject attacker,
            IReadOnlyList<BattlefieldObject> removedObjects,
            DamageReport damage)
        {
            return new InteractionResult(
                InteractionKind.DirectHit,
                attacker,
                null,
                null,
                CollisionDecision.Win(attacker, attacker.Value, damage.Requested),
                removedObjects,
                removedObjects.Count == 0,
                damage,
                attacker.Position);
        }
    }

    public readonly struct InteractionStartedEvent : IGameEvent
    {
        public BattlefieldObject First { get; }
        public BattlefieldObject Second { get; }

        public InteractionStartedEvent(BattlefieldObject first, BattlefieldObject second)
        {
            First = first;
            Second = second;
        }
    }

    public readonly struct InteractionResolvedEvent : IGameEvent
    {
        public InteractionResult Result { get; }

        public InteractionResolvedEvent(InteractionResult result)
        {
            Result = result;
        }
    }
}
