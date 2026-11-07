using System;
using System.Collections.Generic;
using Whiskers.Combatants.Stats;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Dice;
using Whiskers.Dice.Bag;

namespace Whiskers.Combatants
{
    // Base for the player and enemies
    // TODO: equipment, passives, status effects
    public abstract class Combatant
    {
        private readonly List<DiceInstance> ownedDice;

        public string DisplayName { get; }
        public abstract CombatSide Side { get; }
        public Health Health { get; }
        public CombatantStats Stats { get; }
        public bool IsDead => Health.IsDead;

        // Every die the combatant owns during the run
        public IReadOnlyList<DiceInstance> OwnedDice => ownedDice;

        // Exists only while a combat is running
        public DiceBag CombatBag { get; private set; }
        public bool IsInCombat => CombatBag != null;

        protected Combatant(string displayName, int maxHealth, int dicePerRoll, IEnumerable<DiceInstance> startingDice)
        {
            if (startingDice == null)
            {
                throw new ArgumentNullException(nameof(startingDice));
            }

            DisplayName = displayName;
            Health = new Health(maxHealth);
            Stats = new CombatantStats(dicePerRoll);
            ownedDice = new List<DiceInstance>(startingDice);
        }

        public int TakeDamage(int amount)
        {
            if (IsDead)
            {
                return Health.NothingApplied;
            }

            int previousHealth = Health.Current;
            int dealt = Health.ApplyDamage(amount);

            PublishHealthChange(previousHealth);

            if (IsDead)
            {
                EventBus.Publish(new CombatantDiedEvent(this));
            }

            return dealt;
        }

        public int Heal(int amount)
        {
            int previousHealth = Health.Current;
            int healed = Health.ApplyHealing(amount);

            PublishHealthChange(previousHealth);
            return healed;
        }

        public void ChangeMaxHealth(int newMax)
        {
            int previousHealth = Health.Current;
            Health.ChangeMax(newMax);

            PublishHealthChange(previousHealth);
        }

        // A die gained mid-combat becomes drawable right away
        public void AddDice(DiceInstance die)
        {
            ownedDice.Add(die ?? throw new ArgumentNullException(nameof(die)));
            CombatBag?.AddToBag(die);
            EventBus.Publish(new CombatantDiceCollectionChangedEvent(this));
        }

        // Takes effect from the next combat; the current combat bag is left untouched
        public bool RemoveDice(DiceInstance die)
        {
            if (!ownedDice.Remove(die))
            {
                return false;
            }

            EventBus.Publish(new CombatantDiceCollectionChangedEvent(this));
            return true;
        }

        // Puts every owned die into a fresh bag, so each combat starts with an empty discard pile
        public DiceBag BeginCombat(Random random)
        {
            CombatBag = new DiceBag(Side, ownedDice, random);
            return CombatBag;
        }

        public void EndCombat()
        {
            CombatBag = null;
        }

        public IReadOnlyList<DiceInstance> DrawForRoll()
        {
            if (!IsInCombat)
            {
                throw new InvalidOperationException($"{DisplayName} cannot draw dice outside of combat.");
            }

            return CombatBag.Draw(Stats.DicePerRoll.Value);
        }

        public override string ToString()
        {
            return $"{DisplayName} [{Side}] {Health.Current}/{Health.Max} HP";
        }

        private void PublishHealthChange(int previousHealth)
        {
            if (previousHealth != Health.Current)
            {
                EventBus.Publish(new CombatantHealthChangedEvent(this, previousHealth, Health.Current));
            }
        }
    }
}
