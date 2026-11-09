using System.Collections.Generic;
using UnityEngine;
using Whiskers.Core;

namespace Whiskers.Combat.Presentation
{
    // Where damage flights of one side land: put it on the HP bar (UI) or on any world object.
    public sealed class DamageTargetAnchor : MonoBehaviour
    {
        private static readonly Dictionary<CombatSide, DamageTargetAnchor> anchorsBySide = new Dictionary<CombatSide, DamageTargetAnchor>();

        [SerializeField] private CombatSide side = CombatSide.Player;

        private Canvas canvas;

        public static bool TryGet(CombatSide side, out DamageTargetAnchor anchor)
        {
            return anchorsBySide.TryGetValue(side, out anchor) && anchor != null;
        }

        private void OnEnable()
        {
            canvas = GetComponentInParent<Canvas>();
            anchorsBySide[side] = this;
        }

        private void OnDisable()
        {
            if (anchorsBySide.TryGetValue(side, out DamageTargetAnchor registered) && registered == this)
            {
                anchorsBySide.Remove(side);
            }
        }

        // World point on the plane at worldZ, seen by worldCamera. UI anchors are converted through the screen.
        public Vector3 GetWorldPosition(Camera worldCamera, float worldZ)
        {
            if (!(transform is RectTransform) || canvas == null || canvas.renderMode == RenderMode.WorldSpace || worldCamera == null)
            {
                Vector3 position = transform.position;
                return new Vector3(position.x, position.y, worldZ);
            }

            Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, transform.position);
            float depth = worldZ - worldCamera.transform.position.z;

            return worldCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        }
    }
}
