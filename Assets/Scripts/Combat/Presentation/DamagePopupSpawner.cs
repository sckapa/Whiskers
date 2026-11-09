using System.Collections;
using UnityEngine;
using Whiskers.Battlefield;
using Whiskers.Combat.Interactions;
using Whiskers.Combatants;
using Whiskers.Core;
using Whiskers.Dice.Rolling;

namespace Whiskers.Combat.Presentation
{
    // Damage feedback. With flights on: a visual effect (or the number itself) flies from the impact point on the board
    // to the target's DamageTargetAnchor (HP bar), there the impact effect and the number play, and only then
    // the HP bar, HP text and hit reaction update (DisplayedHealth).
    // Without an anchor for the target side the number plays on the board as before.
    public sealed class DamagePopupSpawner : MonoBehaviour
    {
        [SerializeField] private DiceRollDisplay display = null;
        [SerializeField] private DamagePopup popupPrefab = null;
        [SerializeField] private int sortingOrderAboveDisplay = 10;

        [Header("Flight to the HP bar")]
        [SerializeField] private bool flyToHealth = true;
        [Tooltip("Board camera, Camera.main if empty")]
        [SerializeField] private Camera worldCamera = null;
        [Tooltip("What flies. Empty = the damage number flies")]
        [SerializeField] private GameObject projectilePrefab = null;
        [Tooltip("Optional, spawned where the projectile lands")]
        [SerializeField] private GameObject impactPrefab = null;
        [SerializeField, Min(0f)] private float impactLifetime = 1f;
        [SerializeField, Min(0.01f)] private float flightDuration = 0.45f;
        [Tooltip("World units the path bends upward")]
        [SerializeField] private float arcHeight = 1.2f;
        [SerializeField] private AnimationCurve flightEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Number position relative to the anchor, e.g. next to the bar so the UI does not cover it")]
        [SerializeField] private Vector3 numberOffset = Vector3.zero;

        private bool isDelaying;

        private void OnEnable()
        {
            if (flyToHealth)
            {
                DisplayedHealth.RegisterDelayer();
                isDelaying = true;
            }
        }

        private void OnDisable()
        {
            if (isDelaying)
            {
                DisplayedHealth.UnregisterDelayer();
                isDelaying = false;
            }
        }

        public void Show(DamageReport damage)
        {
            if (popupPrefab == null || display == null)
            {
                DisplayedHealth.ReleaseNext(damage.Target);
                return;
            }

            if (BattlefieldManager.Instance == null || !BattlefieldManager.Instance.IsReady)
            {
                DisplayedHealth.ReleaseNext(damage.Target);
                return;
            }

            Vector2 normalized = BattlefieldManager.Instance.Board.ToNormalized(damage.Position);
            Vector3 start = display.NormalizedToWorld(normalized);

            if (flyToHealth && damage.Target != null && DamageTargetAnchor.TryGet(damage.Target.Side, out DamageTargetAnchor anchor))
            {
                StartCoroutine(Fly(damage, start, anchor));
                return;
            }

            DisplayedHealth.ReleaseNext(damage.Target);
            SpawnPopup(start).Play(damage.Dealt, damage.WasBlocked);
        }

        private IEnumerator Fly(DamageReport damage, Vector3 start, DamageTargetAnchor anchor)
        {
            PresentationGate.Begin();

            try
            {
                yield return FlySteps(damage, start, anchor);
            }
            finally
            {
                PresentationGate.End();
            }
        }

        private IEnumerator FlySteps(DamageReport damage, Vector3 start, DamageTargetAnchor anchor)
        {
            Combatant target = damage.Target;
            Camera cameraToUse = worldCamera != null ? worldCamera : Camera.main;

            // The number itself flies when there is no projectile
            DamagePopup flyingNumber = null;
            GameObject flying;

            if (projectilePrefab != null)
            {
                flying = Instantiate(projectilePrefab, start, Quaternion.identity, transform);
            }
            else
            {
                flyingNumber = SpawnPopup(start);
                flyingNumber.ShowStill(damage.Dealt, damage.WasBlocked);
                flying = flyingNumber.gameObject;
            }

            Vector3 end = start;

            for (float time = 0f; time < flightDuration; time += Time.deltaTime)
            {
                // Every frame, the bar may move (layout, screen size)
                end = GetEnd(anchor, cameraToUse, start.z, end);
                float progress = flightEase.Evaluate(Mathf.Clamp01(time / flightDuration));
                Vector3 position = Vector3.Lerp(start, end, progress);
                position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;

                if (flying != null)
                {
                    flying.transform.position = position;
                }

                yield return null;
            }

            end = GetEnd(anchor, cameraToUse, start.z, end);
            DisplayedHealth.ReleaseNext(target);

            if (impactPrefab != null)
            {
                Destroy(Instantiate(impactPrefab, end, Quaternion.identity, transform), impactLifetime);
            }

            if (flyingNumber != null)
            {
                flyingNumber.transform.position = end + numberOffset;
                flyingNumber.Play(damage.Dealt, damage.WasBlocked);
                yield break;
            }

            if (flying != null)
            {
                Destroy(flying);
            }

            SpawnPopup(end + numberOffset).Play(damage.Dealt, damage.WasBlocked);
        }

        // The anchor can disappear mid-flight (scene change), then the last known point is kept
        private static Vector3 GetEnd(DamageTargetAnchor anchor, Camera cameraToUse, float worldZ, Vector3 lastEnd)
        {
            return anchor != null ? anchor.GetWorldPosition(cameraToUse, worldZ) : lastEnd;
        }

        private DamagePopup SpawnPopup(Vector3 position)
        {
            DamagePopup popup = Instantiate(popupPrefab, position, Quaternion.identity, transform);
            SpriteRenderer displayRenderer = display.GetComponent<SpriteRenderer>();
            popup.SetSorting(displayRenderer.sortingLayerID, displayRenderer.sortingOrder + sortingOrderAboveDisplay);
            return popup;
        }
    }
}
