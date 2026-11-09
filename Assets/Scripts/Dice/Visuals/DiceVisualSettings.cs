using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Whiskers.Core;

namespace Whiskers.Dice.Visuals
{
    [Serializable]
    public sealed class SymbolSpriteEntry
    {
        [SerializeField] private FaceSymbol symbol = FaceSymbol.None;
        [SerializeField] private Sprite sprite = null;

        public FaceSymbol Symbol => symbol;
        public Sprite Sprite => sprite;
    }

    // TODO(art): palettes, font, icons, cheese material
    [CreateAssetMenu(fileName = "DiceVisualSettings", menuName = "Whiskers/Dice Visual Settings")]
    public sealed class DiceVisualSettings : ScriptableObject
    {
        [SerializeField] private DiceStyle playerStyle;
        [SerializeField] private DiceStyle enemyStyle;

        [SerializeField] private TMP_FontAsset numberFont = null;

        [Header("Holes (cheese dice), relative to the face slot size")]
        [SerializeField] private Sprite holeSprite;
        [SerializeField, Range(0.05f, 0.6f)] private float holeSize = 0.3f;
        [SerializeField, Range(0.1f, 0.6f)] private float holeSpacing = 0.34f;
        // Random shift of every hole from its grid cell
        [SerializeField, Range(0f, 0.3f)] private float holePositionJitter = 0.05f;
        // Random size change of every hole, 0.25 = +-25%
        [SerializeField, Range(0f, 0.6f)] private float holeSizeJitter = 0.25f;

        [SerializeField] private List<SymbolSpriteEntry> symbolSprites = new List<SymbolSpriteEntry>();

        public TMP_FontAsset NumberFont => numberFont;
        public Sprite HoleSprite => holeSprite;
        public float HoleSize => holeSize;
        public float HoleSpacing => holeSpacing;
        public float HolePositionJitter => holePositionJitter;
        public float HoleSizeJitter => holeSizeJitter;

        public DiceStyle GetStyle(CombatSide owner)
        {
            return owner == CombatSide.Player ? playerStyle : enemyStyle;
        }

        public bool TryGetSymbolSprite(FaceSymbol symbol, out Sprite sprite)
        {
            SymbolSpriteEntry entry = symbolSprites.Find(candidate => candidate.Symbol == symbol);
            sprite = entry?.Sprite;
            return sprite != null;
        }

        public void InitializeDefaults(DiceStyle player, DiceStyle enemy, Sprite hole)
        {
            playerStyle = player;
            enemyStyle = enemy;
            holeSprite = hole;
        }
    }
}
