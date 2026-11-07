using System;
using System.Collections.Generic;

namespace Whiskers.Combatants.Stats
{
    // Integer stat made of a base value plus modifiers, never dropping below its minimum
    public sealed class Stat
    {
        private readonly List<StatModifier> modifiers = new List<StatModifier>();

        public int BaseValue { get; }
        public int MinValue { get; }
        public IReadOnlyList<StatModifier> Modifiers => modifiers;

        public int Value
        {
            get
            {
                int total = BaseValue;

                foreach (StatModifier modifier in modifiers)
                {
                    total += modifier.Amount;
                }

                return Math.Max(total, MinValue);
            }
        }

        public Stat(int baseValue, int minValue)
        {
            if (baseValue < minValue)
            {
                throw new ArgumentOutOfRangeException(nameof(baseValue), baseValue, $"Base value cannot be below {minValue}.");
            }

            BaseValue = baseValue;
            MinValue = minValue;
        }

        public void AddModifier(StatModifier modifier)
        {
            if (modifier.Source == null)
            {
                throw new ArgumentException("Modifier needs a source to be removable later.", nameof(modifier));
            }

            modifiers.Add(modifier);
        }

        public int RemoveModifiersFrom(object source)
        {
            return modifiers.RemoveAll(modifier => ReferenceEquals(modifier.Source, source));
        }
    }
}
