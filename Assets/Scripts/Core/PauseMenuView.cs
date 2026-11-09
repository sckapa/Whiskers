using UnityEngine;
using UnityEngine.UI;
using Whiskers.Core.Events;

namespace Whiskers.Core
{
    // Pause button + pause panel. Buttons are wired in code, nothing to set up in the OnClick lists.
    public sealed class PauseMenuView : MonoBehaviour
    {
        [SerializeField] private Button pauseButton = null;
        [Tooltip("Shown while paused")]
        [SerializeField] private GameObject pausePanel = null;
        [SerializeField] private Button resumeButton = null;

        private void Awake()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GamePauseChangedEvent>(OnPauseChanged);
            ShowPanel(GamePause.IsPaused);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GamePauseChangedEvent>(OnPauseChanged);
        }

        private void OnPauseClicked()
        {
            if (GamePause.Instance != null)
            {
                GamePause.Instance.Toggle();
            }
        }

        private void OnResumeClicked()
        {
            if (GamePause.Instance != null)
            {
                GamePause.Instance.Resume();
            }
        }

        private void OnPauseChanged(GamePauseChangedEvent changed)
        {
            ShowPanel(changed.IsPaused);
        }

        private void ShowPanel(bool visible)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(visible);
            }
        }
    }
}
