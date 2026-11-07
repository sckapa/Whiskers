using System.Collections.Generic;
using UnityEngine;
using Whiskers.Core;
using Whiskers.Dice;

namespace Whiskers.Battlefield
{
    // Logical participant of battlefield interactions: a rolled die, a combination of dice or a summon.
    // Lives independently of any visual GameObject; views follow it, never the other way round.
    // Only BattlefieldObjectRegistry mutates it, so every change is announced through events.
    public sealed class BattlefieldObject
    {
        private readonly List<DiceFace> faces;
        private readonly List<int> sourceDiceIds;

        public int Id { get; }
        public CombatSide Owner { get; }
        public BattlefieldObjectType Type { get; }

        // Position on the board in logical board units
        public Vector2 Position { get; internal set; }
        public BattlefieldObjectState State { get; internal set; }

        // Current numerical strength. Changes during interactions, e.g. after a subtraction.
        public int Value { get; internal set; }

        // Rolled faces carried by this object. A combined object keeps the faces of all its parts.
        public IReadOnlyList<DiceFace> Faces => faces;

        // Ids of DiceInstance objects this object came from
        public IReadOnlyList<int> SourceDiceIds => sourceDiceIds;

        public bool CanInteract => State == BattlefieldObjectState.Active || State == BattlefieldObjectState.AwaitingInteraction;

        internal BattlefieldObject(
            int id,
            CombatSide owner,
            BattlefieldObjectType type,
            Vector2 position,
            int value,
            IEnumerable<DiceFace> faces,
            IEnumerable<int> sourceDiceIds)
        {
            Id = id;
            Owner = owner;
            Type = type;
            Position = position;
            Value = value;
            State = BattlefieldObjectState.Active;
            this.faces = new List<DiceFace>(faces);
            this.sourceDiceIds = new List<int>(sourceDiceIds);
        }

        public bool IsAlliedWith(BattlefieldObject other)
        {
            return other != null && other.Owner == Owner;
        }

        public bool IsOpposing(BattlefieldObject other)
        {
            return other != null && other.Owner != Owner;
        }

        public override string ToString()
        {
            return $"{Type} #{Id} [{Owner}] value {Value} at {Position} ({State})";
        }
    }
}
