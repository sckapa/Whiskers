using System;

namespace Whiskers.Combatants
{
    // Pure HP arithmetic. Returns the amount actually applied so callers can report exact numbers.
    public sealed class Health
    {
        private const int DeathThreshold = 0;
        private const int MinimumMax = 1;

        // Returned when damage or healing could not be applied at all
        public const int NothingApplied = 0;

        public int Current { get; private set; }
        public int Max { get; private set; }
        public bool IsDead => Current <= DeathThreshold;

        public Health(int max)
        {
            ValidateMax(max);
            Max = max;
            Current = max;
        }

        public int ApplyDamage(int amount)
        {
            ValidateAmount(amount);

            int dealt = Math.Min(amount, Current - DeathThreshold);
            Current -= dealt;
            return dealt;
        }

        // The dead cannot be healed; reviving will be a separate explicit mechanic if it is ever needed
        public int ApplyHealing(int amount)
        {
            ValidateAmount(amount);

            if (IsDead)
            {
                return NothingApplied;
            }

            int healed = Math.Min(amount, Max - Current);
            Current += healed;
            return healed;
        }

        // Raising the maximum also raises current HP by the same amount, lowering it clamps current HP
        public void ChangeMax(int newMax)
        {
            ValidateMax(newMax);

            int increase = newMax - Max;
            Max = newMax;

            if (IsDead)
            {
                return;
            }

            if (increase > 0)
            {
                Current += increase;
            }

            Current = Math.Min(Current, Max);
        }

        private static void ValidateAmount(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cannot be negative.");
            }
        }

        private static void ValidateMax(int max)
        {
            if (max < MinimumMax)
            {
                throw new ArgumentOutOfRangeException(nameof(max), max, $"Max HP must be at least {MinimumMax}.");
            }
        }
    }
}
