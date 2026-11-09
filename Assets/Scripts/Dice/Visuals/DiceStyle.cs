using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whiskers.Dice.Visuals
{
    [Serializable]
    public sealed class DiceColorEntry
    {
        [SerializeField] private DiceColor color;
        [SerializeField] private Color bodyColor = Color.white;
        [SerializeField] private Color glyphColor = Color.black;

        public DiceColor ColorKey => color;
        public Color BodyColor => bodyColor;
        public Color GlyphColor => glyphColor;

        public DiceColorEntry(DiceColor color, Color bodyColor, Color glyphColor)
        {
            this.color = color;
            this.bodyColor = bodyColor;
            this.glyphColor = glyphColor;
        }
    }

    [Serializable]
    public sealed class DiceStyle
    {
        [SerializeField] private Material bodyMaterial;
        [SerializeField] private Color defaultBodyColor = UnityEngine.Color.white;
        [SerializeField] private Color defaultGlyphColor = UnityEngine.Color.black;

        [SerializeField] private bool smallNumbersAsHoles;

        [SerializeField] private List<DiceColorEntry> colors = new List<DiceColorEntry>();

        public Material BodyMaterial => bodyMaterial;
        public bool SmallNumbersAsHoles => smallNumbersAsHoles;

        public DiceStyle(Material bodyMaterial, Color defaultBodyColor, Color defaultGlyphColor, bool smallNumbersAsHoles, List<DiceColorEntry> colors)
        {
            this.bodyMaterial = bodyMaterial;
            this.defaultBodyColor = defaultBodyColor;
            this.defaultGlyphColor = defaultGlyphColor;
            this.smallNumbersAsHoles = smallNumbersAsHoles;
            this.colors = colors ?? new List<DiceColorEntry>();
        }

        public Color GetBodyColor(DiceColor diceColor)
        {
            return TryFind(diceColor, out DiceColorEntry entry) ? entry.BodyColor : defaultBodyColor;
        }

        public Color GetGlyphColor(DiceColor diceColor)
        {
            return TryFind(diceColor, out DiceColorEntry entry) ? entry.GlyphColor : defaultGlyphColor;
        }

        private bool TryFind(DiceColor diceColor, out DiceColorEntry found)
        {
            found = diceColor == DiceColor.None ? null : colors.Find(entry => entry.ColorKey == diceColor);
            return found != null;
        }
    }
}
