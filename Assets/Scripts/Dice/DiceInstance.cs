using System;
using System.Collections.Generic;

namespace Whiskers.Dice
{
    // A concrete die owned by a combatant during a run. Holds its own copy of faces and body color
    // so shops and events can upgrade, replace or repaint them.
    public sealed class DiceInstance
    {
        private readonly List<DiceFace> faces;

        public int Id { get; }
        public string DisplayName { get; }
        public DiceSize Size { get; }
        public IReadOnlyList<DiceFace> Faces => faces;

        // Color of the whole die, carries its inherent effect
        public DiceColor BodyColor { get; private set; }

        public DiceInstance(int id, string displayName, DiceSize size, IEnumerable<DiceFace> faces, DiceColor bodyColor = DiceColor.None)
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
        }

        public static DiceInstance FromDefinition(int id, DiceDefinition definition)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            return new DiceInstance(id, definition.DisplayName, definition.Size, definition.Faces, definition.BodyColor);
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

        public override string ToString()
        {
            return $"{DisplayName} #{Id} ({Size})";
        }
    }
}
