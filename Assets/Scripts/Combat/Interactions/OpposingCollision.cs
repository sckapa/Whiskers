using System;
using System.Collections.Generic;
using Whiskers.Battlefield;
using Whiskers.Dice;

namespace Whiskers.Combat.Interactions
{
    public sealed class CollisionOutcome
    {
        public CollisionDecision Decision { get; }
        public IReadOnlyList<BattlefieldObject> RemovedObjects { get; }

        public bool WinnerStays { get; }

        public CollisionOutcome(CollisionDecision decision, IReadOnlyList<BattlefieldObject> removedObjects, bool winnerStays)
        {
            Decision = decision;
            RemovedObjects = removedObjects;
            WinnerStays = winnerStays;
        }
    }

    // Loser is removed. Winner is removed too unless it has a red glyph.
    public sealed class OpposingCollision
    {
        public const DiceColor StayOnBoardGlyph = DiceColor.Red;

        private readonly BattlefieldObjectRegistry registry;
        private readonly List<ICollisionRule> rules = new List<ICollisionRule>();
        private readonly ICollisionRule defaultRule = new SubtractionCollisionRule();

        public OpposingCollision(BattlefieldObjectRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public static bool IsOpposingPair(BattlefieldObject first, BattlefieldObject second)
        {
            return first != null && second != null && first.IsOpposing(second);
        }

        public static bool StaysOnBoard(BattlefieldObject battlefieldObject)
        {
            // TODO: items and effects
            return battlefieldObject.HasGlyphColor(StayOnBoardGlyph);
        }

        public void AddRule(ICollisionRule rule)
        {
            rules.Add(rule ?? throw new ArgumentNullException(nameof(rule)));
        }

        public void RemoveRule(ICollisionRule rule)
        {
            rules.Remove(rule);
        }

        public CollisionOutcome Resolve(BattlefieldObject first, BattlefieldObject second)
        {
            if (!IsOpposingPair(first, second))
            {
                throw new ArgumentException($"{first} and {second} are not an opposing pair.");
            }

            CollisionDecision decision = Decide(first, second);
            List<BattlefieldObject> removed = new List<BattlefieldObject>();
            bool winnerStays = false;

            foreach (BattlefieldObject participant in new[] { first, second })
            {
                if (participant == decision.Winner && StaysOnBoard(participant))
                {
                    winnerStays = true;
                    registry.SetValue(participant.Id, decision.RemainingValue);
                    continue;
                }

                registry.Remove(participant.Id);
                removed.Add(participant);
            }

            return new CollisionOutcome(decision, removed, winnerStays);
        }

        private CollisionDecision Decide(BattlefieldObject first, BattlefieldObject second)
        {
            foreach (ICollisionRule rule in rules)
            {
                if (rule.TryDecide(first, second, out CollisionDecision decision))
                {
                    return decision;
                }
            }

            defaultRule.TryDecide(first, second, out CollisionDecision fallback);
            return fallback;
        }
    }
}
