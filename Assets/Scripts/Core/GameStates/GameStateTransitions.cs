using System;
using System.Collections.Generic;

namespace Whiskers.Core.GameStates
{
    // Whitelist of allowed state changes. Anything not listed here is rejected by the state machine.
    public sealed class GameStateTransitions
    {
        private readonly Dictionary<GameState, HashSet<GameState>> allowedTargetsBySource =
            new Dictionary<GameState, HashSet<GameState>>();

        public GameStateTransitions Allow(GameState from, params GameState[] targets)
        {
            if (!allowedTargetsBySource.TryGetValue(from, out HashSet<GameState> allowedTargets))
            {
                allowedTargets = new HashSet<GameState>();
                allowedTargetsBySource.Add(from, allowedTargets);
            }

            allowedTargets.UnionWith(targets);
            return this;
        }

        public bool IsAllowed(GameState from, GameState to)
        {
            return allowedTargetsBySource.TryGetValue(from, out HashSet<GameState> allowedTargets)
                && allowedTargets.Contains(to);
        }

        public static GameStateTransitions CreateDefault()
        {
            return new GameStateTransitions()
                // Bootstrap may enter any state, so every scene can be started directly from the editor
                .Allow(GameState.None, AllPlayableStates())
                .Allow(GameState.MainMenu, GameState.Run)
                .Allow(GameState.Run, GameState.Combat, GameState.Shop, GameState.Event, GameState.MainMenu)
                .Allow(GameState.Combat, GameState.Run, GameState.Victory, GameState.Defeat)
                .Allow(GameState.Shop, GameState.Run)
                .Allow(GameState.Event, GameState.Run, GameState.Combat, GameState.Defeat)
                .Allow(GameState.Victory, GameState.MainMenu)
                .Allow(GameState.Defeat, GameState.MainMenu);
        }

        private static GameState[] AllPlayableStates()
        {
            List<GameState> states = new List<GameState>((GameState[])Enum.GetValues(typeof(GameState)));
            states.Remove(GameState.None);
            return states.ToArray();
        }
    }
}
