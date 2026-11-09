using System;
using System.Collections.Generic;
using Whiskers.Combatants.Definitions;
using Whiskers.Combatants.Stats;
using Whiskers.Combatants.Status;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Dice;
using Whiskers.Dice.Bag;
using Whiskers.Effects;

namespace Whiskers.Combatants
{
    // Base for the player and enemies
    // TODO: equipment (its effects will be added as passives while equipped)
    public abstract class Combatant
    {
        private readonly List<DiceInstance> ownedDice;
        private readonly List<EffectDefinition> passiveEffects;

        public string DisplayName { get; }

        // Sprite / animator for the combat view, null when created without a definition
        public CombatantVisuals Visuals { get; internal set; }
        public abstract CombatSide Side { get; }
        public Health Health { get; }
        public CombatantStats Stats { get; }
        public CombatantStatuses Statuses { get; }

        // Effects of the combatant itself (passives, later equipment), checked in every effect phase
        public IReadOnlyList<EffectDefinition> PassiveEffects => passiveEffects;
        public bool IsDead => Health.IsDead;

        // Every die the combatant owns during the run
        public IReadOnlyList<DiceInstance> OwnedDice => ownedDice;

        // Exists only while a combat is running
        public DiceBag CombatBag { get; private set; }
        public bool IsInCombat => CombatBag != null;

        protected Combatant(
            string displayName,
            int maxHealth,
            int dicePerRoll,
            IEnumerable<DiceInstance> startingDice,
            IEnumerable<EffectDefinition> passives = null)
        {
            if (startingDice == null)
            {
                throw new ArgumentNullException(nameof(startingDice));
            }

            DisplayName = displayName;
            Health = new Health(maxHealth);
            Stats = new CombatantStats(dicePerRoll);
            Statuses = new CombatantStatuses(this);
            ownedDice = new List<DiceInstance>(startingDice);
            passiveEffects = passives == null ? new List<EffectDefinition>() : new List<EffectDefinition>(passives);
            passiveEffects.RemoveAll(effect => effect == null);
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

        public void AddPassive(EffectDefinition effect)
        {
            passiveEffects.Add(effect != null ? effect : throw new ArgumentNullException(nameof(effect)));
            EventBus.Publish(new CombatantPassivesChangedEvent(this));
        }

        public bool RemovePassive(EffectDefinition effect)
        {
            if (!passiveEffects.Remove(effect))
            {
                return false;
            }

            EventBus.Publish(new CombatantPassivesChangedEvent(this));
            return true;
        }

        // Puts every owned die into a fresh bag, so each combat starts with an empty discard pile.
        // Charges and statuses don't carry over from the previous combat.
        public DiceBag BeginCombat(Random random)
        {
            CombatBag = new DiceBag(Side, ownedDice, random);
            Statuses.Clear();

            foreach (DiceInstance die in ownedDice)
            {
                die.ResetCharges();
            }

            return CombatBag;
        }

        public void EndCombat()
        {
            CombatBag = null;
            Statuses.Clear();
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
