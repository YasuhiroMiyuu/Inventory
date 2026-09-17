using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CaseFit
{
    public class CaseFitWinScreen : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] TMP_Text starsLabel;

        [Header("Play Again")]
        [Tooltip("Leave empty to return to the scene the player just finished.")]
        [SerializeField] string gameSceneName = "";

        void Start()
        {
            if (starsLabel == null) return;

            int stars = PlayerPrefs.GetInt("CaseFit_TotalStars", 0);
            int max = Mathf.Max(1, PlayerPrefs.GetInt("CaseFit_MaxStars", 9));
            starsLabel.text = stars + " / " + max + " STARS";
        }

        public void PlayAgain()
        {
            string scene = string.IsNullOrEmpty(gameSceneName)
                ? PlayerPrefs.GetString("CaseFit_GameScene", "")
                : gameSceneName;

            if (string.IsNullOrEmpty(scene))
            {
                Debug.LogError("[Case Fit] CaseFitWinScreen: no game scene to return to. Fill in 'Game Scene Name'.", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError($"[Case Fit] Scene '{scene}' is not in the build list. Add it in File > Build Profiles > Scene List.", this);
                return;
            }

            SceneManager.LoadScene(scene);
        }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }
    }
}
