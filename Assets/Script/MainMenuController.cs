using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CaseFit
{
    public class MainMenuController : MonoBehaviour
    {
        public const string VolumeKey = "CaseFit_Volume";

        [Header("Scenes")]
        [SerializeField] string gameSceneName = "CaseFit";

        [Header("Panels")]
        [SerializeField] GameObject settingsPanel;

        [Header("Audio")]
        [SerializeField] Slider volumeSlider;
        [SerializeField] TMP_Text volumeValueLabel;
        [Tooltip("Optional: a short blip played when the volume changes, so the level is audible.")]
        [SerializeField] AudioSource previewSource;

        void Start()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);

            float volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
            AudioListener.volume = volume;

            if (volumeSlider != null)
            {
                volumeSlider.minValue = 0f;
                volumeSlider.maxValue = 1f;
                volumeSlider.SetValueWithoutNotify(volume);
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }

            ShowVolume(volume);
        }

        public void StartGame()
        {
            if (string.IsNullOrEmpty(gameSceneName))
            {
                Debug.LogError("[Case Fit] MainMenuController: 'Game Scene Name' is empty.", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[Case Fit] Scene '{gameSceneName}' is not in the build list. " +
                               "Add it in File > Build Profiles > Scene List.", this);
                return;
            }

            SceneManager.LoadScene(gameSceneName);
        }

        public void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
        }

        public void ToggleSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        public void SetVolume(float value)
        {
            value = Mathf.Clamp01(value);
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(VolumeKey, value);
            PlayerPrefs.Save();
            ShowVolume(value);

            if (previewSource != null && value > 0f) previewSource.Play();
        }

        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        void ShowVolume(float value)
        {
            if (volumeValueLabel != null) volumeValueLabel.text = Mathf.RoundToInt(value * 100f) + "%";
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ApplySavedVolume()
        {
            AudioListener.volume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        }
    }
}
