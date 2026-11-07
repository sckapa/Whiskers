using System.Collections.Generic;
using UnityEngine;
using Whiskers.Dice;

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
        [SerializeField, Min(MinimumMaxHealth)] private int maxHealth = DefaultMaxHealth;
        [SerializeField, Min(MinimumDicePerRoll)] private int dicePerRoll = DefaultDicePerRoll;
        [SerializeField] private List<DiceDefinition> startingDice = new List<DiceDefinition>();

        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public int MaxHealth => maxHealth;
        public int DicePerRoll => dicePerRoll;
        public IReadOnlyList<DiceDefinition> StartingDice => startingDice;

        protected virtual void OnValidate()
        {
            if (startingDice.Contains(null))
            {
                Debug.LogWarning($"'{name}' has empty entries in its starting dice, they will be skipped.", this);
            }
        }
    }
}
