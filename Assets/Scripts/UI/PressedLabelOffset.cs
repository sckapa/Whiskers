using UnityEngine;
using UnityEngine.UI;

namespace Whiskers.UI
{
    // Drops the label while the button shows its pressed sprite, so it follows the face.
    // Reads the sprite because Selectable has no public state event, and this covers
    // pointer presses and keyboard or gamepad submits alike.
    [RequireComponent(typeof(Selectable))]
    public sealed class PressedLabelOffset : MonoBehaviour
    {
        [SerializeField] private RectTransform label = null;
        [SerializeField] private Vector2 pressedOffset = new Vector2(0f, -16f);

        private Selectable selectable;
        private Vector2 restPosition;
        private bool isDropped;

        public void Configure(RectTransform labelTransform, Vector2 offset)
        {
            label = labelTransform;
            pressedOffset = offset;
        }

        private void Awake()
        {
            selectable = GetComponent<Selectable>();

            if (label != null)
            {
                restPosition = label.anchoredPosition;
            }
        }

        private void OnDisable()
        {
            SetDropped(false);
        }

        private void LateUpdate()
        {
            SetDropped(IsShowingPressedSprite());
        }

        private bool IsShowingPressedSprite()
        {
            Sprite pressedSprite = selectable.spriteState.pressedSprite;
            Image image = selectable.image;

            return selectable.transition == Selectable.Transition.SpriteSwap
                && pressedSprite != null
                && image != null
                && image.overrideSprite == pressedSprite;
        }

        private void SetDropped(bool dropped)
        {
            if (label == null || dropped == isDropped)
            {
                return;
            }

            isDropped = dropped;
            label.anchoredPosition = dropped ? restPosition + pressedOffset : restPosition;
        }
    }
}
