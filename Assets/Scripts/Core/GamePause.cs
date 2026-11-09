using UnityEngine;
using UnityEngine.InputSystem;
using Whiskers.Core.Events;

namespace Whiskers.Core
{
    public readonly struct GamePauseChangedEvent : IGameEvent
    {
        public bool IsPaused { get; }

        public GamePauseChangedEvent(bool isPaused)
        {
            IsPaused = isPaused;
        }
    }

    // Stops game time and audio. Toggle with the pause button or Escape.
    // Anything that must keep working while paused (UI tweens) uses unscaled time.
    public sealed class GamePause : Singleton<GamePause>
    {
        private const float StoppedTimeScale = 0f;

        [SerializeField] private bool escapeToggles = true;

        private float timeScaleBeforePause = 1f;
        private bool paused;

        public static bool IsPaused => Instance != null && Instance.paused;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (escapeToggles && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Toggle();
            }
        }

        protected override void OnDestroy()
        {
            // Never leave the game frozen when the scene unloads
            if (paused)
            {
                SetPaused(false);
            }

            base.OnDestroy();
        }

        public void Toggle()
        {
            SetPaused(!paused);
        }

        public void Pause()
        {
            SetPaused(true);
        }

        public void Resume()
        {
            SetPaused(false);
        }

        public void SetPaused(bool value)
        {
            if (paused == value)
            {
                return;
            }

            paused = value;

            if (paused)
            {
                timeScaleBeforePause = Time.timeScale;
                Time.timeScale = StoppedTimeScale;
            }
            else
            {
                Time.timeScale = timeScaleBeforePause;
            }

            AudioListener.pause = paused;
            EventBus.Publish(new GamePauseChangedEvent(paused));
        }
    }
}
