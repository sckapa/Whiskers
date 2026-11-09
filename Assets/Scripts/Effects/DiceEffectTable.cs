using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Dice;

namespace Whiskers.Effects
{
    // Which effects a die carries: by its body color, by the glyph color of the rolled face and by the rolled symbol.
    // One table for the whole game, so a color always means the same thing.
    [CreateAssetMenu(fileName = "DiceEffectTable", menuName = "Whiskers/Effects/Dice Effect Table")]
    public sealed class DiceEffectTable : ScriptableObject
    {
        [Serializable]
        private sealed class ColorEntry
        {
            public DiceColor color = DiceColor.None;
            [Tooltip("The whole die is painted in this color")]
            public List<EffectDefinition> bodyEffects = new List<EffectDefinition>();
            [Tooltip("The rolled number or symbol is printed in this color")]
            public List<EffectDefinition> glyphEffects = new List<EffectDefinition>();
        }

        [Serializable]
        private sealed class SymbolEntry
        {
            public FaceSymbol symbol = FaceSymbol.None;
            public List<EffectDefinition> effects = new List<EffectDefinition>();
        }

        [SerializeField] private List<ColorEntry> colors = new List<ColorEntry>();
        [SerializeField] private List<SymbolEntry> symbols = new List<SymbolEntry>();

        // Order: die body, rolled symbol, glyph color. Empty slots are skipped.
        public void CollectEffects(DiceInstance die, DiceFace rolledFace, List<EffectDefinition> output)
        {
            if (die == null)
            {
                throw new ArgumentNullException(nameof(die));
            }

            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            ColorEntry body = FindColor(die.BodyColor);

            if (body != null)
            {
                AddAll(body.bodyEffects, output);
            }

            if (rolledFace == null)
            {
                return;
            }

            if (rolledFace.IsSymbol)
            {
                SymbolEntry symbol = symbols.Find(entry => entry.symbol == rolledFace.Symbol);

                if (symbol != null)
                {
                    AddAll(symbol.effects, output);
                }
            }

            ColorEntry glyph = FindColor(rolledFace.GlyphColor);

            if (glyph != null)
            {
                AddAll(glyph.glyphEffects, output);
            }
        }

        private ColorEntry FindColor(DiceColor color)
        {
            return color == DiceColor.None ? null : colors.Find(entry => entry.color == color);
        }

        private static void AddAll(List<EffectDefinition> source, List<EffectDefinition> output)
        {
            foreach (EffectDefinition effect in source)
            {
                if (effect != null)
                {
                    output.Add(effect);
                }
            }
        }
    }
}
