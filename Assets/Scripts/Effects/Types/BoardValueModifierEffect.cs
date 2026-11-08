using System;
using System.Collections.Generic;
using UnityEngine;
using Whiskers.Battlefield;

namespace Whiskers.Effects.Types
{
    // Serialized, add new values at the end
    public enum BoardObjectTarget
    {
        // The triggering die's own object
        Self,
        AllAllied,
        AllOpposing,
        NearestOpposing
    }

    // Buff (positive amount) or debuff (negative) of the values on the board.
    // Only the battlefield copy changes, the dice in the bag keep their faces. Values never drop below zero.
    [CreateAssetMenu(fileName = "BoardValueModifierEffect", menuName = "Whiskers/Effects/Board Value Modifier")]
    public sealed class BoardValueModifierEffect : EffectDefinition
    {
        private const int MinimumValue = 0;

        [SerializeField] private BoardObjectTarget target = BoardObjectTarget.AllAllied;
        [SerializeField] private int amount = 1;

        public override void Apply(EffectContext context)
        {
            BattlefieldObjectRegistry registry = context.Board.Objects;

            foreach (BattlefieldObject targetObject in CollectTargets(context))
            {
                // Symbol-only objects have no number to change
                if (targetObject.IsNumerical && targetObject.CanInteract)
                {
                    registry.SetValue(targetObject.Id, Math.Max(MinimumValue, targetObject.Value + amount));
                }
            }
        }

        protected override bool IsApplicable(EffectContext context)
        {
            bool needsSource = target == BoardObjectTarget.Self || target == BoardObjectTarget.NearestOpposing;
            return !needsSource || (context.SourceObject != null && context.SourceObject.CanInteract);
        }

        private List<BattlefieldObject> CollectTargets(EffectContext context)
        {
            BattlefieldBoard board = context.Board;
            List<BattlefieldObject> targets = new List<BattlefieldObject>();

            switch (target)
            {
                case BoardObjectTarget.Self:
                    targets.Add(context.SourceObject);
                    break;
                case BoardObjectTarget.AllAllied:
                    targets.AddRange(board.GetObjectsOwnedBy(context.Owner.Side));
                    break;
                case BoardObjectTarget.AllOpposing:
                    targets.AddRange(board.GetObjectsOwnedBy(context.Opponent.Side));
                    break;
                case BoardObjectTarget.NearestOpposing:
                    if (board.TryGetNearestOpposing(context.SourceObject, out BattlefieldObject nearest))
                    {
                        targets.Add(nearest);
                    }

                    break;
            }

            return targets;
        }
    }
}
