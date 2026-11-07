namespace Whiskers.Core.GameStates
{
    // Top-level states of the game. Victory and Defeat describe the outcome of the whole run,
    // a regular won combat returns to Run (the map).
    public enum GameState
    {
        None,
        MainMenu,
        Run,
        Combat,
        Shop,
        Event,
        Victory,
        Defeat
    }
}
