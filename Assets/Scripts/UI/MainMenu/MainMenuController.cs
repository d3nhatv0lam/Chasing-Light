using ChasingLight.UI.Leaderboard;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Điều hướng luồng từ Main Menu:
    /// Play Game -> DifficultySelect, Settings -> Settings, Exit -> Thoát ứng dụng (giữ phiên đăng nhập).
    /// Leaderboard -> Mở Popup Leaderboard.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scenes (phải có trong Build Settings)")]
        [SerializeField] private string _difficultySceneName  = "DifficultySelect";
        [SerializeField] private string _settingsSceneName    = "Settings";

        [Header("Buttons")]
        [SerializeField] private Button _playGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _leaderboardButton;
        [SerializeField] private Button _exitButton;

        [Header("Leaderboard Popup")]
        [SerializeField] private LeaderboardPopupController _leaderboardPopup;

        private void Start()
        {
            AudioManager.Instance?.PlayBGM(SoundID.BGM_Main);
        }

        private void OnEnable()
        {
            if (_playGameButton != null)    _playGameButton.onClick.AddListener(OnPlayGameClicked);
            if (_settingsButton != null)    _settingsButton.onClick.AddListener(OnSettingsClicked);
            if (_leaderboardButton != null) _leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
            if (_exitButton != null)        _exitButton.onClick.AddListener(OnExitClicked);
        }

        private void OnDisable()
        {
            if (_playGameButton != null)    _playGameButton.onClick.RemoveListener(OnPlayGameClicked);
            if (_settingsButton != null)    _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (_leaderboardButton != null) _leaderboardButton.onClick.RemoveListener(OnLeaderboardClicked);
            if (_exitButton != null)        _exitButton.onClick.RemoveListener(OnExitClicked);
        }

        private void OnPlayGameClicked() => LoadScene(_difficultySceneName);

        private void OnSettingsClicked() => LoadScene(_settingsSceneName);

        private void OnLeaderboardClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);

            if (_leaderboardPopup == null)
            {
                _leaderboardPopup = FindFirstObjectByType<LeaderboardPopupController>(FindObjectsInactive.Include);
            }

            if (_leaderboardPopup != null)
            {
                _leaderboardPopup.OpenGlobal();
            }
            else
            {
                Debug.LogWarning("[MainMenuController] Leaderboard popup is not assigned in Inspector.", this);
            }
        }

        private void OnExitClicked()
        {
            PlayerPrefs.Save();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[MainMenuController] Tên scene trống.", this);
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogWarning($"[MainMenuController] Scene '{sceneName}' chưa tồn tại hoặc chưa thêm vào Build Settings.", this);
                return;
            }

            SceneManager.LoadScene(sceneName);
        }
    }
}
