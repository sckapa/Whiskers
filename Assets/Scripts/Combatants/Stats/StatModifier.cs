namespace Whiskers.Combatants.Stats
{
    // Flat bonus or penalty to a stat. Source is the item, passive or effect that granted it,
    // so everything it granted can be removed at once when it is unequipped or expires.
    public readonly struct StatModifier
    {
        public int Amount { get; }
        public object Source { get; }

        public StatModifier(int amount, object source)
        {
            Amount = amount;
            Source = source;
        }
    }
}
