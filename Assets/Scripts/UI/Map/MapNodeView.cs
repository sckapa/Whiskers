using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Whiskers.Run.Map;

namespace Whiskers.UI.Map
{
    public enum MapNodeState
    {
        Locked,
        Available,
        Completed,
        Current
    }

    // One node button on the map. Put it on a prefab (Button + Image, optional label / marker),
    // or let MapScreen create a plain one.
    public sealed class MapNodeView : MonoBehaviour
    {
        private const float FullTurnRadians = Mathf.PI * 2f;

        [SerializeField] private Button button = null;
        [SerializeField] private Image icon = null;
        [Tooltip("Optional, shows the type letter when there is no icon sprite")]
        [SerializeField] private TMP_Text label = null;
        [Tooltip("Optional, shown on the node the player stands on")]
        [SerializeField] private GameObject currentMarker = null;

        [Header("States")]
        [SerializeField] private Color lockedTint = new Color(0.45f, 0.45f, 0.45f, 0.8f);
        [SerializeField] private Color completedTint = new Color(0.6f, 0.6f, 0.6f, 1f);
        [SerializeField, Min(0f)] private float availablePulse = 0.12f;
        [SerializeField, Min(0.01f)] private float pulsePeriod = 1f;

        private Color baseColor = Color.white;
        private Vector3 baseScale;
        private MapNodeState state;
        private Action<MapNode> clicked;

        public MapNode Node { get; private set; }

        public void Configure(Button nodeButton, Image nodeIcon, TMP_Text nodeLabel)
        {
            button = nodeButton;
            icon = nodeIcon;
            label = nodeLabel;
        }

        public void Bind(MapNode node, Sprite sprite, Color color, string text, Action<MapNode> onClicked)
        {
            Node = node;
            clicked = onClicked;
            baseColor = color;
            baseScale = transform.localScale;

            if (icon != null && sprite != null)
            {
                icon.sprite = sprite;
                icon.preserveAspect = true;
            }

            if (label != null)
            {
                label.text = sprite != null ? string.Empty : text;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => clicked?.Invoke(Node));
        }

        public void SetState(MapNodeState newState)
        {
            state = newState;
            button.interactable = state == MapNodeState.Available;

            if (icon != null)
            {
                icon.color = state == MapNodeState.Locked ? baseColor * lockedTint
                    : state == MapNodeState.Completed ? baseColor * completedTint
                    : baseColor;
            }

            if (currentMarker != null)
            {
                currentMarker.SetActive(state == MapNodeState.Current);
            }

            transform.localScale = baseScale;
        }

        private void Update()
        {
            if (state != MapNodeState.Available)
            {
                return;
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime / pulsePeriod * FullTurnRadians) * availablePulse;
            transform.localScale = baseScale * pulse;
        }
    }
}
