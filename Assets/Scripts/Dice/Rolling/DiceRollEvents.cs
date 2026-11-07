using System.Collections.Generic;
using UnityEngine;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Dice.Geometry;

namespace Whiskers.Dice.Rolling
{
    public readonly struct DieRollResult
    {
        public DiceInstance Die { get; }
        public CombatSide Owner { get; }
        public DieLanding Landing { get; }

        // CapFace if a D9 landed on a cap
        public DiceFace Face { get; }

        // 0..1 in the tray
        public Vector2 NormalizedPosition { get; }

        public DieRollResult(DiceInstance die, CombatSide owner, DieLanding landing, DiceFace face, Vector2 normalizedPosition)
        {
            Die = die;
            Owner = owner;
            Landing = landing;
            Face = face;
            NormalizedPosition = normalizedPosition;
        }
    }

    // Once per die
    public readonly struct DieSettledEvent : IGameEvent
    {
        public DieRollResult Result { get; }

        public DieSettledEvent(DieRollResult result)
        {
            Result = result;
        }
    }

    public readonly struct DiceThrowSettledEvent : IGameEvent
    {
        public int ThrowId { get; }
        public CombatSide Owner { get; }
        public IReadOnlyList<DieRollResult> Results { get; }

        public DiceThrowSettledEvent(int throwId, CombatSide owner, IReadOnlyList<DieRollResult> results)
        {
            ThrowId = throwId;
            Owner = owner;
            Results = results;
        }
    }
}
