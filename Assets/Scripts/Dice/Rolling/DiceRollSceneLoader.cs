using UnityEngine;
using UnityEngine.SceneManagement;

namespace Whiskers.Dice.Rolling
{
    // Loads DiceRoll additively, it must be in the build scene list.
    public sealed class DiceRollSceneLoader : MonoBehaviour
    {
        [SerializeField] private string diceSceneName = "DiceRoll";

        private void Start()
        {
            if (SceneManager.GetSceneByName(diceSceneName).isLoaded)
            {
                return;
            }

            SceneManager.LoadSceneAsync(diceSceneName, LoadSceneMode.Additive);
        }
    }
}
