using Whiskers.Core.Events;

namespace Whiskers.Core.GameStates
{
    public readonly struct GameStateChangedEvent : IGameEvent
    {
        public GameState PreviousState { get; }
        public GameState CurrentState { get; }

        public GameStateChangedEvent(GameState previousState, GameState currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }
    }
}
