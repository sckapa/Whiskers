using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Whiskers.UI
{
    // Moves this marker beside the selected target, hidden when none of them is selected.
    // Expects a top-left pivot so the offset is measured from the target's top edge.
    [RequireComponent(typeof(RectTransform))]
    public sealed class SelectionCursor : MonoBehaviour
    {
        [SerializeField] private Graphic graphic = null;
        [SerializeField] private Selectable[] targets = new Selectable[0];
        [SerializeField] private float offsetBelowTop = 16f;

        private readonly Vector3[] corners = new Vector3[4];
        private RectTransform rectTransform;

        public void Configure(Graphic cursorGraphic, Selectable[] cursorTargets, float cursorOffsetBelowTop)
        {
            graphic = cursorGraphic;
            targets = cursorTargets;
            offsetBelowTop = cursorOffsetBelowTop;
        }

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
        }

        private void LateUpdate()
        {
            RectTransform target = FindSelectedTarget();

            if (graphic != null)
            {
                graphic.enabled = target != null;
            }

            if (target == null)
            {
                return;
            }

            // Corner 1 is top-left, converted into this marker's parent space
            target.GetWorldCorners(corners);
            Transform parent = rectTransform.parent;
            Vector3 targetTopLeft = parent != null ? parent.InverseTransformPoint(corners[1]) : corners[1];

            Vector3 position = rectTransform.localPosition;
            position.y = targetTopLeft.y - offsetBelowTop;
            rectTransform.localPosition = position;
        }

        private RectTransform FindSelectedTarget()
        {
            EventSystem eventSystem = EventSystem.current;

            if (eventSystem == null || eventSystem.currentSelectedGameObject == null)
            {
                return null;
            }

            GameObject selected = eventSystem.currentSelectedGameObject;

            foreach (Selectable target in targets)
            {
                if (target != null && target.gameObject == selected)
                {
                    return (RectTransform)target.transform;
                }
            }

            return null;
        }
    }
}
