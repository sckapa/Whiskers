using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Combatants;
using Whiskers.Combatants.Stats;
using Whiskers.Combatants.Status;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Dice;
using Whiskers.Dice.Rolling;
using Whiskers.Effects;
using Whiskers.Effects.Types;

namespace Whiskers.Combat
{
    // Runs the effects of combat phases 1 (landing), 2 (start of combat) and 4 (end of roll) for one combat.
    // Order for every die: body color, rolled symbol, glyph color, then the owner's passives / equipment.
    // Player first, then enemy, so the same board always gives the same order.
    public sealed class CombatEffectRunner : IEffectServices
    {
        private const int PoisonStacksLostPerTick = 1;
        private const int NoPoison = 0;
        private const int NeverExpires = int.MaxValue;
        private const int NextRollOffset = 1;

        private readonly PlayerCombatant player;
        private readonly EnemyCombatant enemy;
        private readonly BattlefieldBoard board;
        private readonly DiceEffectTable table;
        private readonly System.Random random;
        private readonly int maxAdditionalDicePerRoll;

        private readonly List<DieRollResult> rollResults = new List<DieRollResult>();
        private readonly List<EffectDefinition> collectedEffects = new List<EffectDefinition>();
        private readonly Dictionary<Combatant, int> requestedRolls = new Dictionary<Combatant, int>();
        private readonly Dictionary<int, IReadOnlyList<DieRollResult>> settledThrows = new Dictionary<int, IReadOnlyList<DieRollResult>>();
        private readonly HashSet<int> awaitedThrowIds = new HashSet<int>();
        private readonly List<TimedModifier> timedModifiers = new List<TimedModifier>();
        private readonly HashSet<OncePerCombatKey> appliedOncePerCombat = new HashSet<OncePerCombatKey>();

        private readonly AuraTracker auras;

        private int rollNumber;
        private int additionalDiceThisRoll;
        private bool combatStartDone;
        private bool isRunningLanding;
        private bool isDisposed;

        // Domains open on the board right now
        public IReadOnlyList<ActiveAura> Auras => auras.Auras;

        // Every die that landed this roll, extra rolls included
        public IReadOnlyList<DieRollResult> RollResults => rollResults;

        private bool IsCombatOver => isDisposed || player.IsDead || enemy.IsDead;

        // table can be null: then only passives have effects
        public CombatEffectRunner(
            PlayerCombatant player,
            EnemyCombatant enemy,
            BattlefieldBoard board,
            DiceEffectTable table,
            System.Random random,
            int maxAdditionalDicePerRoll)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            this.table = table;
            this.maxAdditionalDicePerRoll = maxAdditionalDicePerRoll;
            auras = new AuraTracker(board);

            EventBus.Subscribe<DiceThrowSettledEvent>(OnThrowSettled);
        }

        // Phase 1. Extra rolls requested by effects are thrown after the current dice and processed the same way.
        public IEnumerator RunLandingPhase(int roll, IReadOnlyList<DieRollResult> roundResults)
        {
            rollNumber = roll;
            additionalDiceThisRoll = 0;
            rollResults.Clear();
            requestedRolls.Clear();
            ExpireModifiers();

            EventBus.Publish(new EffectPhaseStartedEvent(EffectTrigger.Landing, rollNumber));

            List<DieRollResult> batch = new List<DieRollResult>(roundResults);
            isRunningLanding = true;

            while (batch.Count > 0 && !IsCombatOver)
            {
                rollResults.AddRange(batch);

                foreach (DieRollResult result in batch)
                {
                    ApplyLandingEffects(result);
                }

                List<int> throwIds = ThrowRequestedDice();

                if (throwIds.Count == 0 || IsCombatOver)
                {
                    break;
                }

                yield return new WaitUntil(() => IsCombatOver || throwIds.TrueForAll(settledThrows.ContainsKey));

                batch = CollectSettledResults(throwIds);
            }

            isRunningLanding = false;
        }

        // Phase 2: statuses tick, start-of-combat effects (first roll only), then the effects before every interaction phase
        public void RunStartPhase()
        {
            TickStatuses(player);
            TickStatuses(enemy);

            if (!combatStartDone)
            {
                combatStartDone = true;
                RunTrigger(EffectTrigger.CombatStart);
            }

            RunTrigger(EffectTrigger.BeforeInteractions);
        }

