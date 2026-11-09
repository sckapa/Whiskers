using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Combat;
using Whiskers.Combatants;
using Whiskers.Combatants.Definitions;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Core.GameStates;
using Whiskers.Dice;
using Whiskers.Run.Map;
using Random = System.Random;

namespace Whiskers.Run
{
    // One run: the player (HP and dice carry over between fights), the map and the progress on it.
    // Lives in Bootstrap and survives scene loads.
    public sealed class RunManager : Singleton<RunManager>
    {
        [SerializeField] private RunConfig config = null;

        private readonly HashSet<int> completedNodeIds = new HashSet<int>();
        private Random random;
        private EnemyDefinition pendingEnemy;

        public bool IsRunActive => Player != null;
        public PlayerCombatant Player { get; private set; }
        public RunMap Map { get; private set; }

        // Last completed node, null before the first one
        public MapNode CurrentNode { get; private set; }

        // Node being played (fight in progress), null on the map
        public MapNode ActiveNode { get; private set; }

        public RunOutcome Outcome { get; private set; } = RunOutcome.None;
        public int CompletedNodeCount => completedNodeIds.Count;
        public bool HasPendingCombat => pendingEnemy != null;

        protected override bool PersistAcrossScenes => true;

        private void OnEnable()
        {
            EventBus.Subscribe<CombatEndedEvent>(OnCombatEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CombatEndedEvent>(OnCombatEnded);
        }

        public void StartNewRun()
        {
            // Needs a Run Config with a player and a DiceManager in Bootstrap
            if (config == null || config.Player == null || DiceManager.Instance == null)
            {
                return;
            }

            random = config.Seed == 0 ? new Random() : new Random(config.Seed);
            DiceManager.Instance.ResetAll();
            completedNodeIds.Clear();
            CurrentNode = null;
            ActiveNode = null;
            pendingEnemy = null;
            Outcome = RunOutcome.None;

            Player = CombatantFactory.CreatePlayer(config.Player, DiceManager.Instance.CreateDice);
            Map = MapGenerator.Generate(config.Map, random);

            EventBus.Publish(new RunStartedEvent(this));
            ChangeState(GameState.Run);
        }

        // Back to the main menu, the run is thrown away
        public void EndRun()
        {
            Player = null;
            Map = null;
            CurrentNode = null;
            ActiveNode = null;
            pendingEnemy = null;
            completedNodeIds.Clear();
            ChangeState(GameState.MainMenu);
        }

        public bool IsCompleted(MapNode node)
        {
            return node != null && completedNodeIds.Contains(node.Id);
        }

        // Bottom row at the start, afterwards the nodes the current node leads to
        public bool IsAvailable(MapNode node)
        {
            if (node == null || Map == null || ActiveNode != null || IsCompleted(node))
            {
                return false;
            }

            return CurrentNode == null ? node.Row == 0 : CurrentNode.LeadsTo(node.Id);
        }

        public void ChooseNode(MapNode node)
        {
            if (!IsAvailable(node))
            {
                return;
            }

            ActiveNode = node;

            switch (node.Type)
            {
                case MapNodeType.Combat:
                case MapNodeType.Elite:
                case MapNodeType.Boss:
                    BeginCombat(node);
                    break;
                case MapNodeType.Rest:
                    Player.Heal(Mathf.CeilToInt(Player.Health.Max * config.RestHealShare));
                    CompleteActiveNode();
                    break;
                default:
                    // TODO: shops and events (task 28), for now the node is just passed
                    CompleteActiveNode();
                    break;
            }
        }

        // CombatManager asks for its fight when the Combat scene starts
        public bool TryTakePendingCombat(out PlayerCombatant player, out EnemyCombatant enemy)
        {
            player = null;
            enemy = null;

            if (pendingEnemy == null || DiceManager.Instance == null)
            {
                return false;
            }

            player = Player;
            enemy = CombatantFactory.CreateEnemy(pendingEnemy, DiceManager.Instance.CreateDice);
            pendingEnemy = null;
            return true;
        }

        private void BeginCombat(MapNode node)
        {
            IReadOnlyList<EnemyDefinition> pool = config.GetEnemyPool(node.Type);
            List<EnemyDefinition> candidates = new List<EnemyDefinition>(pool);
            candidates.RemoveAll(enemy => enemy == null);

            // No enemies in the config: the fight is skipped
            if (candidates.Count == 0)
            {
                CompleteActiveNode();
                return;
            }

            pendingEnemy = candidates[random.Next(candidates.Count)];
            ChangeState(GameState.Combat);
        }

        private void OnCombatEnded(CombatEndedEvent ended)
        {
            if (!IsRunActive || ActiveNode == null)
            {
                return;
            }

            // The run decides where to go after a fight, CombatManager leaves the game state alone during a run
            if (ended.Winner != CombatSide.Player)
            {
                FinishRun(RunOutcome.Defeat, GameState.Defeat);
                return;
            }

            bool wasBoss = ActiveNode.Type == MapNodeType.Boss;
            CompleteActiveNode();

            if (wasBoss)
            {
                FinishRun(RunOutcome.Victory, GameState.Victory);
            }
            else
            {
                ChangeState(GameState.Run);
            }
        }

        private void FinishRun(RunOutcome outcome, GameState state)
        {
            Outcome = outcome;
            EventBus.Publish(new RunFinishedEvent(outcome));
            ChangeState(state);
        }

        private void CompleteActiveNode()
        {
            completedNodeIds.Add(ActiveNode.Id);
            CurrentNode = ActiveNode;
            ActiveNode = null;
            EventBus.Publish(new RunMapChangedEvent());
        }

        private static void ChangeState(GameState state)
        {
            GameManager gameManager = GameManager.Instance;

            if (gameManager != null && gameManager.CurrentState != state)
            {
                gameManager.ChangeState(state);
            }
        }
    }

    public enum RunOutcome
    {
        None,
        Victory,
        Defeat
    }

    public readonly struct RunStartedEvent : IGameEvent
    {
        public RunManager Run { get; }

        public RunStartedEvent(RunManager run)
        {
            Run = run;
        }
    }

    // A node was completed, the map view should refresh
    public readonly struct RunMapChangedEvent : IGameEvent
    {
    }

    public readonly struct RunFinishedEvent : IGameEvent
    {
        public RunOutcome Outcome { get; }

        public RunFinishedEvent(RunOutcome outcome)
        {
            Outcome = outcome;
        }
    }
}
