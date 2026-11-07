using System;
using UnityEngine;

namespace Whiskers.Dice
{
    // One side of a die: a number or a symbol, optionally printed in a color that adds an effect.
    // Immutable from code: upgrading a face means replacing it with a new instance.
    [Serializable]
    public sealed class DiceFace
    {
        private const int NoValue = 0;

        [SerializeField] private FaceType type;
        [SerializeField] private int value;
        [SerializeField] private FaceSymbol symbol;
        [SerializeField] private DiceColor glyphColor;

        public FaceType Type => type;
        public int Value => value;
        public FaceSymbol Symbol => symbol;

        // Color of the printed number or symbol, carries the additional face effect
        public DiceColor GlyphColor => glyphColor;

        public bool IsNumber => type == FaceType.Number;
        public bool IsSymbol => type == FaceType.Symbol;
        public bool HasGlyphColor => glyphColor != DiceColor.None;

        private DiceFace(FaceType type, int value, FaceSymbol symbol, DiceColor glyphColor)
        {
            this.type = type;
            this.value = value;
            this.symbol = symbol;
            this.glyphColor = glyphColor;
        }

        public static DiceFace CreateNumber(int value, DiceColor glyphColor = DiceColor.None)
        {
            return new DiceFace(FaceType.Number, value, FaceSymbol.None, glyphColor);
        }

        public static DiceFace CreateSymbol(FaceSymbol symbol, DiceColor glyphColor = DiceColor.None)
        {
            if (symbol == FaceSymbol.None)
            {
                throw new ArgumentException("A symbol face needs an actual symbol.", nameof(symbol));
            }

            return new DiceFace(FaceType.Symbol, NoValue, symbol, glyphColor);
        }

        public DiceFace WithGlyphColor(DiceColor newGlyphColor)
        {
            return new DiceFace(type, value, symbol, newGlyphColor);
        }

        public override string ToString()
        {
            string glyph = IsNumber ? value.ToString() : symbol.ToString();
            return HasGlyphColor ? $"{glyph} ({glyphColor})" : glyph;
        }
    }
}
