using UnityEngine;
using UnityEngine.UI;
using Whiskers.Run;

namespace Whiskers.UI
{
    // Main menu buttons. Wired in code, the OnClick lists stay empty.
    public sealed class MainMenuScreen : MonoBehaviour
    {
        [SerializeField] private Button newRunButton = null;
        [SerializeField] private Button exitButton = null;

        private void Awake()
        {
            if (newRunButton != null)
            {
                newRunButton.onClick.AddListener(OnNewRun);
            }

            if (exitButton != null)
            {
                exitButton.onClick.AddListener(OnExit);
            }
        }

        private void OnNewRun()
        {
            // RunManager lives in Bootstrap, the game has to be started from there
            if (RunManager.Instance != null)
            {
                RunManager.Instance.StartNewRun();
            }
        }

        private static void OnExit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
