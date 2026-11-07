using System;
using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Combatants;
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

        // Board units
        public Vector2 Position { get; }

        public bool IsEmpty => Target == null;

        public DamageReport(BattlefieldObject source, Combatant target, int requested, int dealt, Vector2 position)
        {
            Source = source;
            Target = target;
            Requested = requested;
            Dealt = dealt;
            Position = position;
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

            // TODO: shield, armor
            int dealt = target.TakeDamage(amount);

            DamageReport report = new DamageReport(source, target, amount, dealt, position);
            EventBus.Publish(new InteractionDamageEvent(report));
            return report;
        }
    }
}
