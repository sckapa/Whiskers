using UnityEngine;
using Whiskers.Combatants;
using Whiskers.Core.Events;

namespace Whiskers.Combat.Presentation
{
    // Which side of the bar disappears as HP drops. Serialized, add new values at the end.
    public enum HealthBarDepletion
    {
        FromRight,
        FromLeft,
        FromTop,
        FromBottom
    }

    // HP bar cut by a mask: fillMask (with RectMask2D) shrinks toward the kept edge, the bar picture inside it
    // is pinned to that edge with its full size, so the other side is cut off as HP drops.
    // Optional trail: a second mask that follows the fill with a delay (shows how much was just lost).
    public sealed class HealthBarView : CombatantBoundView
    {
        private const float Empty = 0f;
        private const float Full = 1f;
        private const float Center = 0.5f;

        [SerializeField] private HealthBarDepletion depletion = HealthBarDepletion.FromRight;
        [SerializeField] private RectTransform fillMask = null;
        [Tooltip("0 = jump to the new value")]
        [SerializeField, Min(0f)] private float fillSpeed = 2f;

        [Header("Optional damage trail")]
        [SerializeField] private RectTransform trailMask = null;
        [SerializeField, Min(0f)] private float trailDelay = 0.35f;
        [SerializeField, Min(0f)] private float trailSpeed = 1f;

        private float fullLength;
        private float targetPercent = Full;
        private float shownPercent = Full;
        private float trailPercent = Full;
        private float trailWait;

        private bool IsHorizontal => depletion == HealthBarDepletion.FromRight || depletion == HealthBarDepletion.FromLeft;
        private RectTransform.Axis Axis => IsHorizontal ? RectTransform.Axis.Horizontal : RectTransform.Axis.Vertical;

        // Normalized point of the edge that stays put
        private Vector2 KeptEdge
        {
            get
            {
                switch (depletion)
                {
                    case HealthBarDepletion.FromLeft:
                        return new Vector2(Full, Center);
                    case HealthBarDepletion.FromTop:
                        return new Vector2(Center, Empty);
                    case HealthBarDepletion.FromBottom:
                        return new Vector2(Center, Full);
                    default:
                        return new Vector2(Empty, Center);
                }
            }
        }

        private void Awake()
        {
            fullLength = IsHorizontal ? fillMask.rect.width : fillMask.rect.height;
            PrepareMask(fillMask);
            PrepareMask(trailMask);
        }

        protected override void OnEnable()
        {
            EventBus.Subscribe<DisplayedHealthChangedEvent>(OnHealthChanged);
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            EventBus.Unsubscribe<DisplayedHealthChangedEvent>(OnHealthChanged);
        }

        protected override void OnBound()
        {
            targetPercent = CalculatePercent();
            shownPercent = targetPercent;
            trailPercent = targetPercent;
            Apply();
        }

        private void OnHealthChanged(DisplayedHealthChangedEvent changed)
        {
            if (!IsMine(changed.Combatant))
            {
                return;
            }

            targetPercent = CalculatePercent();

            // Healing: the trail is not needed, it jumps up together with the fill
            if (targetPercent > trailPercent)
            {
                trailPercent = targetPercent;
            }

            trailWait = trailDelay;
        }

        private void Update()
        {
            shownPercent = fillSpeed > 0f
                ? Mathf.MoveTowards(shownPercent, targetPercent, fillSpeed * Time.unscaledDeltaTime)
                : targetPercent;

            if (trailWait > 0f)
            {
                trailWait -= Time.unscaledDeltaTime;
            }
            else
            {
                trailPercent = Mathf.MoveTowards(trailPercent, shownPercent, trailSpeed * Time.unscaledDeltaTime);
            }

            Apply();
        }

        private float CalculatePercent()
        {
            Health health = Combatant.Health;
            return health.Max > 0 ? Mathf.Clamp01((float)DisplayedHealth.Get(Combatant) / health.Max) : Empty;
        }

        private void Apply()
        {
            fillMask.SetSizeWithCurrentAnchors(Axis, fullLength * shownPercent);

            if (trailMask != null)
            {
                trailMask.SetSizeWithCurrentAnchors(Axis, fullLength * Mathf.Max(trailPercent, shownPercent));
            }
        }

        // The mask shrinks toward the kept edge; the pictures inside are pinned to it at full size
        private void PrepareMask(RectTransform mask)
        {
            if (mask == null)
            {
                return;
            }

            // Pictures first, while the mask still has its full size
            foreach (Transform child in mask)
            {
                if (child is RectTransform content)
                {
                    PinToEdge(content, KeptEdge);
                }
            }

            MovePivotOnAxis(mask);
        }

        // Moves the pivot along the depletion axis without moving the rect on screen
        private void MovePivotOnAxis(RectTransform rect)
        {
            Vector2 pivot = rect.pivot;
            Vector2 newPivot = IsHorizontal ? new Vector2(KeptEdge.x, pivot.y) : new Vector2(pivot.x, KeptEdge.y);
            Vector2 size = rect.rect.size;
            Vector2 shift = Vector2.Scale(newPivot - pivot, size);

            rect.pivot = newPivot;
            rect.anchoredPosition += Vector2.Scale(shift, rect.localScale);
        }

        // Fixed size, anchored and pivoted at the edge
        private static void PinToEdge(RectTransform content, Vector2 edge)
        {
            Vector2 size = content.rect.size;
            content.anchorMin = edge;
            content.anchorMax = edge;
            content.pivot = edge;
            content.sizeDelta = size;
            content.anchoredPosition = Vector2.zero;
        }
    }
}
