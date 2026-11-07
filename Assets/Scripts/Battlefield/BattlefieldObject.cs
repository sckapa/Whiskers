using System.Collections.Generic;
using UnityEngine;
using Whiskers.Core;
using Whiskers.Dice;

namespace Whiskers.Battlefield
{
    // Rolled die, merged dice or summon on the board.
    // Changed only through BattlefieldObjectRegistry.
    public sealed class BattlefieldObject
    {
        private readonly List<DiceFace> faces;
        private readonly List<int> sourceDiceIds;

        public int Id { get; }
        public CombatSide Owner { get; }
        public BattlefieldObjectType Type { get; }

        // Board units
        public Vector2 Position { get; internal set; }
        public BattlefieldObjectState State { get; internal set; }

        // Can change during interactions
        public int Value { get; internal set; }

        // Rolled faces of all dice in the object
        public IReadOnlyList<DiceFace> Faces => faces;

        // DiceInstance ids this object was made from
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

        public bool IsNumerical
        {
            get
            {
                foreach (DiceFace face in faces)
                {
                    if (face.IsNumber)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool HasGlyphColor(DiceColor color)
        {
            foreach (DiceFace face in faces)
            {
                if (face.GlyphColor == color)
                {
                    return true;
                }
            }

            return false;
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
