using System;
using System.Collections.Generic;
using Whiskers.Core.Events;

namespace Whiskers.Dice
{
    // Die owned during a run, shops and events can change it
    public sealed class DiceInstance
    {
        private const int NoCharges = 0;

        private readonly List<DiceFace> faces;

        public int Id { get; }
        public string DisplayName { get; }
        public DiceSize Size { get; }
        public IReadOnlyList<DiceFace> Faces => faces;

        // Color of the whole die, carries its inherent effect
        public DiceColor BodyColor { get; private set; }

        public DiceFace CapFace { get; private set; }

        // Stored by charging effects during a combat, reset when a combat starts
        public int Charges { get; private set; }

        public DiceInstance(
            int id,
            string displayName,
            DiceSize size,
            IEnumerable<DiceFace> faces,
            DiceColor bodyColor = DiceColor.None,
            DiceFace capFace = null)
        {
            if (faces == null)
            {
                throw new ArgumentNullException(nameof(faces));
            }

            this.faces = new List<DiceFace>(faces);

            if (this.faces.Count != size.GetFaceCount())
            {
                throw new ArgumentException($"{size} needs {size.GetFaceCount()} faces, got {this.faces.Count}.", nameof(faces));
            }

            Id = id;
            DisplayName = displayName;
            Size = size;
            BodyColor = bodyColor;
            CapFace = capFace ?? DiceFaceSets.CreateDefaultCapFace();
        }

        public static DiceInstance FromDefinition(int id, DiceDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return new DiceInstance(id, definition.DisplayName, definition.Size, definition.Faces, definition.BodyColor, definition.CapFace);
        }

        public DiceFace GetFace(int faceIndex)
        {
            return faces[faceIndex];
        }

        public void Repaint(DiceColor newBodyColor)
        {
            BodyColor = newBodyColor;
        }

        public void ReplaceFace(int faceIndex, DiceFace newFace)
        {
            faces[faceIndex] = newFace ?? throw new ArgumentNullException(nameof(newFace));
        }

        public void ReplaceCapFace(DiceFace newCapFace)
        {
            CapFace = newCapFace ?? throw new ArgumentNullException(nameof(newCapFace));
        }

        public void AddCharges(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Use TryConsumeCharges to remove charges.");
            }

            SetCharges(Charges + amount);
        }

        public bool TryConsumeCharges(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount cannot be negative.");
            }

            if (Charges < amount)
            {
                return false;
            }

            SetCharges(Charges - amount);
            return true;
        }

        public void ResetCharges()
        {
            SetCharges(NoCharges);
        }

        public override string ToString()
        {
            return $"{DisplayName} #{Id} ({Size})";
        }

        private void SetCharges(int charges)
        {
            if (charges == Charges)
            {
                return;
            }

            int previous = Charges;
            Charges = charges;
            EventBus.Publish(new DieChargesChangedEvent(this, previous, charges));
        }
    }

    public readonly struct DieChargesChangedEvent : IGameEvent
    {
        public DiceInstance Die { get; }
        public int PreviousCharges { get; }
        public int CurrentCharges { get; }

        public DieChargesChangedEvent(DiceInstance die, int previousCharges, int currentCharges)
        {
            Die = die;
            PreviousCharges = previousCharges;
            CurrentCharges = currentCharges;
        }
    }
}
