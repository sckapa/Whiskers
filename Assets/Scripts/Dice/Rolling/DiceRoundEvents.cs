using System.Collections.Generic;
using UnityEngine;
using Whiskers.Core;
using Whiskers.Core.Events;

namespace Whiskers.Dice.Rolling
{
    public readonly struct PlayerThrowAimStartedEvent : IGameEvent
    {
    }

    public readonly struct PlayerThrowAimCanceledEvent : IGameEvent
    {
    }

    // Normalized tray space
    public readonly struct PlayerThrowReleasedEvent : IGameEvent
    {
        public Vector2 Direction { get; }
        public float Speed { get; }

        public PlayerThrowReleasedEvent(Vector2 direction, float speed)
        {
            Direction = direction;
            Speed = speed;
        }
    }

    public readonly struct DiceHandReleasedEvent : IGameEvent
    {
        public CombatSide Owner { get; }

        public DiceHandReleasedEvent(CombatSide owner)
        {
            Owner = owner;
        }
    }

    public readonly struct DieCollidedEvent : IGameEvent
    {
        public float ImpactSpeed { get; }

        public bool HitAnotherDie { get; }

        public DieCollidedEvent(float impactSpeed, bool hitAnotherDie)
        {
            ImpactSpeed = impactSpeed;
            HitAnotherDie = hitAnotherDie;
        }
    }

    public readonly struct DiceRoundStartedEvent : IGameEvent
    {
    }

    public readonly struct DiceRoundSettledEvent : IGameEvent
    {
        public IReadOnlyList<DieRollResult> Results { get; }

        public DiceRoundSettledEvent(IReadOnlyList<DieRollResult> results)
        {
            Results = results;
        }
    }
}
