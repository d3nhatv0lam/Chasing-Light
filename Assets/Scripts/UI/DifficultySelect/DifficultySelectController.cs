using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Scene DifficultySelect (mô hình 2 Panel trong 1 Scene):
    /// - Bước 1: Mặc định hiện DifficultyPanel, ẩn LevelListPanel.
    ///   + Tự động cập nhật trạng thái mở khóa cho các nút độ khó (Easy mở sẵn, Normal mở khi hết Easy, Hard mở khi hết Normal).
    ///   + Tự động kích hoạt làm mới kỷ lục từ Cloud nếu là tài khoản đăng nhập.
    /// - Bước 2: Bấm chọn độ khó (Easy/Normal/Hard) -> ẩn DifficultyPanel, hiện LevelListPanel với danh sách màn tương ứng.
    ///   + Mỗi màn hiển thị trạng thái khóa (LockOverlay) hoặc sao (Star0-3) qua StageButtonView.
    /// - Bước 3:
    ///   + Bấm nút Back/Close trên LevelListPanel -> ẩn LevelListPanel, hiện lại DifficultyPanel.
    ///   + Bấm nút Back trên DifficultyPanel -> quay về scene MainMenu.
    /// </summary>
    public sealed class DifficultySelectController : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("Debug")]
        [SerializeField] private bool _unlockAllStagesInEditor = true;
