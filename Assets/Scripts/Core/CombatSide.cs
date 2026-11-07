namespace Whiskers.Core
{
    // Side a combatant, die or battlefield object belongs to.
    // All rats share the Enemy side, so allied/opposing checks stay a simple comparison.
    public enum CombatSide
    {
        Player,
        Enemy
    }
}
