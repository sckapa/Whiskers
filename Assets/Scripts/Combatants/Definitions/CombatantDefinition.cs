using System.Collections.Generic;
using UnityEngine;
using Whiskers.Dice;
using Whiskers.Effects;

namespace Whiskers.Combatants.Definitions
{
    // Shared design-time data for anyone who fights: the player cat and the rats
    public abstract class CombatantDefinition : ScriptableObject
    {
        private const int DefaultMaxHealth = 50;
        private const int DefaultDicePerRoll = 5;
        private const int MinimumMaxHealth = 1;
        private const int MinimumDicePerRoll = 1;

        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private CombatantVisuals visuals = new CombatantVisuals();
        [SerializeField, Min(MinimumMaxHealth)] private int maxHealth = DefaultMaxHealth;
        [SerializeField, Min(MinimumDicePerRoll)] private int dicePerRoll = DefaultDicePerRoll;
        [SerializeField] private List<DiceDefinition> startingDice = new List<DiceDefinition>();

        // Innate passives, e.g. a rat boss aura. Equipment adds its own effects at runtime.
        [SerializeField] private List<EffectDefinition> passiveEffects = new List<EffectDefinition>();

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public CombatantVisuals Visuals => visuals;
        public int MaxHealth => maxHealth;
        public int DicePerRoll => dicePerRoll;
        public IReadOnlyList<DiceDefinition> StartingDice => startingDice;
        public IReadOnlyList<EffectDefinition> PassiveEffects => passiveEffects;
    }
}
