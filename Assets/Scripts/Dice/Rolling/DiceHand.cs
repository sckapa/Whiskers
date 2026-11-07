using System.Collections.Generic;
using Whiskers.Core;

namespace Whiskers.Dice.Rolling
{
    public sealed class DiceHand
    {
        private readonly List<RollingDie> dice;

        public CombatSide Owner { get; }
        public IReadOnlyList<RollingDie> Dice => dice;
        public bool IsReleased { get; private set; }

        // Set on release
        public int ThrowId { get; private set; }

        internal DiceHand(CombatSide owner, List<RollingDie> dice)
        {
            Owner = owner;
            this.dice = dice;
        }

        public void SetJiggling(bool jiggling)
        {
            if (IsReleased)
            {
                return;
            }

            foreach (RollingDie die in dice)
            {
                die.SetJiggling(jiggling);
            }
        }

        internal void MarkReleased(int throwId)
        {
            IsReleased = true;
            ThrowId = throwId;
        }
    }
}