#endif
        [Header("Panels")]
        [Tooltip("Panel chọn độ khó (DifficultyPanel).")]
        [SerializeField] private GameObject _difficultyPanel;
        [Tooltip("Panel hiển thị danh sách màn chơi (LevelListPanel).")]
        [SerializeField] private GameObject _levelListPanel;

        [Header("Difficulty Buttons")]
        [SerializeField] private Button _easyButton;
        [SerializeField] private Button _normalButton;
        [SerializeField] private Button _hardButton;

        [Header("Difficulty Lock Overlays")]
        [SerializeField] private GameObject _normalLockOverlay;
        [SerializeField] private GameObject _hardLockOverlay;

        [Header("Navigation")]
        [Tooltip("Nút Đóng/Back trên LevelListPanel để quay lại DifficultyPanel.")]
        [SerializeField] private Button _backButton;
        [Tooltip("Nút Back trên màn chọn độ khó để quay về MainMenu.")]
        [SerializeField] private Button _backToMenuButton;

        [Header("Stage List")]
        [Tooltip("Container có GridLayoutGroup (StageListPanel).")]
        [SerializeField] private RectTransform _stageContainer;
        [Tooltip("Nguồn danh sách màn. Tự cập nhật bởi StageCatalogBuilder trong Editor.")]
        [SerializeField] private StageCatalog _stageCatalog;
        [Tooltip("Prefab nút màn chơi.")]
        [SerializeField] private StageButtonView _stageItemPrefab;

        [Header("Scenes (phải có trong Build Settings)")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        [SerializeField] private string _gameplaySceneName = "Game";

        private Difficulty _current;

        private void Awake()
        {
            if (_difficultyPanel == null && _easyButton != null && _easyButton.transform.parent != null)
                _difficultyPanel = _easyButton.transform.parent.gameObject;

            if (_levelListPanel == null && _stageContainer != null)
                _levelListPanel = _stageContainer.gameObject;

            // Tự động nhận diện nút Back về MainMenu nếu chưa gán trong Inspector
            if (_backToMenuButton == null)
            {
                foreach (var btn in GetComponentsInChildren<Button>(true))
                {
                    if (btn == _easyButton || btn == _normalButton || btn == _hardButton || btn == _backButton)
                        continue;
                    if (_stageContainer != null && btn.transform.IsChildOf(_stageContainer))
                        continue;

                    _backToMenuButton = btn;
                    break;
                }
            }

            if (_normalLockOverlay == null && _normalButton != null)
                _normalLockOverlay = _normalButton.transform.Find("LockOverlay")?.gameObject;
            if (_hardLockOverlay == null && _hardButton != null)
                _hardLockOverlay = _hardButton.transform.Find("LockOverlay")?.gameObject;
        }

        private void OnEnable()
        {
            Bind(_easyButton, Difficulty.Easy);
            Bind(_normalButton, Difficulty.Normal);
            Bind(_hardButton, Difficulty.Hard);

            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(OnBackClicked);
            }

            if (_backToMenuButton != null && _backToMenuButton != _backButton)
            {
                _backToMenuButton.onClick.RemoveAllListeners();
                _backToMenuButton.onClick.AddListener(LoadMainMenu);
            }

            SaveManager.Instance.OnSaveUpdated += HandleSaveUpdated;
        }

        private void OnDisable()
        {
            _easyButton.onClick.RemoveAllListeners();
            _normalButton.onClick.RemoveAllListeners();
            _hardButton.onClick.RemoveAllListeners();
            if (_backButton != null) _backButton.onClick.RemoveAllListeners();
            if (_backToMenuButton != null) _backToMenuButton.onClick.RemoveAllListeners();

            if (SaveManager.Instance != null)
                SaveManager.Instance.OnSaveUpdated -= HandleSaveUpdated;
        }

        private void Start()
        {
            AudioManager.Instance?.PlayBGM(SoundID.BGM_Main);

            if (!SaveManager.Instance.IsGuest)
            {
                SaveManager.Instance.RefreshFromCloud();
            }

            ShowDifficultyPanel();
        }

        private void Bind(Button button, Difficulty difficulty)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectDifficulty(difficulty));
        }

        public void ShowDifficultyPanel()
        {
            if (_levelListPanel != null) _levelListPanel.SetActive(false);
            if (_difficultyPanel != null) _difficultyPanel.SetActive(true);
            if (_backToMenuButton != null && _backToMenuButton != _backButton)
                _backToMenuButton.gameObject.SetActive(true);

            UpdateDifficultyButtons();
        }

        private void UpdateDifficultyButtons()
        {
            if (_easyButton != null)
                _easyButton.interactable = true;

            ApplyDifficultyState(_normalButton, _normalLockOverlay, IsDifficultyUnlocked(Difficulty.Normal));
            ApplyDifficultyState(_hardButton, _hardLockOverlay, IsDifficultyUnlocked(Difficulty.Hard));
        }

        private static void ApplyDifficultyState(Button button, GameObject lockOverlay, bool isUnlocked)
        {
            if (button != null)
                button.interactable = isUnlocked;

            if (lockOverlay != null)
                lockOverlay.SetActive(!isUnlocked);
        }

        public void SelectDifficulty(Difficulty difficulty)
        {
            _current = difficulty;
            StageContext.LastSelectedDifficulty = difficulty;

            RebuildList();

            if (_difficultyPanel != null) _difficultyPanel.SetActive(false);
            if (_backToMenuButton != null && _backToMenuButton != _backButton)
                _backToMenuButton.gameObject.SetActive(false);
            if (_levelListPanel != null) _levelListPanel.SetActive(true);
        }

        public void OnBackClicked()
        {
            if (_levelListPanel != null && _levelListPanel.activeSelf)
            {
                ShowDifficultyPanel();
                return;
            }

            LoadMainMenu();
        }

        public void LoadMainMenu()
        {
            if (string.IsNullOrEmpty(_mainMenuSceneName) || !Application.CanStreamedLevelBeLoaded(_mainMenuSceneName))
            {
                Debug.LogWarning($"[DifficultySelectController] Scene '{_mainMenuSceneName}' chưa có trong Build Settings.", this);
                return;
            }

            SceneManager.LoadScene(_mainMenuSceneName);
        }

        private void HandleSaveUpdated(GameSaveData _)
        {
            if (_levelListPanel != null && _levelListPanel.activeSelf)
                RebuildList();
            else if (_difficultyPanel != null && _difficultyPanel.activeSelf)
                UpdateDifficultyButtons();
        }

        private void RebuildList()
        {
            if (_stageContainer == null) return;

            for (int i = _stageContainer.childCount - 1; i >= 0; i--)
                Destroy(_stageContainer.GetChild(i).gameObject);

            foreach (var stage in GetStages(_current))
            {
                bool isUnlocked = IsStageUnlocked(stage);
                
                int stars = SaveManager.Instance.GetStageStars(stage.Id);

                var item = Instantiate(_stageItemPrefab, _stageContainer);
                item.Setup(stage, isUnlocked, stars, () => OnStageClicked(stage));
            }
        }

        private bool IsDifficultyUnlocked(Difficulty difficulty)
        {
#if UNITY_EDITOR
            if (_unlockAllStagesInEditor) return true;
#endif
            if (difficulty == Difficulty.Easy) return true;

            if (difficulty == Difficulty.Normal)
            {
                var easyStages = _stageCatalog.GetStages(Difficulty.Easy).ToList();
                return easyStages.Count > 0 && SaveManager.Instance.IsStageCompleted(easyStages[^1].Id);
            }

            if (difficulty == Difficulty.Hard)
            {
                var normalStages = _stageCatalog.GetStages(Difficulty.Normal).ToList();
                return normalStages.Count > 0 && SaveManager.Instance.IsStageCompleted(normalStages[^1].Id);
            }

            return false;
        }

        private bool IsStageUnlocked(StageAsset stage)
        {
#if UNITY_EDITOR
            if (_unlockAllStagesInEditor) return true;
#endif
            var prevStage = GetPreviousCampaignStage(stage);
            if (prevStage == null) return true; // Màn 1 Easy luôn mở

            return SaveManager.Instance.IsStageCompleted(prevStage.Id);
        }

        private StageAsset GetPreviousCampaignStage(StageAsset stage)
        {
            var currentDiffStages = _stageCatalog.GetStages(stage.Difficulty).ToList();
            int idx = currentDiffStages.IndexOf(stage);
            if (idx > 0) return currentDiffStages[idx - 1];

            // Nếu là màn đầu của Normal -> lấy màn cuối của Easy
            if (stage.Difficulty == Difficulty.Normal)
            {
                var easyStages = _stageCatalog.GetStages(Difficulty.Easy).ToList();
                return easyStages.Count > 0 ? easyStages[^1] : null;
            }

            // Nếu là màn đầu của Hard -> lấy màn cuối của Normal
            if (stage.Difficulty == Difficulty.Hard)
            {
                var normalStages = _stageCatalog.GetStages(Difficulty.Normal).ToList();
                return normalStages.Count > 0 ? normalStages[^1] : null;
            }

            return null; // Màn 1 của Easy không có màn trước
        }

        private IEnumerable<StageAsset> GetStages(Difficulty difficulty) =>
            _stageCatalog.GetStages(difficulty);

        private void OnStageClicked(StageAsset stage)
        {
            if (!Application.CanStreamedLevelBeLoaded(_gameplaySceneName))
            {
                Debug.LogWarning($"[DifficultySelectController] Scene '{_gameplaySceneName}' chưa có trong Build Settings.", this);
                return;
            }

            StageContext.SelectedStage = stage;
            SceneManager.LoadScene(_gameplaySceneName);
        }
    }
}
