using System;

namespace Whiskers.Combatants
{
    // Damage and healing return the amount actually applied
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

        // Dead combatants can't be healed
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

        // Raising max heals by the difference
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
