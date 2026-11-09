using UnityEngine;
using Whiskers.Core;
using Whiskers.Core.Events;

namespace Whiskers.Dice.Rolling
{
    public sealed class DiceRollStage : Singleton<DiceRollStage>
    {
        [SerializeField] private DiceTray tray = null;
        [SerializeField] private DiceRollCamera rollCamera = null;
        [SerializeField] private DiceThrower thrower = null;
        [SerializeField] private DiceRollRound round = null;

        public DiceTray Tray => tray;
        public DiceRollCamera RollCamera => rollCamera;
        public DiceThrower Thrower => thrower;
        public DiceRollRound Round => round;

        private void Start()
        {
            EventBus.Publish(new DiceRollStageReadyEvent(this));
        }
    }

    public readonly struct DiceRollStageReadyEvent : IGameEvent
    {
        public DiceRollStage Stage { get; }

        public DiceRollStageReadyEvent(DiceRollStage stage)
        {
            Stage = stage;
        }
    }
}
