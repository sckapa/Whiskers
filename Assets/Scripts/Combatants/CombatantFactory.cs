using System;
using System.Collections.Generic;
using Whiskers.Combatants.Definitions;
using Whiskers.Dice;

namespace Whiskers.Combatants
{
    // Builds runtime combatants from their definitions.
    // Dice creation is passed in (usually DiceManager.Instance.CreateDice) so the factory does not depend on scene objects.
    public static class CombatantFactory
    {
        public static PlayerCombatant CreatePlayer(PlayerDefinition definition, Func<DiceDefinition, DiceInstance> createDice)
        {
            ValidateArguments(definition, createDice);

            return new PlayerCombatant(
                definition.DisplayName,
                definition.MaxHealth,
                definition.DicePerRoll,
                CreateStartingDice(definition, createDice));
        }

        public static EnemyCombatant CreateEnemy(EnemyDefinition definition, Func<DiceDefinition, DiceInstance> createDice)
        {
            ValidateArguments(definition, createDice);

            return new EnemyCombatant(
                definition.DisplayName,
                definition.Tier,
                definition.MaxHealth,
                definition.DicePerRoll,
                CreateStartingDice(definition, createDice));
        }

        private static List<DiceInstance> CreateStartingDice(CombatantDefinition definition, Func<DiceDefinition, DiceInstance> createDice)
        {
            List<DiceInstance> dice = new List<DiceInstance>(definition.StartingDice.Count);

            foreach (DiceDefinition diceDefinition in definition.StartingDice)
            {
                // Empty inspector slots are reported by the definition's OnValidate
                if (diceDefinition != null)
                {
                    dice.Add(createDice(diceDefinition));
                }
            }

            return dice;
        }

        private static void ValidateArguments(CombatantDefinition definition, Func<DiceDefinition, DiceInstance> createDice)
        {
            if (definition == null)
            {
                throw new ArgumentNullException(nameof(definition));
            }

            if (createDice == null)
            {
                throw new ArgumentNullException(nameof(createDice));
            }
        }
    }
}
