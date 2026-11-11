using UnityEngine;

namespace Whiskers.UI
{
    // Steps a UI element up and down in whole art pixels, so pixel art never lands between pixels.
    // Each pattern entry is a height in art pixels held for one step. Rest position is the
    // authored one, so the edit-mode layout still matches the reference.
    [RequireComponent(typeof(RectTransform))]
    public sealed class PixelBob : MonoBehaviour
    {
        [SerializeField] private int[] pattern = { 0, 1, 1, 0 };
        [SerializeField] private float stepSeconds = 0.3f;
        [SerializeField] private float pixelSize = 8f;
        [SerializeField] private int delaySteps = 0;

        private RectTransform rectTransform;
        private Vector2 restPosition;

        public void Configure(int[] bobPattern, float bobStepSeconds, float bobPixelSize, int bobDelaySteps)
        {
            pattern = bobPattern;
            stepSeconds = bobStepSeconds;
            pixelSize = bobPixelSize;
            delaySteps = bobDelaySteps;
        }

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            restPosition = rectTransform.anchoredPosition;
        }

        private void OnDisable()
        {
            rectTransform.anchoredPosition = restPosition;
        }

        private void LateUpdate()
        {
            if (pattern == null || pattern.Length == 0 || stepSeconds <= 0f)
            {
                return;
            }

            // Unscaled so the menu keeps moving if something pauses time
            int step = Mathf.FloorToInt(Time.unscaledTime / stepSeconds) - delaySteps;
            int index = ((step % pattern.Length) + pattern.Length) % pattern.Length;
            rectTransform.anchoredPosition = restPosition + new Vector2(0f, pattern[index] * pixelSize);
        }
    }
}
