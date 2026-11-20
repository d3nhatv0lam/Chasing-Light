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
    /// - Bước 3: Bấm nút Back -> ẩn LevelListPanel, hiện lại DifficultyPanel (không cần load lại scene).
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
        [Tooltip("Nút Back trên LevelListPanel để quay lại DifficultyPanel.")]
        [SerializeField] private Button _backButton;

        [Header("Stage List")]
        [Tooltip("Container có GridLayoutGroup (StageListPanel).")]
        [SerializeField] private RectTransform _stageContainer;
        [Tooltip("Nguồn danh sách màn. Tự cập nhật bởi StageCatalogBuilder trong Editor.")]
        [SerializeField] private StageCatalog _stageCatalog;
        [Tooltip("Prefab nút màn chơi.")]
        [SerializeField] private Button _stageItemPrefab;

        [Header("Scenes (phải có trong Build Settings)")]
        [SerializeField] private string _gameplaySceneName = "Game";

        private Difficulty _current;

        private void Awake()
        {
            if (_difficultyPanel == null && _easyButton != null && _easyButton.transform.parent != null)
                _difficultyPanel = _easyButton.transform.parent.gameObject;

            if (_levelListPanel == null && _stageContainer != null)
                _levelListPanel = _stageContainer.gameObject;
        }

        private void OnEnable()
        {
            Bind(_easyButton, Difficulty.Easy);
            Bind(_normalButton, Difficulty.Normal);
            Bind(_hardButton, Difficulty.Hard);

            if (_backButton != null)
            {
                _backButton.onClick.RemoveAllListeners();
                _backButton.onClick.AddListener(ShowDifficultyPanel);
            }
        }

        private void OnDisable()
        {
            if (_easyButton != null) _easyButton.onClick.RemoveAllListeners();
            if (_normalButton != null) _normalButton.onClick.RemoveAllListeners();
            if (_hardButton != null) _hardButton.onClick.RemoveAllListeners();
            if (_backButton != null) _backButton.onClick.RemoveAllListeners();
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
        }

        public void SelectDifficulty(Difficulty difficulty)
        {
            _current = difficulty;
            StageContext.LastSelectedDifficulty = difficulty;

            RebuildList();

            if (_difficultyPanel != null) _difficultyPanel.SetActive(false);
            if (_levelListPanel != null) _levelListPanel.SetActive(true);
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
