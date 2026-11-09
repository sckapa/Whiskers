using System;
using System.Collections.Generic;
using Whiskers.Combatants.Definitions;
using Whiskers.Dice;

namespace Whiskers.Combatants
{
    // createDice: usually DiceManager.Instance.CreateDice
    public static class CombatantFactory
    {
        public static PlayerCombatant CreatePlayer(PlayerDefinition definition, Func<DiceDefinition, DiceInstance> createDice)
        {
            ValidateArguments(definition, createDice);

            PlayerCombatant player = new PlayerCombatant(
                definition.DisplayName,
                definition.MaxHealth,
                definition.DicePerRoll,
                CreateStartingDice(definition, createDice),
                definition.PassiveEffects);

            player.Visuals = definition.Visuals;
            return player;
        }

        public static EnemyCombatant CreateEnemy(EnemyDefinition definition, Func<DiceDefinition, DiceInstance> createDice)
        {
            ValidateArguments(definition, createDice);

            EnemyCombatant enemy = new EnemyCombatant(
                definition.DisplayName,
                definition.Tier,
                definition.MaxHealth,
                definition.DicePerRoll,
                CreateStartingDice(definition, createDice),
                definition.PassiveEffects);

            enemy.Visuals = definition.Visuals;
            return enemy;
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
