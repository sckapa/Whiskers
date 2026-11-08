using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Whiskers.Core.Events;
using Whiskers.Core.GameStates;

namespace Whiskers.Core
{
    // Loads the scene of every game state. Lives in Bootstrap next to GameManager.
    // Leaving Combat waits a little, so the last hit and the death animation are seen.
    public sealed class SceneFlow : Singleton<SceneFlow>
    {
        private const float Transparent = 0f;
        private const float Opaque = 1f;

        [Serializable]
        private sealed class StateScene
        {
            public GameState state = GameState.MainMenu;
            public string sceneName = string.Empty;
        }

        [SerializeField] private List<StateScene> scenes = new List<StateScene>
        {
            new StateScene { state = GameState.MainMenu, sceneName = "MainMenu" },
            new StateScene { state = GameState.Run, sceneName = "Map" },
            new StateScene { state = GameState.Combat, sceneName = "Combat" },
            new StateScene { state = GameState.Victory, sceneName = "RunEnd" },
            new StateScene { state = GameState.Defeat, sceneName = "RunEnd" }
        };

        [Tooltip("Pause after the last animation of a fight (death, victory) before leaving the Combat scene")]
        [SerializeField, Min(0f)] private float delayAfterCombat = 1.5f;
        [Tooltip("Leave anyway after this many seconds of waiting for animations (pause time not counted)")]
        [SerializeField, Min(0f)] private float maxWaitForAnimations = 10f;

        [Header("Optional fade")]
        [Tooltip("Full-screen CanvasGroup on a persistent canvas (child of this object), black image inside")]
        [SerializeField] private CanvasGroup fade = null;
        [SerializeField, Min(0f)] private float fadeDuration = 0.25f;

        private Coroutine loading;
        private GameState queuedState = GameState.None;

        public bool IsLoading => loading != null;

        protected override bool PersistAcrossScenes => true;

        protected override void OnAwake()
        {
            if (fade != null)
            {
                fade.alpha = Transparent;
                fade.blocksRaycasts = false;
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameStateChangedEvent>(OnStateChanged);
        }

        private void OnStateChanged(GameStateChangedEvent changed)
        {
            if (!TryGetScene(changed.CurrentState, out string sceneName))
            {
                return;
            }

            // A state change during a load is applied after it
            if (loading != null)
            {
                queuedState = changed.CurrentState;
                return;
            }

            bool afterCombat = changed.PreviousState == GameState.Combat;
            loading = StartCoroutine(Load(sceneName, afterCombat));
        }

        private IEnumerator Load(string sceneName, bool afterCombat)
        {
            if (afterCombat)
            {
                yield return WaitForCombatVisuals();
            }

            yield return Fade(Opaque);

            if (SceneManager.GetActiveScene().name != sceneName)
            {
                // Null when the scene is not in the build scene list
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

                if (operation != null)
                {
                    yield return operation;
                }
            }

            PresentationGate.Reset();
            yield return Fade(Transparent);
            loading = null;

            if (queuedState != GameState.None)
            {
                GameState next = queuedState;
                queuedState = GameState.None;

                if (TryGetScene(next, out string nextScene))
                {
                    loading = StartCoroutine(Load(nextScene, false));
                }
            }
        }

        // The killing blow is resolved before it is shown: wait for its animation, the damage flight and the death
        private IEnumerator WaitForCombatVisuals()
        {
            // One frame, so the visuals of the last hit have started
            yield return null;

            float waited = 0f;

            while (PresentationGate.IsBusy && waited < maxWaitForAnimations)
            {
                if (!GamePause.IsPaused)
                {
                    waited += Time.unscaledDeltaTime;
                }

                yield return null;
            }

            for (float time = 0f; time < delayAfterCombat; time += GamePause.IsPaused ? 0f : Time.unscaledDeltaTime)
            {
                yield return null;
            }
        }

        private IEnumerator Fade(float target)
        {
            if (fade == null)
            {
                yield break;
            }

            fade.blocksRaycasts = true;
            float start = fade.alpha;

            for (float time = 0f; time < fadeDuration; time += Time.unscaledDeltaTime)
            {
                fade.alpha = Mathf.Lerp(start, target, time / fadeDuration);
                yield return null;
            }

            fade.alpha = target;
            fade.blocksRaycasts = target > Transparent;
        }

        private bool TryGetScene(GameState state, out string sceneName)
        {
            StateScene entry = scenes.Find(candidate => candidate.state == state);
            sceneName = entry != null ? entry.sceneName : null;
            return !string.IsNullOrEmpty(sceneName);
        }
    }
}
