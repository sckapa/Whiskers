using System;
using Whiskers.Battlefield;

namespace Whiskers.Combat.Interactions
{
    // Merges two allied objects at their midpoint. Values are summed, faces and dice ids are kept.
    public sealed class AlliedCombination
    {
        private readonly BattlefieldObjectRegistry registry;

        public AlliedCombination(BattlefieldObjectRegistry registry)
        {
            this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
        }

        public static bool IsAlliedPair(BattlefieldObject first, BattlefieldObject second)
        {
            return first != null && second != null && first != second && first.IsAlliedWith(second);
        }

        public BattlefieldObject Combine(BattlefieldObject first, BattlefieldObject second)
        {
            if (!IsAlliedPair(first, second))
            {
                throw new ArgumentException($"{first} and {second} are not an allied pair.");
            }

            // TODO: synergy rules
            return registry.Merge(new[] { first.Id, second.Id });
        }
    }
}
