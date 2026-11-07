namespace Whiskers.Battlefield
{
    public enum BattlefieldObjectType
    {
        Die,
        // Result of merging allied objects
        Combined,
        Summon
    }

    public enum BattlefieldObjectState
    {
        Active,
        // Picked by the interaction queue and currently being resolved
        AwaitingInteraction,
        // Consumed by a merge, replaced by the combined object
        Combined,
        Destroyed
    }
}
