using TMPro;
using UnityEngine;
using Whiskers.Combatants;
using Whiskers.Core;
using Whiskers.Core.Events;

namespace Whiskers.Combat.Presentation
{
    // HP text for one side
    // TODO(art): HP bar
    [RequireComponent(typeof(TMP_Text))]
    public sealed class CombatantHealthLabel : MonoBehaviour
    {
        private const string HealthFormat = "{0} / {1}";

        [SerializeField] private CombatSide side = CombatSide.Player;
        [SerializeField, Min(0f)] private float punchScale = 1.25f;
        [SerializeField, Min(0.01f)] private float punchDuration = 0.25f;

        private TMP_Text label;
        private Combatant combatant;
        private Vector3 baseScale;
        private float punchTime = float.PositiveInfinity;

        private void Awake()
        {
            label = GetComponent<TMP_Text>();
            baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Subscribe<CombatantHealthChangedEvent>(OnHealthChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<CombatStartedEvent>(OnCombatStarted);
            EventBus.Unsubscribe<CombatantHealthChangedEvent>(OnHealthChanged);
        }

        private void Update()
        {
            if (punchTime >= punchDuration)
            {
                return;
            }

            punchTime += Time.deltaTime;
            float progress = Mathf.Clamp01(punchTime / punchDuration);
            transform.localScale = baseScale * Mathf.Lerp(punchScale, 1f, progress);
        }

        private void OnCombatStarted(CombatStartedEvent started)
        {
            combatant = side == CombatSide.Player ? started.Player : (Combatant)started.Enemy;
            Refresh();
        }

        private void OnHealthChanged(CombatantHealthChangedEvent changed)
        {
            if (changed.Combatant != combatant)
            {
                return;
            }

            Refresh();
            punchTime = 0f;
        }

        private void Refresh()
        {
            label.text = string.Format(HealthFormat, combatant.Health.Current, combatant.Health.Max);
        }
    }
}
