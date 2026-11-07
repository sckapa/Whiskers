using UnityEngine;
using Whiskers.Core;
using Whiskers.Core.Events;

namespace Whiskers.Battlefield
{
    public enum TerritoryAxis
    {
        // Player on the left, enemy on the right
        Horizontal,
        // Player at the bottom, enemy at the top
        Vertical
    }

    // Player and enemy parts of the board, in 0..1 coordinates.
    public sealed class BattlefieldTerritory
    {
        private readonly float startingPlayerShare;
        private readonly float minPlayerShare;
        private readonly float maxPlayerShare;

        public TerritoryAxis Axis { get; }

        // Border position
        public float PlayerShare { get; private set; }

        public BattlefieldTerritory(TerritoryAxis axis, float startingPlayerShare, float minPlayerShare, float maxPlayerShare)
        {
            Axis = axis;
            this.minPlayerShare = minPlayerShare;
            this.maxPlayerShare = maxPlayerShare;
            this.startingPlayerShare = Mathf.Clamp(startingPlayerShare, minPlayerShare, maxPlayerShare);
            PlayerShare = this.startingPlayerShare;
        }

        public float GetShare(CombatSide side)
        {
            return side == CombatSide.Player ? PlayerShare : 1f - PlayerShare;
        }

        public CombatSide GetOwnerAt(Vector2 normalizedPosition)
        {
            float coordinate = Axis == TerritoryAxis.Horizontal ? normalizedPosition.x : normalizedPosition.y;
            return coordinate < PlayerShare ? CombatSide.Player : CombatSide.Enemy;
        }

        // amount is a share of the whole board
        public void Expand(CombatSide side, float amount)
        {
            float signedAmount = side == CombatSide.Player ? amount : -amount;
            SetPlayerShare(PlayerShare + signedAmount);
        }

        public void Reset()
        {
            SetPlayerShare(startingPlayerShare);
        }

        private void SetPlayerShare(float share)
        {
            float clamped = Mathf.Clamp(share, minPlayerShare, maxPlayerShare);

            if (Mathf.Approximately(clamped, PlayerShare))
            {
                return;
            }

            PlayerShare = clamped;
            EventBus.Publish(new TerritoryChangedEvent(PlayerShare));
        }
    }

    public readonly struct TerritoryChangedEvent : IGameEvent
    {
        public float PlayerShare { get; }

        public TerritoryChangedEvent(float playerShare)
        {
            PlayerShare = playerShare;
        }
    }
}
