using System.Collections.Generic;
using Whiskers.Core;

namespace Whiskers.Dice
{
    // Creates dice with unique ids and keeps a lookup of every die that exists in the current run
    public class DiceManager : Singleton<DiceManager>
    {
        private const string StandardDiceNameFormat = "Standard {0}";

        private readonly Dictionary<int, DiceInstance> diceById = new Dictionary<int, DiceInstance>();
        private readonly UniqueIdGenerator idGenerator = new UniqueIdGenerator();

        public IReadOnlyDictionary<int, DiceInstance> DiceById => diceById;

        public DiceInstance CreateDice(DiceDefinition definition)
        {
            return Register(DiceInstance.FromDefinition(idGenerator.Next(), definition));
        }

        public DiceInstance CreateStandardDice(DiceSize size)
        {
            string displayName = string.Format(StandardDiceNameFormat, size);
            return Register(new DiceInstance(idGenerator.Next(), displayName, size, DiceFaceSets.CreateStandardNumbers(size)));
        }

        public bool TryGetDice(int id, out DiceInstance dice)
        {
            return diceById.TryGetValue(id, out dice);
        }

        public bool RemoveDice(int id)
        {
            return diceById.Remove(id);
        }

        // Called when a new run starts so ids and the lookup begin from scratch
        public void ResetAll()
        {
            diceById.Clear();
            idGenerator.Reset();
        }

        private DiceInstance Register(DiceInstance dice)
        {
            diceById.Add(dice.Id, dice);
            return dice;
        }
    }
}
