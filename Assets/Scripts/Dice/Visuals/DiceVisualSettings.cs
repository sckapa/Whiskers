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

        [SerializeField] private Sprite holeSprite;

        [SerializeField] private List<SymbolSpriteEntry> symbolSprites = new List<SymbolSpriteEntry>();

        public TMP_FontAsset NumberFont => numberFont;
        public Sprite HoleSprite => holeSprite;

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
