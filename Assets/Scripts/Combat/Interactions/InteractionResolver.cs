using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Core.Events;

namespace Whiskers.Combat.Interactions
{
    // Merges or collides a pair and applies damage. Visuals are played separately.
    public sealed class InteractionResolver
    {
        private const float Half = 0.5f;

        private readonly BattlefieldObjectRegistry registry;

        // Objects that already dealt damage this roll
        private readonly HashSet<int> attackedThisRoll = new HashSet<int>();

        public AlliedCombination Combination { get; }
        public OpposingCollision Collision { get; }
        public DamageResolver Damage { get; }

        public InteractionResolver(BattlefieldObjectRegistry registry, DamageResolver damage)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
            Damage = damage ?? throw new ArgumentNullException(nameof(damage));
            Combination = new AlliedCombination(registry);
            Collision = new OpposingCollision(registry);
        }

        public void BeginRoll()
        {
            attackedThisRoll.Clear();
        }

        public bool HasAttackedThisRoll(BattlefieldObject battlefieldObject)
        {
            return battlefieldObject != null && attackedThisRoll.Contains(battlefieldObject.Id);
        }

        public bool CanResolve(BattlefieldObject first, BattlefieldObject second)
        {
            return first != null && second != null && first != second && IsActive(first) && IsActive(second);
        }

        public InteractionResult Resolve(BattlefieldObject first, BattlefieldObject second)
        {
            if (!CanResolve(first, second))
            {
                throw new InvalidOperationException($"Pair {first} / {second} is no longer valid.");
            }

            EventBus.Publish(new InteractionStartedEvent(first, second));

            InteractionResult result = AlliedCombination.IsAlliedPair(first, second)
                ? Combine(first, second)
                : Collide(first, second);

            EventBus.Publish(new InteractionResolvedEvent(result));
            return result;
        }

        // Object with nobody to collide with
        public InteractionResult ResolveDirectHit(BattlefieldObject attacker)
        {
            if (attacker == null || !IsActive(attacker))
            {
                throw new InvalidOperationException($"{attacker} is not on the board.");
            }

            EventBus.Publish(new InteractionStartedEvent(attacker, null));

            List<BattlefieldObject> removed = new List<BattlefieldObject>();
            DamageReport damage = DamageReport.None(attacker.Position);

            if (!HasAttackedThisRoll(attacker))
            {
                // TODO: symbol effects
                damage = Damage.Deal(attacker, attacker.Value, attacker.Position);
                attackedThisRoll.Add(attacker.Id);

                if (!OpposingCollision.StaysOnBoard(attacker))
                {
                    registry.Remove(attacker.Id);
                    removed.Add(attacker);
                }
            }

            InteractionResult result = InteractionResult.ForDirectHit(attacker, removed, damage);
            EventBus.Publish(new InteractionResolvedEvent(result));
            return result;
        }

        private InteractionResult Combine(BattlefieldObject first, BattlefieldObject second)
        {
            BattlefieldObject combined = Combination.Combine(first, second);
            return InteractionResult.ForCombination(first, second, combined);
        }

        private InteractionResult Collide(BattlefieldObject first, BattlefieldObject second)
        {
            Vector2 impactPosition = (first.Position + second.Position) * Half;
            CollisionOutcome outcome = Collision.Resolve(first, second);
            CollisionDecision decision = outcome.Decision;
            DamageReport damage = DamageReport.None(impactPosition);

            if (decision.HasWinner)
            {
                damage = Damage.Deal(decision.Winner, decision.Damage, impactPosition);
                attackedThisRoll.Add(decision.Winner.Id);
            }

            return InteractionResult.ForCollision(first, second, outcome, damage, impactPosition);
        }

        private bool IsActive(BattlefieldObject battlefieldObject)
        {
            return registry.TryGetActive(battlefieldObject.Id, out BattlefieldObject active) && active == battlefieldObject;
        }
    }
}
