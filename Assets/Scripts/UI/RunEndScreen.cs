using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Whiskers.Run;

namespace Whiskers.UI
{
    // Victory / defeat screen at the end of a run
    public sealed class RunEndScreen : MonoBehaviour
    {
        private const string ProgressFormat = "Nodes cleared: {0}";

        [SerializeField] private TMP_Text titleLabel = null;
        [SerializeField] private TMP_Text progressLabel = null;
        [SerializeField] private Button mainMenuButton = null;
        [SerializeField] private string victoryTitle = "Victory";
        [SerializeField] private string defeatTitle = "Defeat";

        private void Awake()
        {
            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenu);
            }
        }

        private void Start()
        {
            RunManager run = RunManager.Instance;

            if (titleLabel != null)
            {
                titleLabel.text = run != null && run.Outcome == RunOutcome.Victory ? victoryTitle : defeatTitle;
            }

            if (progressLabel != null && run != null)
            {
                progressLabel.text = string.Format(ProgressFormat, run.CompletedNodeCount);
            }
        }

        private void OnMainMenu()
        {
            if (RunManager.Instance != null)
            {
                RunManager.Instance.EndRun();
            }
        }
    }
}
