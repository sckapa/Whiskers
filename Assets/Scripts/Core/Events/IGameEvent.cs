namespace Whiskers.Core.Events
{
    // Marker for every payload sent through the EventBus.
    // Implement it on readonly structs to avoid allocations when publishing.
    public interface IGameEvent
    {
    }
}