        // Phase 4, after the leftover dice dealt their damage
        public void RunEndOfRollPhase()
        {
            RunTrigger(EffectTrigger.EndOfRoll);
            auras.EndRoll();
        }

        // Removes everything this combat granted. The runner is not used afterwards.
        public void EndCombat()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            EventBus.Unsubscribe<DiceThrowSettledEvent>(OnThrowSettled);
            auras.EndCombat();

            foreach (TimedModifier modifier in timedModifiers)
            {
                modifier.Remove();
            }

            timedModifiers.Clear();
        }

        public void RequestAdditionalRolls(Combatant roller, int count)
        {
            if (roller == null || count <= 0)
            {
                return;
            }

            // Only landing effects can roll more dice
            if (!isRunningLanding)
            {
                return;
            }

            requestedRolls.TryGetValue(roller, out int alreadyRequested);
            requestedRolls[roller] = alreadyRequested + count;
        }

        public void AddTimedModifier(Stat stat, int amount, EffectDuration duration)
        {
            if (stat == null)
            {
                throw new ArgumentNullException(nameof(stat));
            }

            int expiresAfterRoll = duration == EffectDuration.NextRoll ? rollNumber + NextRollOffset : NeverExpires;
            TimedModifier modifier = new TimedModifier(stat, expiresAfterRoll);

            stat.AddModifier(new StatModifier(amount, modifier));
            timedModifiers.Add(modifier);
        }

        public void ActivateAura(AuraEffect aura, EffectContext context)
        {
            if (aura != null && !IsCombatOver)
            {
                auras.Open(aura, context.Owner.Side, context.SourceObject);
            }
        }

        private void ApplyLandingEffects(DieRollResult result)
        {
            if (IsCombatOver)
            {
                return;
            }

            Combatant owner = GetCombatant(result.Owner);
            board.TryGetObjectForDie(result.Die.Id, out BattlefieldObject dieObject);

            EffectContext context = CreateContext(EffectTrigger.Landing, owner, result.Die, result.Face, dieObject);
            ApplyDieEffects(context);
            ApplyPassives(context);
        }

        private void RunTrigger(EffectTrigger trigger)
        {
            if (IsCombatOver)
            {
                return;
            }

            EventBus.Publish(new EffectPhaseStartedEvent(trigger, rollNumber));

            // Copy, effects can't add dice here but stay safe if that changes
            foreach (DieRollResult result in new List<DieRollResult>(rollResults))
            {
                board.TryGetObjectForDie(result.Die.Id, out BattlefieldObject dieObject);
                ApplyDieEffects(CreateContext(trigger, GetCombatant(result.Owner), result.Die, result.Face, dieObject));
            }

            ApplyPassives(CreateContext(trigger, player, null, null, null));
            ApplyPassives(CreateContext(trigger, enemy, null, null, null));
        }

        private void ApplyDieEffects(EffectContext context)
        {
            if (table == null || !context.HasDie)
            {
                return;
            }

            collectedEffects.Clear();
            table.CollectEffects(context.Die, context.RolledFace, collectedEffects);

            // Copy, a nested call (e.g. an effect publishing an event someone reacts to) could reuse the list
            foreach (EffectDefinition effect in collectedEffects.ToArray())
            {
                TryApply(effect, context, false);
            }
        }

        private void ApplyPassives(EffectContext context)
        {
            // Copy, an effect may add or remove passives
            foreach (EffectDefinition passive in new List<EffectDefinition>(context.Owner.PassiveEffects))
            {
                TryApply(passive, context, true);
            }
        }

        private void TryApply(EffectDefinition effect, EffectContext context, bool fromPassive)
        {
            if (effect == null || IsCombatOver || !effect.CanApply(context))
            {
                return;
            }

            if (effect.OncePerCombat && !appliedOncePerCombat.Add(new OncePerCombatKey(effect, context.Owner.Side)))
            {
                return;
            }

            effect.Apply(context);

            EventBus.Publish(new EffectAppliedEvent(effect, context, fromPassive));
        }

        private void TickStatuses(Combatant combatant)
        {
            if (IsCombatOver)
            {
                return;
            }

            int poison = combatant.Statuses.GetStacks(StatusType.Poison);

            if (poison <= NoPoison)
            {
                return;
            }

            combatant.Statuses.TryConsume(StatusType.Poison, PoisonStacksLostPerTick);
            int dealt = combatant.TakeDamage(poison);
            EventBus.Publish(new StatusTickedEvent(combatant, StatusType.Poison, dealt));
        }

        // Player first. Stops at the per-roll limit and when a bag has nothing left to draw.
        private List<int> ThrowRequestedDice()
        {
            List<int> throwIds = new List<int>();

            foreach (Combatant roller in new Combatant[] { player, enemy })
            {
                if (!requestedRolls.TryGetValue(roller, out int requested))
                {
                    continue;
                }

                List<DiceInstance> dice = DrawAdditionalDice(roller, requested);

                if (dice.Count > 0)
                {
                    int throwId = DiceRollStage.Instance.Round.ThrowAdditional(dice, roller.Side);
                    awaitedThrowIds.Add(throwId);
                    throwIds.Add(throwId);
                }
            }

            requestedRolls.Clear();
            return throwIds;
        }

        private List<DiceInstance> DrawAdditionalDice(Combatant roller, int requested)
        {
            int allowed = Math.Min(requested, maxAdditionalDicePerRoll - additionalDiceThisRoll);
            List<DiceInstance> dice = new List<DiceInstance>();

            while (dice.Count < allowed && roller.CombatBag != null && roller.CombatBag.TryDrawOne(out DiceInstance die))
            {
                dice.Add(die);
            }

            additionalDiceThisRoll += dice.Count;
            return dice;
        }

        private List<DieRollResult> CollectSettledResults(List<int> throwIds)
        {
            List<DieRollResult> collected = new List<DieRollResult>();

            foreach (int throwId in throwIds)
            {
                if (settledThrows.TryGetValue(throwId, out IReadOnlyList<DieRollResult> results))
                {
                    collected.AddRange(results);
                    settledThrows.Remove(throwId);
                }
            }

            return collected;
        }

        private void OnThrowSettled(DiceThrowSettledEvent settled)
        {
            if (awaitedThrowIds.Remove(settled.ThrowId))
            {
                settledThrows[settled.ThrowId] = settled.Results;
            }
        }

        private void ExpireModifiers()
        {
            for (int i = timedModifiers.Count - 1; i >= 0; i--)
            {
                if (timedModifiers[i].ExpiresAfterRoll <= rollNumber)
                {
                    timedModifiers[i].Remove();
                    timedModifiers.RemoveAt(i);
                }
            }
        }

        private EffectContext CreateContext(EffectTrigger trigger, Combatant owner, DiceInstance die, DiceFace face, BattlefieldObject dieObject)
        {
            return new EffectContext(trigger, owner, GetOpponent(owner), board, die, face, dieObject, rollResults, random, this);
        }

        private Combatant GetCombatant(CombatSide side)
        {
            return side == CombatSide.Player ? (Combatant)player : enemy;
        }

        private Combatant GetOpponent(Combatant combatant)
        {
            return combatant == player ? (Combatant)enemy : player;
        }

        // Also the modifier's source, so exactly its own bonus is removed
        private sealed class TimedModifier
        {
            private readonly Stat stat;

            public int ExpiresAfterRoll { get; }

            public TimedModifier(Stat stat, int expiresAfterRoll)
            {
                this.stat = stat;
                ExpiresAfterRoll = expiresAfterRoll;
            }

            public void Remove()
            {
                stat.RemoveModifiersFrom(this);
            }
        }

        private readonly struct OncePerCombatKey : IEquatable<OncePerCombatKey>
        {
            private readonly EffectDefinition effect;
            private readonly CombatSide side;

            public OncePerCombatKey(EffectDefinition effect, CombatSide side)
            {
                this.effect = effect;
                this.side = side;
            }

            public bool Equals(OncePerCombatKey other)
            {
                return ReferenceEquals(effect, other.effect) && side == other.side;
            }

            public override bool Equals(object obj)
            {
                return obj is OncePerCombatKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    const int HashMultiplier = 397;
                    return (effect.GetInstanceID() * HashMultiplier) ^ (int)side;
                }
            }
        }
    }
}
