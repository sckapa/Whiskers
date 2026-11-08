using UnityEngine;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Dice.Rolling;

namespace Whiskers.Battlefield
{
    // Creates the board and registers every settled die on it.
    // Board size = tray floor size.
    public sealed class BattlefieldManager : Singleton<BattlefieldManager>
    {
        [SerializeField] private TerritoryAxis territoryAxis = TerritoryAxis.Horizontal;

        [SerializeField, Range(0f, 1f)] private float startingPlayerShare = 0.5f;

        [SerializeField, Range(0f, 1f)] private float minPlayerShare = 0.2f;
        [SerializeField, Range(0f, 1f)] private float maxPlayerShare = 0.8f;

        [Header("Debug")]
        [SerializeField] private bool drawInteractionGraph = true;
        [SerializeField] private bool drawEdgeDistances = true;

        // Null until the DiceRoll scene is loaded
        public BattlefieldBoard Board { get; private set; }
        public bool IsReady => Board != null;

        private void OnEnable()
        {
            EventBus.Subscribe<DiceRollStageReadyEvent>(OnStageReady);
            EventBus.Subscribe<DieSettledEvent>(OnDieSettled);
            EventBus.Subscribe<BattlefieldObjectRegisteredEvent>(OnObjectRegistered);
            EventBus.Subscribe<BattlefieldObjectMovedEvent>(OnObjectMoved);
            EventBus.Subscribe<BattlefieldObjectRemovedEvent>(OnObjectRemoved);
            EventBus.Subscribe<BattlefieldObjectsMergedEvent>(OnObjectsMerged);

            if (DiceRollStage.Instance != null)
            {
                CreateBoard(DiceRollStage.Instance.Tray);
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DiceRollStageReadyEvent>(OnStageReady);
            EventBus.Unsubscribe<DieSettledEvent>(OnDieSettled);
            EventBus.Unsubscribe<BattlefieldObjectRegisteredEvent>(OnObjectRegistered);
            EventBus.Unsubscribe<BattlefieldObjectMovedEvent>(OnObjectMoved);
            EventBus.Unsubscribe<BattlefieldObjectRemovedEvent>(OnObjectRemoved);
            EventBus.Unsubscribe<BattlefieldObjectsMergedEvent>(OnObjectsMerged);
        }

        public void ResetForCombat()
        {
            Board?.Clear();
        }

        private void OnStageReady(DiceRollStageReadyEvent stageReady)
        {
            CreateBoard(stageReady.Stage.Tray);
        }

        private void CreateBoard(DiceTray tray)
        {
            if (IsReady)
            {
                return;
            }

            BattlefieldTerritory territory = new BattlefieldTerritory(territoryAxis, startingPlayerShare, minPlayerShare, maxPlayerShare);
            Board = new BattlefieldBoard(tray.FloorSize, territory);
            EventBus.Publish(new BattlefieldReadyEvent(Board));
        }

        private void OnDieSettled(DieSettledEvent settled)
        {
            if (!IsReady)
            {
                Debug.LogWarning($"Die {settled.Result.Die?.Id} settled before the battlefield board was created; it was not registered.");
                return;
            }

            DieRollResult result = settled.Result;
            Board.RegisterRolledDie(result.Die, result.Face, result.Owner, result.NormalizedPosition);
        }

        // The graph follows the registry: it only changes in response to these events

        private void OnObjectRegistered(BattlefieldObjectRegisteredEvent registered)
        {
            if (IsReady)
            {
                Board.Graph.AddNode(registered.Object);
            }
        }

        private void OnObjectMoved(BattlefieldObjectMovedEvent moved)
        {
            if (IsReady)
            {
                Board.Graph.UpdatePosition(moved.Object.Id);
            }
        }

        private void OnObjectRemoved(BattlefieldObjectRemovedEvent removed)
        {
            if (IsReady)
            {
                Board.Graph.RemoveNode(removed.Object.Id);
            }
        }

        private void OnObjectsMerged(BattlefieldObjectsMergedEvent merged)
        {
            if (IsReady)
            {
                Board.Graph.ReplaceNodes(merged.MergedObjects, merged.Result);
            }
        }

        private void OnDrawGizmos()
        {
            if (drawInteractionGraph && IsReady && DiceRollStage.Instance != null)
            {
                InteractionGraphGizmos.Draw(Board, DiceRollStage.Instance.Tray, drawEdgeDistances);
            }
        }

        private void OnValidate()
        {
            maxPlayerShare = Mathf.Max(maxPlayerShare, minPlayerShare);
        }
    }

    public readonly struct BattlefieldReadyEvent : IGameEvent
    {
        public BattlefieldBoard Board { get; }

        public BattlefieldReadyEvent(BattlefieldBoard board)
        {
            Board = board;
        }
    }
}
