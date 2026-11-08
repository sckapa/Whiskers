using System;
using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Combatants;
using Whiskers.Combatants.Status;
using Whiskers.Core;
using Whiskers.Core.Events;

namespace Whiskers.Combat.Interactions
{
    public readonly struct DamageReport
    {
        public BattlefieldObject Source { get; }
        public Combatant Target { get; }
        public int Requested { get; }

        public int Dealt { get; }

        // A shield on the target absorbed the whole attack
        public bool WasBlocked { get; }

        // Board units
        public Vector2 Position { get; }

        public bool IsEmpty => Target == null;

        public DamageReport(BattlefieldObject source, Combatant target, int requested, int dealt, Vector2 position, bool wasBlocked = false)
        {
            Source = source;
            Target = target;
            Requested = requested;
            Dealt = dealt;
            Position = position;
            WasBlocked = wasBlocked;
        }

        public static DamageReport None(Vector2 position)
        {
            return new DamageReport(null, null, 0, 0, position);
        }
    }

    public readonly struct InteractionDamageEvent : IGameEvent
    {
        public DamageReport Report { get; }

        public InteractionDamageEvent(DamageReport report)
        {
            Report = report;
        }
    }

    // Published right before interaction damage is applied (InteractionDamageEvent follows right after it)
    public readonly struct InteractionDamageIncomingEvent : IGameEvent
    {
        public BattlefieldObject Source { get; }
        public Combatant Target { get; }
        public int Amount { get; }

        public InteractionDamageIncomingEvent(BattlefieldObject source, Combatant target, int amount)
        {
            Source = source;
            Target = target;
            Amount = amount;
        }
    }

    // Applies damage to the other side right away.
    public sealed class DamageResolver
    {
        private readonly Combatant player;
        private readonly Combatant enemy;

        public DamageResolver(Combatant player, Combatant enemy)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
            this.enemy = enemy ?? throw new ArgumentNullException(nameof(enemy));
        }

        public Combatant GetOpponentOf(CombatSide side)
        {
            return side == CombatSide.Player ? enemy : player;
        }

        public DamageReport Deal(BattlefieldObject source, int amount, Vector2 position)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (amount <= 0)
            {
                return DamageReport.None(position);
            }

            Combatant target = GetOpponentOf(source.Owner);

            EventBus.Publish(new InteractionDamageIncomingEvent(source, target, amount));

            // TODO: armor
            bool blocked = target.Statuses.TryConsume(StatusType.Shield);
            int dealt = blocked ? Health.NothingApplied : target.TakeDamage(amount);

            DamageReport report = new DamageReport(source, target, amount, dealt, position, blocked);
            EventBus.Publish(new InteractionDamageEvent(report));
            return report;
        }
    }
}
