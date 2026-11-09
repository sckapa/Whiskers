namespace Whiskers.Dice
{
    // A face shows either a number or a symbol, nothing else
    public enum FaceType
    {
        Number,
        Symbol
    }

    // Body color = die effect, glyph color = face effect
    // Serialized, add new values at the end
    public enum DiceColor
    {
        None,
        // Stays on the board with the leftover value
        Red,
        // Extra roll on landing
        Yellow
    }

    public enum FaceSymbol
    {
        None,
        Sword,
        Shield,
        Moon,
        Sun,
        Tree,
        Rot,
        Clover,
        Dragon
    }

    public static class FaceSymbolExtensions
    {
        public static bool IsSummon(this FaceSymbol symbol)
        {
            return symbol == FaceSymbol.Dragon;
        }
    }
}
