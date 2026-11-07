using System;

namespace Whiskers.Core.GameStates
{
    // Plain C# state holder, kept free of MonoBehaviour so it can be unit tested in isolation
    public sealed class GameStateMachine
    {
        private readonly GameStateTransitions transitions;

        public GameState CurrentState { get; private set; } = GameState.None;

        public GameStateMachine(GameStateTransitions transitions)
        {
            this.transitions = transitions ?? throw new ArgumentNullException(nameof(transitions));
        }

        public bool CanTransitionTo(GameState nextState)
        {
            return transitions.IsAllowed(CurrentState, nextState);
        }

        public bool TryTransitionTo(GameState nextState)
        {
            if (!CanTransitionTo(nextState))
            {
                return false;
            }

            CurrentState = nextState;
            return true;
        }
    }
}
