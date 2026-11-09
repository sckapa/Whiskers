using System;
using System.Collections.Generic;
using Whiskers.Core.Events;

namespace Whiskers.Combatants.Status
{
    // Serialized, add new values at the end
    public enum StatusType
    {
        // Each stack blocks one incoming attack completely
        Shield,
        // Deals damage equal to the stacks before every interaction phase, then loses one stack
        Poison
    }

    // Stacked status effects of one combatant. Cleared when a combat starts and ends.
    public sealed class CombatantStatuses
    {
        private const int NoStacks = 0;

        private readonly Dictionary<StatusType, int> stacksByType = new Dictionary<StatusType, int>();
        private readonly Combatant owner;

        public CombatantStatuses(Combatant owner)
        {
            this.owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public int GetStacks(StatusType type)
        {
            return stacksByType.TryGetValue(type, out int stacks) ? stacks : NoStacks;
        }

        public bool Has(StatusType type)
        {
            return GetStacks(type) > NoStacks;
        }

        public void Add(StatusType type, int stacks)
        {
            if (stacks < NoStacks)
            {
                throw new ArgumentOutOfRangeException(nameof(stacks), stacks, "Use TryConsume to remove stacks.");
            }

            SetStacks(type, GetStacks(type) + stacks);
        }

        // False (and nothing removed) if there are not enough stacks
        public bool TryConsume(StatusType type, int stacks = 1)
        {
            if (stacks < NoStacks)
            {
                throw new ArgumentOutOfRangeException(nameof(stacks), stacks, "Stacks cannot be negative.");
            }

            int current = GetStacks(type);

            if (current < stacks)
            {
                return false;
            }

            SetStacks(type, current - stacks);
            return true;
        }

        public void Clear()
        {
            // Copy, SetStacks changes the dictionary
            foreach (StatusType type in new List<StatusType>(stacksByType.Keys))
            {
                SetStacks(type, NoStacks);
            }
        }

        private void SetStacks(StatusType type, int stacks)
        {
            int previous = GetStacks(type);

            if (previous == stacks)
            {
                return;
            }

            if (stacks == NoStacks)
            {
                stacksByType.Remove(type);
            }
            else
            {
                stacksByType[type] = stacks;
            }

            EventBus.Publish(new CombatantStatusChangedEvent(owner, type, previous, stacks));
        }
    }

    public readonly struct CombatantStatusChangedEvent : IGameEvent
    {
        public Combatant Combatant { get; }
        public StatusType Type { get; }
        public int PreviousStacks { get; }
        public int CurrentStacks { get; }

        public CombatantStatusChangedEvent(Combatant combatant, StatusType type, int previousStacks, int currentStacks)
        {
            Combatant = combatant;
            Type = type;
            PreviousStacks = previousStacks;
            CurrentStacks = currentStacks;
        }
    }
}
