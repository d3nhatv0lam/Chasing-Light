using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Điều hướng luồng từ Main Menu:
    /// Play Game -> DifficultySelect, Settings -> Settings, Exit -> Logout + về Login.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Scenes (phải có trong Build Settings)")]
        [SerializeField] private string _difficultySceneName = "DifficultySelect";
        [SerializeField] private string _settingsSceneName   = "Settings";
        [SerializeField] private string _loginSceneName      = "Login";

        [Header("Buttons")]
        [SerializeField] private Button _playGameButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _exitButton;

        [Header("Auth (tuỳ chọn)")]
        [Tooltip("Nếu scene có AuthService thì dùng nó để logout. Nếu không, dùng AuthConfig bên dưới để xoá session.")]
        [SerializeField] private AuthService _authService;
        [Tooltip("Dùng để xoá JWT trong PlayerPrefs khi scene không có AuthService.")]
        [SerializeField] private AuthConfig _authConfig;

        private void Awake()
        {
            if (_authService == null)
                _authService = FindFirstObjectByType<AuthService>();
        }

        private void OnEnable()
        {
            if (_playGameButton != null) _playGameButton.onClick.AddListener(OnPlayGameClicked);
            if (_settingsButton != null) _settingsButton.onClick.AddListener(OnSettingsClicked);
            if (_exitButton != null)     _exitButton.onClick.AddListener(OnExitClicked);
        }

        private void OnDisable()
        {
            if (_playGameButton != null) _playGameButton.onClick.RemoveListener(OnPlayGameClicked);
            if (_settingsButton != null) _settingsButton.onClick.RemoveListener(OnSettingsClicked);
            if (_exitButton != null)     _exitButton.onClick.RemoveListener(OnExitClicked);
        }

        private void OnPlayGameClicked() => LoadScene(_difficultySceneName);

        private void OnSettingsClicked() => LoadScene(_settingsSceneName);

        private void OnExitClicked()
        {
            Logout();
            LoadScene(_loginSceneName);
        }

        private void Logout()
        {
            if (_authService != null)
            {
                _authService.Logout();
                SaveManager.Instance.InitializeForGuest();
                return;
            }

            if (_authConfig != null)
            {
                PlayerPrefs.DeleteKey(_authConfig.PrefKeyJwt);
                PlayerPrefs.DeleteKey(_authConfig.PrefKeyEmail);
                PlayerPrefs.DeleteKey(_authConfig.PrefKeyUserId);
                PlayerPrefs.DeleteKey(_authConfig.PrefKeyRefresh);
                PlayerPrefs.Save();
                SaveManager.Instance.InitializeForGuest();
                return;
            }

            SaveManager.Instance.InitializeForGuest();
            Debug.LogWarning("[MainMenuController] Không có AuthService/AuthConfig, session chưa được xoá.", this);
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
