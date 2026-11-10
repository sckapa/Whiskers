using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Whiskers.UI
{
    // Hovering selects, so pointer, keyboard and gamepad share one highlight
    [RequireComponent(typeof(Selectable))]
    public sealed class SelectOnHover : MonoBehaviour, IPointerEnterHandler
    {
        private Selectable selectable;

        private void Awake()
        {
            selectable = GetComponent<Selectable>();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (selectable.IsInteractable())
            {
                selectable.Select();
            }
        }
    }
}
