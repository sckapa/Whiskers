using System.Collections.Generic;
using Whiskers.Core;
using Whiskers.Core.Events;

namespace Whiskers.Dice.Bag
{
    // Events for the bag UI and sounds, carrying counts so listeners do not need a reference to the bag
    // TODO: bag UI

    public readonly struct DiceDrawnEvent : IGameEvent
    {
        public CombatSide Owner { get; }
        public IReadOnlyList<DiceInstance> DrawnDice { get; }
        public int RemainingInBag { get; }
        public int DiscardCount { get; }

        public DiceDrawnEvent(CombatSide owner, IReadOnlyList<DiceInstance> drawnDice, int remainingInBag, int discardCount)
        {
            Owner = owner;
            DrawnDice = drawnDice;
            RemainingInBag = remainingInBag;
            DiscardCount = discardCount;
        }
    }

    public readonly struct DiceDiscardedEvent : IGameEvent
    {
        public CombatSide Owner { get; }
        public DiceInstance Die { get; }
        public int DiscardCount { get; }

        public DiceDiscardedEvent(CombatSide owner, DiceInstance die, int discardCount)
        {
            Owner = owner;
            Die = die;
            DiscardCount = discardCount;
        }
    }

    public readonly struct DiscardReturnedToBagEvent : IGameEvent
    {
        public CombatSide Owner { get; }
        public int ReturnedCount { get; }

        public DiscardReturnedToBagEvent(CombatSide owner, int returnedCount)
        {
            Owner = owner;
            ReturnedCount = returnedCount;
        }
    }
}
