using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Whiskers.Dice.Visuals
{
    internal static class DieGlyphFactory
    {
        private const string GlyphObjectName = "Glyph";
        private const string HoleObjectName = "Hole";

        private const float MinFontSize = 0.01f;
        private const float MaxFontSize = 20f;

        // Relative to the slot size
        private const float HoleDiameterToSlot = 0.24f;
        private const float HoleSpacingToSlot = 0.32f;

        // TODO(art): symbol icons
        private const int SymbolFallbackLetters = 2;

        // Underline 6 and 9 like real dice
        private const string UnderlineFormat = "<u>{0}</u>";
        private static readonly HashSet<int> UnderlinedValues = new HashSet<int> { 6, 9 };

        public static void Create(DiceFace face, DieGlyphSlot slot, DiceStyle style, DiceVisualSettings settings)
        {
            Color color = style.GetGlyphColor(face.GlyphColor);

            if (face.IsNumber && style.SmallNumbersAsHoles && DicePipLayout.CanShow(face.Value) && settings.HoleSprite != null)
            {
                CreateHoles(face.Value, slot, color, settings.HoleSprite);
            }
            else if (face.IsSymbol && settings.TryGetSymbolSprite(face.Symbol, out Sprite symbolSprite))
            {
                CreateSprite(GlyphObjectName, symbolSprite, slot.Anchor, Vector2.zero, slot.Size, color);
            }
            else
            {
                string content = face.IsNumber ? FormatNumber(face.Value) : FormatSymbolFallback(face.Symbol);
                CreateText(content, slot, color, settings.NumberFont);
            }
        }

        private static void CreateHoles(int value, DieGlyphSlot slot, Color color, Sprite holeSprite)
        {
            float diameter = slot.Size * HoleDiameterToSlot;
            float spacing = slot.Size * HoleSpacingToSlot;

            foreach (Vector2 cell in DicePipLayout.GetCells(value))
            {
                CreateSprite(HoleObjectName, holeSprite, slot.Anchor, cell * spacing, diameter, color);
            }
        }

        private static void CreateSprite(string objectName, Sprite sprite, Transform parent, Vector2 offset, float size, Color color)
        {
            GameObject glyph = new GameObject(objectName);
            glyph.transform.SetParent(parent, false);
            glyph.transform.localPosition = offset;

            SpriteRenderer spriteRenderer = glyph.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = color;

            Vector3 spriteBounds = sprite.bounds.size;
            glyph.transform.localScale = Vector3.one * (size / Mathf.Max(spriteBounds.x, spriteBounds.y));
        }

        private static void CreateText(string content, DieGlyphSlot slot, Color color, TMP_FontAsset font)
        {
            GameObject glyph = new GameObject(GlyphObjectName);
            glyph.transform.SetParent(slot.Anchor, false);

            TextMeshPro text = glyph.AddComponent<TextMeshPro>();

            if (font != null)
            {
                text.font = font;
            }

            text.rectTransform.sizeDelta = Vector2.one * slot.Size;
            text.margin = Vector4.zero;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.enableAutoSizing = true;
            text.fontSizeMin = MinFontSize;
            text.fontSizeMax = MaxFontSize;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.text = content;
        }

        private static string FormatNumber(int value)
        {
            string digits = value.ToString();
            return UnderlinedValues.Contains(value) ? string.Format(UnderlineFormat, digits) : digits;
        }

        private static string FormatSymbolFallback(FaceSymbol symbol)
        {
            string name = symbol.ToString();
            return name.Length > SymbolFallbackLetters ? name.Substring(0, SymbolFallbackLetters) : name;
        }
    }
}
