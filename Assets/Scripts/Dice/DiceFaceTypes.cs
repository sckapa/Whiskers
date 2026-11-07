namespace Whiskers.Dice
{
    // A face shows either a number or a symbol, nothing else
    public enum FaceType
    {
        Number,
        Symbol
    }

    // Shared palette for the whole die body and for the number or symbol printed on a face.
    // The meaning depends on where the color is applied: a body color is an inherent die effect,
    // a glyph color is an additional face effect. Ownership is shown by material (cheese for rats), not by color.
    // The final set of colors is not designed yet; new values are appended at the end to keep serialized data valid.
    public enum DiceColor
    {
        None,
        // Glyph: stays on the board after dealing damage, keeping the subtracted value
        Red,
        // Glyph: rolls an additional die on landing
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
        // Summons a dragon to the outer side of the battlefield
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
