using System.Collections.Generic;
using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Core;
using Whiskers.Core.Events;
using Whiskers.Dice.Rolling;

namespace Whiskers.Combat.Presentation
{
    // Shows open auras on the board display: one prefab instance per aura, following its centre,
    // scaled so a prefab 1 world unit wide covers the aura's diameter.
    public sealed class AuraPresenter : MonoBehaviour
    {
        private const float Diameter = 2f;

        [SerializeField] private DiceRollDisplay display = null;
        [Tooltip("1 world unit wide at scale 1, e.g. a circle sprite")]
        [SerializeField] private GameObject playerAuraPrefab = null;
        [Tooltip("Empty = the player prefab is used")]
        [SerializeField] private GameObject enemyAuraPrefab = null;
        [SerializeField] private int sortingOrderAboveDisplay = 5;

        private readonly Dictionary<ActiveAura, GameObject> viewsByAura = new Dictionary<ActiveAura, GameObject>();

        private void OnEnable()
        {
            EventBus.Subscribe<AuraStartedEvent>(OnAuraStarted);
            EventBus.Subscribe<AuraEndedEvent>(OnAuraEnded);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<AuraStartedEvent>(OnAuraStarted);
            EventBus.Unsubscribe<AuraEndedEvent>(OnAuraEnded);
        }

        private void Update()
        {
            if (viewsByAura.Count == 0 || BattlefieldManager.Instance == null || !BattlefieldManager.Instance.IsReady)
            {
                return;
            }

            BattlefieldBoard board = BattlefieldManager.Instance.Board;

            foreach (KeyValuePair<ActiveAura, GameObject> entry in viewsByAura)
            {
                Place(entry.Value.transform, entry.Key, board);
            }
        }

        private void OnAuraStarted(AuraStartedEvent started)
        {
            GameObject prefab = started.Aura.Owner == CombatSide.Enemy && enemyAuraPrefab != null ? enemyAuraPrefab : playerAuraPrefab;

            if (prefab == null || display == null)
            {
                return;
            }

            GameObject view = Instantiate(prefab, transform);
            SpriteRenderer displayRenderer = display.GetComponent<SpriteRenderer>();

            foreach (SpriteRenderer spriteRenderer in view.GetComponentsInChildren<SpriteRenderer>())
            {
                spriteRenderer.sortingLayerID = displayRenderer.sortingLayerID;
                spriteRenderer.sortingOrder = displayRenderer.sortingOrder + sortingOrderAboveDisplay;
            }

            viewsByAura[started.Aura] = view;

            if (BattlefieldManager.Instance != null && BattlefieldManager.Instance.IsReady)
            {
                Place(view.transform, started.Aura, BattlefieldManager.Instance.Board);
            }
        }

        private void OnAuraEnded(AuraEndedEvent ended)
        {
            if (viewsByAura.TryGetValue(ended.Aura, out GameObject view))
            {
                viewsByAura.Remove(ended.Aura);
                Destroy(view);
            }
        }

        private void Place(Transform view, ActiveAura aura, BattlefieldBoard board)
        {
            Vector3 center = display.NormalizedToWorld(board.ToNormalized(aura.Center));
            Vector3 edge = display.NormalizedToWorld(board.ToNormalized(aura.Center + Vector2.right * aura.Radius));
            float worldDiameter = Vector3.Distance(center, edge) * Diameter;

            view.position = center;
            view.localScale = new Vector3(worldDiameter, worldDiameter, 1f);
        }
    }
}
