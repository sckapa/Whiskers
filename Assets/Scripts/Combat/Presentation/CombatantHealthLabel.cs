using TMPro;
using UnityEngine;
using Whiskers.Combatants;
using Whiskers.Core.Events;

namespace Whiskers.Combat.Presentation
{
    // HP text for one side, punches when HP changes. Shows DisplayedHealth (waits for damage flights).
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CombatantHealthLabel : CombatantBoundView
    {
        private const string HealthFormat = "{0} / {1}";

        [SerializeField, Min(0f)] private float punchScale = 1.25f;
        [SerializeField, Min(0.01f)] private float punchDuration = 0.25f;

        private TMP_Text label;
        private Vector3 baseScale;
        private float punchTime = float.PositiveInfinity;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            baseScale = transform.localScale;
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

        private void Update()
        {
            if (punchTime >= punchDuration)
            {
                return;
            }

            // Unscaled, so the punch finishes even if the game is paused right after a hit
            punchTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(punchTime / punchDuration);
            transform.localScale = baseScale * Mathf.Lerp(punchScale, 1f, progress);
        }

        protected override void OnBound()
        {
            Refresh();
        }

        private void OnHealthChanged(DisplayedHealthChangedEvent changed)
        {
            if (!IsMine(changed.Combatant))
            {
                return;
            }

            Refresh();
            punchTime = 0f;
        }

        private void Refresh()
        {
            label.text = string.Format(HealthFormat, DisplayedHealth.Get(Combatant), Combatant.Health.Max);
        }
    }
}
