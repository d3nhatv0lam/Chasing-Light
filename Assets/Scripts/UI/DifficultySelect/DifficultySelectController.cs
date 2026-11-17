using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Scene DifficultySelect: bấm tab độ khó -> StageListPanel liệt kê các StageAsset cùng độ khó.
    /// Chọn một màn -> lưu vào StageContext và load scene Game.
    /// </summary>
    public sealed class DifficultySelectController : MonoBehaviour
    {
        [Header("Difficulty Tabs")]
        [SerializeField] private Button _easyButton;
        [SerializeField] private Button _normalButton;
        [SerializeField] private Button _hardButton;

        [Header("Stage List")]
        [Tooltip("Container có GridLayoutGroup (StageListPanel).")]
        [SerializeField] private RectTransform _stageContainer;
        [Tooltip("Nguồn danh sách màn. Tự cập nhật bởi StageCatalogBuilder trong Editor.")]
        [SerializeField] private StageCatalog _stageCatalog;
        [Tooltip("Prefab nút màn chơi.")]
        [SerializeField] private Button _stageItemPrefab;

        [Header("Scenes (phải có trong Build Settings)")]
        [SerializeField] private string _gameplaySceneName = "Game";

        [Header("Tab Visual")]
        [SerializeField] private float _selectedTabScale = 1.1f;

        private Difficulty _current;

        private void OnEnable()
        {
            Bind(_easyButton, Difficulty.Easy);
            Bind(_normalButton, Difficulty.Normal);
            Bind(_hardButton, Difficulty.Hard);
        }

        private void OnDisable()
        {
            if (_easyButton != null) _easyButton.onClick.RemoveAllListeners();
            if (_normalButton != null) _normalButton.onClick.RemoveAllListeners();
            if (_hardButton != null) _hardButton.onClick.RemoveAllListeners();
        }

        private void Start()
        {
            AudioManager.Instance?.PlayBGM(SoundID.BGM_Main);
            SelectDifficulty(StageContext.LastSelectedDifficulty);
        }

        private void Bind(Button button, Difficulty difficulty)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                AudioManager.Instance?.PlayUI(SoundID.UI_Click);
                SelectDifficulty(difficulty);
            });
        }

        public void SelectDifficulty(Difficulty difficulty)
        {
            _current = difficulty;
            StageContext.LastSelectedDifficulty = difficulty;

            SetTabScale(_easyButton, difficulty == Difficulty.Easy);
            SetTabScale(_normalButton, difficulty == Difficulty.Normal);
            SetTabScale(_hardButton, difficulty == Difficulty.Hard);

            RebuildList();
        }

        private void SetTabScale(Button button, bool selected)
        {
            if (button == null) return;
            button.transform.localScale = Vector3.one * (selected ? _selectedTabScale : 1f);
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
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);

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
