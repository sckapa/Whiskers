using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Effects.Types;

namespace Whiskers.Combat
{
    // An aura that is open on the board right now
    public sealed class ActiveAura
    {
        private readonly HashSet<int> affectedIds = new HashSet<int>();

        public int Id { get; }
        public AuraEffect Effect { get; }
        public CombatSide Owner { get; }

        // The die that opened it; becomes the merged object when that die is merged
        public BattlefieldObject Source { get; internal set; }

        public float Radius => Effect.Radius;

        // Board units
        public Vector2 Center => Source.Position;

        internal ActiveAura(int id, AuraEffect effect, CombatSide owner, BattlefieldObject source)
        {
            Id = id;
            Effect = effect;
            Owner = owner;
            Source = source;
            affectedIds.Add(source.Id);
        }

        public bool Contains(Vector2 boardPosition)
        {
            return Vector2.SqrMagnitude(boardPosition - Center) <= Radius * Radius;
        }

        internal bool HasAffected(int objectId)
        {
            return affectedIds.Contains(objectId);
        }

        internal void MarkAffected(int objectId)
        {
            affectedIds.Add(objectId);
        }
    }

    public readonly struct AuraStartedEvent : IGameEvent
    {
        public ActiveAura Aura { get; }

        public AuraStartedEvent(ActiveAura aura)
        {
            Aura = aura;
        }
    }

    public readonly struct AuraEndedEvent : IGameEvent
    {
        public ActiveAura Aura { get; }

        public AuraEndedEvent(ActiveAura aura)
        {
            Aura = aura;
        }
    }

    // Keeps the open auras of one combat and applies them to objects inside: at opening, when a die lands
    // inside, when an object moves in and when allied objects merge inside. Each object is changed once per aura.
    public sealed class AuraTracker
    {
        private const int MinimumValue = 0;

        private readonly BattlefieldBoard board;
        private readonly List<ActiveAura> auras = new List<ActiveAura>();
        private int nextId;

        public IReadOnlyList<ActiveAura> Auras => auras;

        public AuraTracker(BattlefieldBoard board)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));

            EventBus.Subscribe<BattlefieldObjectRegisteredEvent>(OnRegistered);
            EventBus.Subscribe<BattlefieldObjectMovedEvent>(OnMoved);
            EventBus.Subscribe<BattlefieldObjectsMergedEvent>(OnMerged);
            EventBus.Subscribe<BattlefieldObjectRemovedEvent>(OnRemoved);
        }

        public void Open(AuraEffect effect, CombatSide owner, BattlefieldObject source)
        {
            if (effect == null || source == null)
            {
                return;
            }

            ActiveAura aura = new ActiveAura(nextId++, effect, owner, source);
            auras.Add(aura);
            EventBus.Publish(new AuraStartedEvent(aura));

            // Copy, SetValue publishes events
            foreach (BattlefieldObject candidate in new List<BattlefieldObject>(board.Objects.ActiveObjects))
            {
                TryAffect(aura, candidate);
            }
        }

        // Phase 4
        public void EndRoll()
        {
            Close(aura => aura.Effect.Duration == AuraDuration.ThisRoll);
        }

        public void EndCombat()
        {
            Close(aura => true);

            EventBus.Unsubscribe<BattlefieldObjectRegisteredEvent>(OnRegistered);
            EventBus.Unsubscribe<BattlefieldObjectMovedEvent>(OnMoved);
            EventBus.Unsubscribe<BattlefieldObjectsMergedEvent>(OnMerged);
            EventBus.Unsubscribe<BattlefieldObjectRemovedEvent>(OnRemoved);
        }

        private void OnRegistered(BattlefieldObjectRegisteredEvent registered)
        {
            AffectByAll(registered.Object);
        }

        private void OnMoved(BattlefieldObjectMovedEvent moved)
        {
            AffectByAll(moved.Object);
        }

        // The merged object counts as affected if one of its parts was (no double bonus);
        // an aura following one of the parts now follows the merged object
        private void OnMerged(BattlefieldObjectsMergedEvent merged)
        {
            foreach (ActiveAura aura in auras)
            {
                foreach (BattlefieldObject part in merged.MergedObjects)
                {
                    if (aura.HasAffected(part.Id))
                    {
                        aura.MarkAffected(merged.Result.Id);
                    }

                    if (aura.Source == part)
                    {
                        aura.Source = merged.Result;
                    }
                }
            }

            AffectByAll(merged.Result);
        }

        private void OnRemoved(BattlefieldObjectRemovedEvent removed)
        {
            Close(aura => aura.Source == removed.Object && aura.Effect.Duration == AuraDuration.WhileSourceOnBoard);
        }

        private void AffectByAll(BattlefieldObject battlefieldObject)
        {
            // Copy, an affected value can trigger handlers that open or close auras
            foreach (ActiveAura aura in auras.ToArray())
            {
                TryAffect(aura, battlefieldObject);
            }
        }

        private void TryAffect(ActiveAura aura, BattlefieldObject target)
        {
            if (!target.CanInteract || !target.IsNumerical || aura.HasAffected(target.Id)
                || !IsTarget(aura, target) || !aura.Contains(target.Position))
            {
                return;
            }

            aura.MarkAffected(target.Id);
            board.Objects.SetValue(target.Id, Math.Max(MinimumValue, target.Value + aura.Effect.ValueChange));
        }

        private static bool IsTarget(ActiveAura aura, BattlefieldObject target)
        {
            switch (aura.Effect.Targets)
            {
                case AuraTargets.Allies:
                    return target.Owner == aura.Owner;
                case AuraTargets.Enemies:
                    return target.Owner != aura.Owner;
                default:
                    return true;
            }
        }

        private void Close(Predicate<ActiveAura> shouldClose)
        {
            for (int i = auras.Count - 1; i >= 0; i--)
            {
                ActiveAura aura = auras[i];

                if (shouldClose(aura))
                {
                    auras.RemoveAt(i);
                    EventBus.Publish(new AuraEndedEvent(aura));
                }
            }
        }
    }
}
