using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Scene DifficultySelect (mô hình 2 Panel trong 1 Scene):
    /// - Bước 1: Mặc định hiện DifficultyPanel, ẩn LevelListPanel.
    /// - Bước 2: Bấm chọn độ khó (Easy/Normal/Hard) -> ẩn DifficultyPanel, hiện LevelListPanel với danh sách màn tương ứng.
    /// - Bước 3:
    ///   + Bấm nút Back/Close trên LevelListPanel -> ẩn LevelListPanel, hiện lại DifficultyPanel.
    ///   + Bấm nút Back trên DifficultyPanel -> quay về scene MainMenu.
    /// </summary>
    public sealed class DifficultySelectController : MonoBehaviour
    {
        [Header("Panels")]
        [Tooltip("Panel chọn độ khó (DifficultyPanel).")]
        [SerializeField] private GameObject _difficultyPanel;
        [Tooltip("Panel hiển thị danh sách màn chơi (LevelListPanel).")]
        [SerializeField] private GameObject _levelListPanel;

        [Header("Difficulty Buttons")]
        [SerializeField] private Button _easyButton;
        [SerializeField] private Button _normalButton;
        [SerializeField] private Button _hardButton;

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
        [SerializeField] private Button _stageItemPrefab;

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
        }

        private void OnDisable()
        {
            if (_easyButton != null) _easyButton.onClick.RemoveAllListeners();
            if (_normalButton != null) _normalButton.onClick.RemoveAllListeners();
            if (_hardButton != null) _hardButton.onClick.RemoveAllListeners();
            if (_backButton != null) _backButton.onClick.RemoveAllListeners();
            if (_backToMenuButton != null) _backToMenuButton.onClick.RemoveAllListeners();
        }

        private void Start()
        {
            AudioManager.Instance?.PlayBGM(SoundID.BGM_Main);
            ShowDifficultyPanel();
        }

        private void Bind(Button button, Difficulty difficulty)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectDifficulty(difficulty));
        }

        public void ShowDifficultyPanel()
        {
            if (_levelListPanel != null) _levelListPanel.SetActive(false);
            if (_difficultyPanel != null) _difficultyPanel.SetActive(true);
            if (_backToMenuButton != null && _backToMenuButton != _backButton)
                _backToMenuButton.gameObject.SetActive(true);
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

        private void RebuildList()
        {
            if (_stageContainer == null) return;

            for (int i = _stageContainer.childCount - 1; i >= 0; i--)
                Destroy(_stageContainer.GetChild(i).gameObject);

            foreach (var stage in GetStages(_current))
            {
                CreateStageItem(stage);
            }
        }

        private IEnumerable<StageAsset> GetStages(Difficulty difficulty) =>
            _stageCatalog != null ? _stageCatalog.GetStages(difficulty) : System.Linq.Enumerable.Empty<StageAsset>();

        private void CreateStageItem(StageAsset stage)
        {
            if (_stageItemPrefab == null) return;

            var button = Instantiate(_stageItemPrefab, _stageContainer);
            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = $"Stage {stage.StageNumber}";

            button.onClick.AddListener(() => OnStageClicked(stage));
        }

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
