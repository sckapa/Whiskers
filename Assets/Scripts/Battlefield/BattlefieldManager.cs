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

        // Null until the DiceRoll scene is loaded
        public BattlefieldBoard Board { get; private set; }
        public bool IsReady => Board != null;

        private void OnEnable()
        {
            EventBus.Subscribe<DiceRollStageReadyEvent>(OnStageReady);
            EventBus.Subscribe<DieSettledEvent>(OnDieSettled);

            if (DiceRollStage.Instance != null)
            {
                CreateBoard(DiceRollStage.Instance.Tray);
            }
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<DiceRollStageReadyEvent>(OnStageReady);
            EventBus.Unsubscribe<DieSettledEvent>(OnDieSettled);
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
