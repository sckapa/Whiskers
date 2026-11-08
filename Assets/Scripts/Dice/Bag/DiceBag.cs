using System;
using System.Collections.Generic;
using Whiskers.Core;
using Whiskers.Core.Events;

namespace Whiskers.Dice.Bag
{
    // Works like a deck: bag -> in play -> discard.
    // Random draw, the discard goes back in when the bag is empty.
    public sealed class DiceBag
    {
        private readonly List<DiceInstance> bag = new List<DiceInstance>();
        private readonly List<DiceInstance> inPlay = new List<DiceInstance>();
        private readonly List<DiceInstance> discardPile = new List<DiceInstance>();
        private readonly Random random;

        public CombatSide Owner { get; }

        public IReadOnlyList<DiceInstance> BagContents => bag;
        public IReadOnlyList<DiceInstance> InPlay => inPlay;
        public IReadOnlyList<DiceInstance> DiscardPile => discardPile;

        public int TotalCount => bag.Count + inPlay.Count + discardPile.Count;
        public bool CanDraw => bag.Count > 0 || discardPile.Count > 0;

        // Random is injected so seeded runs can reproduce the same draws
        public DiceBag(CombatSide owner, IEnumerable<DiceInstance> dice, Random random)
        {
            if (dice == null)
            {
                throw new ArgumentNullException(nameof(dice));
            }

            Owner = owner;
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            bag.AddRange(dice);
        }

        // Draws up to the requested amount for a roll. Returns fewer dice only when bag and discard are both empty.
        public IReadOnlyList<DiceInstance> Draw(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), count, "Draw count cannot be negative.");
            }

            List<DiceInstance> drawn = new List<DiceInstance>(count);

            while (drawn.Count < count && TryTakeRandomFromBag(out DiceInstance die))
            {
                drawn.Add(die);
            }

            inPlay.AddRange(drawn);
            EventBus.Publish(new DiceDrawnEvent(Owner, drawn, bag.Count, discardPile.Count));
            return drawn;
        }

        // Extra rolls
        public bool TryDrawOne(out DiceInstance die)
        {
            const int singleDie = 1;

            IReadOnlyList<DiceInstance> drawn = Draw(singleDie);
            die = drawn.Count > 0 ? drawn[0] : null;
            return die != null;
        }

        // Moves a die that has left the battlefield into the discard pile
        public void Discard(DiceInstance die)
        {
            if (!inPlay.Remove(die))
            {
                throw new InvalidOperationException($"{die} is not in play and cannot be discarded.");
            }

            discardPile.Add(die);
            EventBus.Publish(new DiceDiscardedEvent(Owner, die, discardPile.Count));
        }

        // A die gained during combat goes straight into the bag
        public void AddToBag(DiceInstance die)
        {
            bag.Add(die ?? throw new ArgumentNullException(nameof(die)));
        }

        // Gathers every die back into the bag, called at the start of each combat
        public void Reset()
        {
            bag.AddRange(inPlay);
            bag.AddRange(discardPile);
            inPlay.Clear();
            discardPile.Clear();
        }

        private bool TryTakeRandomFromBag(out DiceInstance die)
        {
            if (bag.Count == 0)
            {
                RefillBagFromDiscard();
            }

            if (bag.Count == 0)
            {
                die = null;
                return false;
            }

            // Swap with the last element and remove it, which avoids shifting the list
            int pickedIndex = random.Next(bag.Count);
            int lastIndex = bag.Count - 1;
            die = bag[pickedIndex];
            bag[pickedIndex] = bag[lastIndex];
            bag.RemoveAt(lastIndex);
            return true;
        }

        private void RefillBagFromDiscard()
        {
            if (discardPile.Count == 0)
            {
                return;
            }

            int returnedCount = discardPile.Count;
            bag.AddRange(discardPile);
            discardPile.Clear();
            EventBus.Publish(new DiscardReturnedToBagEvent(Owner, returnedCount));
        }
    }
}
