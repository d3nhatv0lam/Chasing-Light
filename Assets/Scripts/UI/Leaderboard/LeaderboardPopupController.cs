using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChasingLight.Network.Clients;
using ChasingLight.Network.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight.UI.Leaderboard
{
    public enum LeaderboardTab
    {
        Global = 0,
        Stage = 1
    }

    /// <summary>
    /// Điều khiển toàn bộ giao diện Leaderboard Popup:
    /// - Quản lý 2 Tabs: Toàn Cầu (Global) và Theo Màn (Stage)
    /// - Bộ chọn màn (Stage Selector) liên kết với StageCatalog
    /// - Hiển thị danh sách kết quả qua LeaderboardItemView
    /// - Thanh My Rank cố định dưới đáy hiển thị thành tích của người chơi
    /// - Trạng thái: Loading, Empty, Error + Nút Retry
    /// </summary>
    public class LeaderboardPopupController : MonoBehaviour
    {
        [Header("Configuration & Data")]
        [SerializeField] private AuthConfig _config;
        [SerializeField] private StageCatalog _stageCatalog;

        [Header("Modal Backdrop & Panels")]
        [SerializeField] private GameObject _rootModal;
        [SerializeField] private Button _backdropButton;
        [SerializeField] private Button _closeButton;

        [Header("Tabs")]
        [SerializeField] private Button _globalTabButton;
        [SerializeField] private Button _stageTabButton;
        [SerializeField] private Image _globalTabBg;
        [SerializeField] private Image _stageTabBg;
        [SerializeField] private Color _activeTabColor = new Color(0.25f, 0.55f, 0.95f, 1f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.18f, 0.22f, 0.28f, 0.8f);

        [Header("Stage Selector (Chỉ dùng cho Tab Stage)")]
        [SerializeField] private GameObject _stageSelectorPanel;
        [SerializeField] private Button _easyDiffButton;
        [SerializeField] private Button _normalDiffButton;
        [SerializeField] private Button _hardDiffButton;
        [SerializeField] private Button _prevStageButton;
        [SerializeField] private Button _nextStageButton;
        [SerializeField] private TMP_Text _stageTitleText;

        [Header("Scroll List")]
        [SerializeField] private RectTransform _contentContainer;
        [SerializeField] private LeaderboardItemView _itemPrefab;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("UX States")]
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private GameObject _emptyPanel;
        [SerializeField] private TMP_Text _emptyMessageText;
        [SerializeField] private GameObject _errorPanel;
        [SerializeField] private TMP_Text _errorMessageText;
        [SerializeField] private Button _retryButton;

        [Header("My Rank Pinned Footer")]
        [SerializeField] private GameObject _myRankFooter;
        [SerializeField] private LeaderboardItemView _myRankItemView;
        [SerializeField] private TMP_Text _guestNoticeText;

        [Header("Settings")]
        [SerializeField] private int _limit = 50;

        // ── Runtime State ─────────────────────────────────────────────────────
        private LeaderboardTab _currentTab = LeaderboardTab.Global;
        private Difficulty _currentDifficulty = Difficulty.Easy;
        private List<StageAsset> _currentStages = new();
        private int _currentStageIndex = 0;
        private Coroutine _fetchCoroutine;

        private void Awake()
        {
            if (_config == null)
            {
                _config = Resources.Load<AuthConfig>("AuthConfig");
            }

            if (_stageCatalog == null)
            {
                _stageCatalog = Resources.Load<StageCatalog>("StageCatalog");
            }
        }

        private void OnEnable()
        {
            RegisterListeners();
        }

        private void OnDisable()
        {
            UnregisterListeners();
            StopFetch();
        }

        private void Update()
        {
            // Cho phép đóng bằng phím Escape
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_rootModal != null && _rootModal.activeSelf)
                {
                    Close();
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Mở popup Leaderboard ở Tab Toàn cầu.
        /// </summary>
        public void OpenGlobal()
        {
            Open();
            SwitchTab(LeaderboardTab.Global);
        }

        /// <summary>
        /// Mở popup Leaderboard ở Tab Theo màn, trỏ tới màn chơi được chọn.
        /// </summary>
        public void OpenStage(StageAsset targetStage = null)
        {
            Open();
            SwitchTab(LeaderboardTab.Stage);

            if (targetStage != null)
            {
                SetSelectedStage(targetStage);
            }
            else
            {
                RefreshStageSelector();
                FetchCurrentData();
            }
        }

        /// <summary>
        /// Mở popup Leaderboard.
        /// </summary>
        public void Open()
        {
            if (_rootModal != null)
                _rootModal.SetActive(true);
            else
                gameObject.SetActive(true);

            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
        }

        /// <summary>
        /// Đóng popup Leaderboard.
        /// </summary>
        public void Close()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            StopFetch();

            if (_rootModal != null)
                _rootModal.SetActive(false);
            else
                gameObject.SetActive(false);
        }

        // ── Tab Management ────────────────────────────────────────────────────

        public void SwitchTab(LeaderboardTab tab)
        {
            _currentTab = tab;
            UpdateTabVisuals();

            if (_stageSelectorPanel != null)
                _stageSelectorPanel.SetActive(tab == LeaderboardTab.Stage);

            if (tab == LeaderboardTab.Stage)
            {
                RefreshStageSelector();
            }

            FetchCurrentData();
        }

        private void UpdateTabVisuals()
        {
            bool isGlobal = _currentTab == LeaderboardTab.Global;

            if (_globalTabBg != null)
                _globalTabBg.color = isGlobal ? _activeTabColor : _inactiveTabColor;

            if (_stageTabBg != null)
                _stageTabBg.color = !isGlobal ? _activeTabColor : _inactiveTabColor;
        }

        // ── Stage Selector Logic ──────────────────────────────────────────────

        public void SelectDifficulty(Difficulty difficulty)
        {
            _currentDifficulty = difficulty;
            _currentStageIndex = 0;
            RefreshStageSelector();
            FetchCurrentData();
        }

        private void SetSelectedStage(StageAsset stage)
        {
            if (stage == null) return;

            _currentDifficulty = stage.Difficulty;
            LoadStagesForDifficulty(_currentDifficulty);

            int foundIdx = _currentStages.FindIndex(s => s != null && s.Id == stage.Id);
            _currentStageIndex = foundIdx >= 0 ? foundIdx : 0;

            UpdateStageTitle();
            FetchCurrentData();
        }

        private void RefreshStageSelector()
        {
            LoadStagesForDifficulty(_currentDifficulty);

            if (_currentStageIndex >= _currentStages.Count)
                _currentStageIndex = Mathf.Max(0, _currentStages.Count - 1);

            UpdateStageTitle();
        }

        private void LoadStagesForDifficulty(Difficulty difficulty)
        {
            _currentStages.Clear();
            if (_stageCatalog != null)
            {
                _currentStages.AddRange(_stageCatalog.GetStages(difficulty));
            }
        }

        private void NextStage()
        {
            if (_currentStages.Count == 0) return;
            _currentStageIndex = (_currentStageIndex + 1) % _currentStages.Count;
            UpdateStageTitle();
            FetchCurrentData();
        }

        private void PrevStage()
        {
            if (_currentStages.Count == 0) return;
            _currentStageIndex = (_currentStageIndex - 1 + _currentStages.Count) % _currentStages.Count;
            UpdateStageTitle();
            FetchCurrentData();
        }

        private void UpdateStageTitle()
        {
            if (_stageTitleText == null) return;

            if (_currentStages.Count == 0 || _currentStageIndex >= _currentStages.Count)
            {
                _stageTitleText.text = $"{_currentDifficulty} - (Không có màn)";
                return;
            }

            var stage = _currentStages[_currentStageIndex];
            _stageTitleText.text = $"{_currentDifficulty} - Màn {stage.StageNumber}";
        }

        private StageAsset GetCurrentStage()
        {
            if (_currentStages != null && _currentStageIndex >= 0 && _currentStageIndex < _currentStages.Count)
                return _currentStages[_currentStageIndex];
            return null;
        }

        // ── Data Fetching ─────────────────────────────────────────────────────

        public void FetchCurrentData()
        {
            StopFetch();
            ClearList();
            ShowLoading(true);
            ShowEmpty(false);
            ShowError(false);

            string jwt = GetJwt();

            if (_currentTab == LeaderboardTab.Global)
            {
                _fetchCoroutine = StartCoroutine(
                    LeaderboardApiClient.GetGlobalLeaderboard(_config, _limit, OnGlobalLeaderboardLoaded, jwt));
            }
            else
            {
                var stage = GetCurrentStage();
                if (stage == null || string.IsNullOrEmpty(stage.Id))
                {
                    ShowLoading(false);
                    ShowEmpty(true, "Chưa chọn màn chơi hợp lệ.");
                    UpdateMyRankFooterGlobal(null);
                    return;
                }

                _fetchCoroutine = StartCoroutine(
                    LeaderboardApiClient.GetStageLeaderboard(_config, stage.Id, _limit, OnStageLeaderboardLoaded, jwt));
            }
        }

        private void OnGlobalLeaderboardLoaded(ApiResult<List<GlobalLeaderboardEntryDto>> result)
        {
            ShowLoading(false);

            if (!result.IsSuccess)
            {
                ShowError(true, result.ErrorMessage ?? "Không thể tải bảng xếp hạng.");
                return;
            }

            var list = result.Data ?? new List<GlobalLeaderboardEntryDto>();
            if (list.Count == 0)
            {
                ShowEmpty(true, "Chưa có ai ghi danh trên bảng xếp hạng toàn cầu!");
                UpdateMyRankFooterGlobal(null);
                return;
            }

            PopulateGlobalList(list);
        }

        private void OnStageLeaderboardLoaded(ApiResult<List<StageLeaderboardEntryDto>> result)
        {
            ShowLoading(false);

            if (!result.IsSuccess)
            {
                ShowError(true, result.ErrorMessage ?? "Không thể tải bảng xếp hạng.");
                return;
            }

            var list = result.Data ?? new List<StageLeaderboardEntryDto>();
            if (list.Count == 0)
            {
                ShowEmpty(true, "Chưa có người chơi nào hoàn thành màn này!");
                UpdateMyRankFooterStage(null);
                return;
            }

            PopulateStageList(list);
        }

        // ── List Population ───────────────────────────────────────────────────

        private void PopulateGlobalList(List<GlobalLeaderboardEntryDto> list)
        {
            string currentUserId = GetCurrentUserId();
            GlobalLeaderboardEntryDto myEntry = null;

            foreach (var item in list)
            {
                bool isSelf = !string.IsNullOrEmpty(currentUserId) && item.userId == currentUserId;
                if (isSelf) myEntry = item;

                var row = CreateItemView();
                row.BindGlobal(item, isSelf);
            }

            UpdateMyRankFooterGlobal(myEntry);
            ResetScrollPosition();
        }

        private void PopulateStageList(List<StageLeaderboardEntryDto> list)
        {
            string currentUserId = GetCurrentUserId();
            StageLeaderboardEntryDto myEntry = null;

            foreach (var item in list)
            {
                bool isSelf = !string.IsNullOrEmpty(currentUserId) && item.userId == currentUserId;
                if (isSelf) myEntry = item;

                var row = CreateItemView();
                row.BindStage(item, isSelf);
            }

            UpdateMyRankFooterStage(myEntry);
            ResetScrollPosition();
        }

        private LeaderboardItemView CreateItemView()
        {
            if (_itemPrefab == null || _contentContainer == null) return null;
            return Instantiate(_itemPrefab, _contentContainer);
        }

        private void ClearList()
        {
            if (_contentContainer == null) return;
            for (int i = _contentContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_contentContainer.GetChild(i).gameObject);
            }
        }

        private void ResetScrollPosition()
        {
            if (_scrollRect != null)
                _scrollRect.verticalNormalizedPosition = 1f;
        }

        // ── Pinned My Rank Footer ─────────────────────────────────────────────

        private void UpdateMyRankFooterGlobal(GlobalLeaderboardEntryDto myEntry)
        {
            if (_myRankFooter == null) return;

            string userId = GetCurrentUserId();
            bool isGuest = string.IsNullOrEmpty(userId) || (SaveManager.Instance != null && SaveManager.Instance.IsGuest);

            if (isGuest)
            {
                SetGuestFooterNotice("Đăng nhập tài khoản để đua top bảng xếp hạng!");
                return;
            }

            if (myEntry != null)
            {
                ShowMyRankItem(true);
                _myRankItemView.BindGlobal(myEntry, true);
            }
            else
            {
                int localStars = SaveManager.Instance?.CurrentSave?.totalStars ?? 0;
                var fallbackEntry = new GlobalLeaderboardEntryDto
                {
                    userId = userId,
                    displayName = "Bạn",
                    avatarUrl = null,
                    totalStars = localStars,
                    rank = 0 // Chưa vào top 50
                };
                ShowMyRankItem(true);
                _myRankItemView.BindGlobal(fallbackEntry, true);
            }
        }

        private void UpdateMyRankFooterStage(StageLeaderboardEntryDto myEntry)
        {
            if (_myRankFooter == null) return;

            string userId = GetCurrentUserId();
            bool isGuest = string.IsNullOrEmpty(userId) || (SaveManager.Instance != null && SaveManager.Instance.IsGuest);

            if (isGuest)
            {
                SetGuestFooterNotice("Đăng nhập tài khoản để lưu kỷ lục màn chơi!");
                return;
            }

            if (myEntry != null)
            {
                ShowMyRankItem(true);
                _myRankItemView.BindStage(myEntry, true);
            }
            else
            {
                // Thử tìm trong SaveManager local của user xem màn này đã hoàn thành chưa
                var currentStage = GetCurrentStage();
                var localRecord = currentStage != null && SaveManager.Instance != null
                    ? SaveManager.Instance.GetStageRecord(currentStage.Id)
                    : null;

                if (localRecord != null)
                {
                    var fallbackEntry = new StageLeaderboardEntryDto
                    {
                        userId = userId,
                        displayName = "Bạn",
                        avatarUrl = null,
                        bestTimeSeconds = localRecord.bestTimeSeconds,
                        bestStepCount = localRecord.bestStepCount,
                        starsEarned = (short)localRecord.starsEarned,
                        rank = 0
                    };
                    ShowMyRankItem(true);
                    _myRankItemView.BindStage(fallbackEntry, true);
                }
                else
                {
                    SetGuestFooterNotice("Bạn chưa hoàn thành màn chơi này.");
                }
            }
        }

        private void SetGuestFooterNotice(string message)
        {
            ShowMyRankItem(false);
            if (_guestNoticeText != null)
            {
                _guestNoticeText.gameObject.SetActive(true);
                _guestNoticeText.text = message;
            }
        }

        private void ShowMyRankItem(bool show)
        {
            if (_myRankItemView != null)
                _myRankItemView.gameObject.SetActive(show);

            if (_guestNoticeText != null && show)
                _guestNoticeText.gameObject.SetActive(false);
        }

        // ── Helpers & State Control ───────────────────────────────────────────

        private void ShowLoading(bool show)
        {
            if (_loadingPanel != null) _loadingPanel.SetActive(show);
        }

        private void ShowEmpty(bool show, string message = null)
        {
            if (_emptyPanel != null)
            {
                _emptyPanel.SetActive(show);
                if (show && _emptyMessageText != null && !string.IsNullOrEmpty(message))
                    _emptyMessageText.text = message;
            }
        }

        private void ShowError(bool show, string message = null)
        {
            if (_errorPanel != null)
            {
                _errorPanel.SetActive(show);
                if (show && _errorMessageText != null && !string.IsNullOrEmpty(message))
                    _errorMessageText.text = message;
            }
        }

        private void StopFetch()
        {
            if (_fetchCoroutine != null)
            {
                StopCoroutine(_fetchCoroutine);
                _fetchCoroutine = null;
            }
        }

        private string GetJwt()
        {
            if (_config == null) return null;
            return PlayerPrefs.GetString(_config.PrefKeyJwt, "");
        }

        private string GetCurrentUserId()
        {
            if (SaveManager.Instance != null && !SaveManager.Instance.IsGuest)
                return SaveManager.Instance.CurrentSave?.userId;

            if (_config != null)
                return PlayerPrefs.GetString(_config.PrefKeyUserId, "");

            return PlayerPrefs.GetString("auth_user_id", "");
        }

        // ── Event Bindings ────────────────────────────────────────────────────

        private void RegisterListeners()
        {
            if (_backdropButton != null) _backdropButton.onClick.AddListener(Close);
            if (_closeButton != null) _closeButton.onClick.AddListener(Close);

            if (_globalTabButton != null) _globalTabButton.onClick.AddListener(() => SwitchTab(LeaderboardTab.Global));
            if (_stageTabButton != null) _stageTabButton.onClick.AddListener(() => SwitchTab(LeaderboardTab.Stage));

            if (_prevStageButton != null) _prevStageButton.onClick.AddListener(PrevStage);
            if (_nextStageButton != null) _nextStageButton.onClick.AddListener(NextStage);

            if (_easyDiffButton != null) _easyDiffButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Easy));
            if (_normalDiffButton != null) _normalDiffButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Normal));
            if (_hardDiffButton != null) _hardDiffButton.onClick.AddListener(() => SelectDifficulty(Difficulty.Hard));

            if (_retryButton != null) _retryButton.onClick.AddListener(FetchCurrentData);
        }

        private void UnregisterListeners()
        {
            if (_backdropButton != null) _backdropButton.onClick.RemoveListener(Close);
            if (_closeButton != null) _closeButton.onClick.RemoveListener(Close);

            if (_globalTabButton != null) _globalTabButton.onClick.RemoveAllListeners();
            if (_stageTabButton != null) _stageTabButton.onClick.RemoveAllListeners();

            if (_prevStageButton != null) _prevStageButton.onClick.RemoveListener(PrevStage);
            if (_nextStageButton != null) _nextStageButton.onClick.RemoveListener(NextStage);

            if (_easyDiffButton != null) _easyDiffButton.onClick.RemoveAllListeners();
            if (_normalDiffButton != null) _normalDiffButton.onClick.RemoveAllListeners();
            if (_hardDiffButton != null) _hardDiffButton.onClick.RemoveAllListeners();

            if (_retryButton != null) _retryButton.onClick.RemoveListener(FetchCurrentData);
        }
    }
}
