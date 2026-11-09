using Whiskers.Battlefield;

namespace Whiskers.Combat.Interactions
{
    // No winner = both destroyed
    public readonly struct CollisionDecision
    {
        public BattlefieldObject Winner { get; }

        public int RemainingValue { get; }

        public int Damage { get; }

        public bool HasWinner => Winner != null;

        private CollisionDecision(BattlefieldObject winner, int remainingValue, int damage)
        {
            Winner = winner;
            RemainingValue = remainingValue;
            Damage = damage;
        }

        public static CollisionDecision Win(BattlefieldObject winner, int remainingValue, int damage)
        {
            return new CollisionDecision(winner, remainingValue, damage);
        }

        public static CollisionDecision BothDestroyed()
        {
            return new CollisionDecision(null, 0, 0);
        }
    }

    // For symbols and special cases. The first rule that returns true decides.
    public interface ICollisionRule
    {
        bool TryDecide(BattlefieldObject first, BattlefieldObject second, out CollisionDecision decision);
    }

    // Bigger minus smaller, the rest is damage. Equal values destroy both.
    // TODO: symbol rules, symbols count as 0 for now
    public sealed class SubtractionCollisionRule : ICollisionRule
    {
        public bool TryDecide(BattlefieldObject first, BattlefieldObject second, out CollisionDecision decision)
        {
            int difference = first.Value - second.Value;

            if (difference == 0)
            {
                decision = CollisionDecision.BothDestroyed();
            }
            else if (difference > 0)
            {
                decision = CollisionDecision.Win(first, difference, difference);
            }
            else
            {
                decision = CollisionDecision.Win(second, -difference, -difference);
            }

            return true;
        }
    }
}
