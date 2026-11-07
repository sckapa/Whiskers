using TMPro;
using UnityEngine;

namespace Whiskers.Combat.Presentation
{
    // Floating damage number
    // TODO(art): font and color
    [RequireComponent(typeof(TextMeshPro))]
    public sealed class DamagePopup : MonoBehaviour
    {
        private const string DamageFormat = "-{0}";

        [SerializeField, Min(0.01f)] private float lifetime = 0.9f;
        [SerializeField] private float riseDistance = 0.8f;
        [SerializeField] private AnimationCurve alphaOverLifetime = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        [SerializeField] private AnimationCurve scaleOverLifetime = AnimationCurve.EaseInOut(0f, 1.3f, 1f, 1f);

        private TMP_Text label;
        private Vector3 startPosition;
        private Vector3 startScale;
        private float age;

        public void SetSorting(int sortingLayerId, int sortingOrder)
        {
            TextMeshPro text = GetComponent<TextMeshPro>();
            text.sortingLayerID = sortingLayerId;
            text.sortingOrder = sortingOrder;
        }

        public void Play(int amount)
        {
            label = GetComponent<TMP_Text>();
            label.text = string.Format(DamageFormat, amount);
            startPosition = transform.position;
            startScale = transform.localScale;
            age = 0f;
        }

        private void Update()
        {
            if (label == null)
            {
                return;
            }

            age += Time.deltaTime;
            float progress = Mathf.Clamp01(age / lifetime);

            transform.position = startPosition + Vector3.up * (riseDistance * progress);
            transform.localScale = startScale * scaleOverLifetime.Evaluate(progress);
            label.alpha = alphaOverLifetime.Evaluate(progress);

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}
