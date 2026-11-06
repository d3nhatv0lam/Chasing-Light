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
        [Tooltip("Tuỳ chọn: prefab nút màn chơi. Để trống thì tạo nút bằng code.")]
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
            _easyButton?.onClick.RemoveAllListeners();
            _normalButton?.onClick.RemoveAllListeners();
            _hardButton?.onClick.RemoveAllListeners();
        }

        private void Start() => SelectDifficulty(StageContext.LastSelectedDifficulty);

        private void Bind(Button button, Difficulty difficulty)
        {
            if (button == null) return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectDifficulty(difficulty));
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

            int count = 0;
            foreach (var stage in GetStages(_current))
            {
                CreateStageItem(stage);
                count++;
            }

            if (count == 0)
                CreateLabel("Chưa có màn chơi");
        }

        private IEnumerable<StageAsset> GetStages(Difficulty difficulty)
        {
            if (_stageCatalog != null)
            {
                var list = new List<StageAsset>(_stageCatalog.GetStages(difficulty));
                if (list.Count > 0) return list;
            }

#if UNITY_EDITOR
            // Fallback trong Editor khi catalog chưa được cập nhật (vd. vừa merge nhánh).
            var found = new List<StageAsset>();
            foreach (var guid in UnityEditor.AssetDatabase.FindAssets("t:StageAsset"))
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<StageAsset>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null && asset.Difficulty == difficulty) found.Add(asset);
            }

            found.Sort((a, b) => a.StageNumber.CompareTo(b.StageNumber));
            return found;
#else
            return new List<StageAsset>();
#endif
        }

        private void CreateStageItem(StageAsset stage)
        {
            Button button;
            if (_stageItemPrefab != null)
            {
                button = Instantiate(_stageItemPrefab, _stageContainer);
            }
            else
            {
                var go = new GameObject($"Stage_{stage.StageNumber}", typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(_stageContainer, false);
                go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.2f, 0.9f);
                button = go.GetComponent<Button>();
                button.targetGraphic = go.GetComponent<Image>();
                CreateText(go.transform, $"Stage {stage.StageNumber}");
            }

            var captured = stage;
            button.onClick.AddListener(() => OnStageClicked(captured));

            var label = button.GetComponentInChildren<TMP_Text>();
            if (label != null) label.text = $"Stage {stage.StageNumber}";
        }

        private void CreateLabel(string text)
        {
            var go = new GameObject("EmptyLabel", typeof(RectTransform));
            go.transform.SetParent(_stageContainer, false);
            CreateText(go.transform, text);
        }

        private static void CreateText(Transform parent, string text)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 36;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
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

