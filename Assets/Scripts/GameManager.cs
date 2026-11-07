using UnityEngine;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Core.GameStates;

// Entry point of the game. Owns the top-level state machine and announces every state change on the EventBus.
public class GameManager : Singleton<GameManager>
{
    [Tooltip("State entered on start. Lets a scene be played directly from the editor, e.g. Combat.")]
    [SerializeField] private GameState initialState = GameState.MainMenu;

    private GameStateMachine stateMachine;

    public GameState CurrentState => stateMachine.CurrentState;

    protected override bool PersistAcrossScenes => true;

    protected override void OnAwake()
    {
        stateMachine = new GameStateMachine(GameStateTransitions.CreateDefault());
    }

    private void Start()
    {
        ChangeState(initialState);
    }

    public bool CanChangeState(GameState nextState)
    {
        return stateMachine.CanTransitionTo(nextState);
    }

    public bool ChangeState(GameState nextState)
    {
        GameState previousState = stateMachine.CurrentState;

        if (!stateMachine.TryTransitionTo(nextState))
        {
            Debug.LogWarning($"Game state transition {previousState} -> {nextState} is not allowed.");
            return false;
        }

        EventBus.Publish(new GameStateChangedEvent(previousState, nextState));
        return true;
    }
}
