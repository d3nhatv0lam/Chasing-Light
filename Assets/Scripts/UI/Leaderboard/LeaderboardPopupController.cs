using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ChasingLight.Network.Clients;
using ChasingLight.Network.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ChasingLight.UI.Leaderboard
{
    public enum LeaderboardTab
    {
        Global = 0,
        Stage = 1
    }

    /// <summary>
    /// Controls the Leaderboard Popup UI:
    /// - Manages 2 Tabs: Global and By Stage
    /// - Stage Selector linked to StageCatalog
    /// - Renders rankings via LeaderboardItemView
    /// - Pinned My Rank footer docked at the bottom
    /// - States: Loading, Empty, Error + Retry button
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
        [SerializeField] private TMP_Text _globalTabText;
        [SerializeField] private TMP_Text _stageTabText;
        [SerializeField] private Color _activeTabColor = new Color(0.18f, 0.28f, 0.44f, 0.95f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.08f, 0.10f, 0.15f, 0.65f);
        [SerializeField] private Color _activeTabTextColor = new Color(1f, 0.95f, 0.82f, 1f); // Warm Gold/White
        [SerializeField] private Color _inactiveTabTextColor = new Color(0.58f, 0.64f, 0.72f, 1f); // Muted Silver

        [Header("Stage Selector (Active on Stage Tab)")]
        [SerializeField] private GameObject _stageSelectorPanel;
        [SerializeField] private Button _easyDiffButton;
        [SerializeField] private Button _normalDiffButton;
        [SerializeField] private Button _hardDiffButton;
        [SerializeField] private Image _easyDiffBg;
        [SerializeField] private Image _normalDiffBg;
        [SerializeField] private Image _hardDiffBg;
        [SerializeField] private TMP_Text _easyDiffText;
        [SerializeField] private TMP_Text _normalDiffText;
        [SerializeField] private TMP_Text _hardDiffText;
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

        // Resolved from _scrollRect in Awake; no Inspector wiring needed.
        private RectTransform _scrollViewRect;

        // ── Runtime State ─────────────────────────────────────────────────────
        private LeaderboardTab _currentTab = LeaderboardTab.Global;
        private Difficulty _currentDifficulty = Difficulty.Easy;
        private List<StageAsset> _currentStages = new();
        private int _currentStageIndex = 0;
        private Coroutine _fetchCoroutine;

        private void Awake()
        {
            ResetRectTransform();

            if (_scrollViewRect == null && _scrollRect != null)
                _scrollViewRect = _scrollRect.GetComponent<RectTransform>();

            if (_config == null)
            {
                _config = Resources.Load<AuthConfig>("AuthConfig");
            }

            if (_stageCatalog == null)
            {
                _stageCatalog = Resources.Load<StageCatalog>("StageCatalog");
            }

            SetModalVisible(false);
        }

        private void ResetRectTransform()
        {
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.anchoredPosition = Vector2.zero;
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
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                if (_rootModal != null && _rootModal.activeSelf)
                {
                    Close();
                }
            }
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Opens the leaderboard popup at Global tab.
        /// </summary>
        public void OpenGlobal()
        {
            Open();
            SwitchTab(LeaderboardTab.Global);
        }

        /// <summary>
        /// Opens the leaderboard popup at Stage tab, optionally targeted at a specific stage.
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
        /// Opens the leaderboard modal.
        /// </summary>
        public void Open()
        {
            ResetRectTransform();
            SetModalVisible(true);
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);

            var modalCard = _rootModal != null ? _rootModal.GetComponent<UIModalCard>() : null;
            if (modalCard != null)
            {
                modalCard.PopIn();
            }
        }

        /// <summary>
        /// Closes the leaderboard modal.
        /// </summary>
        public void Close()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            StopFetch();

            var modalCard = _rootModal != null ? _rootModal.GetComponent<UIModalCard>() : null;
            if (modalCard != null)
            {
                modalCard.PopOut(() => SetModalVisible(false));
            }
            else
            {
                SetModalVisible(false);
            }
        }

        private void SetModalVisible(bool visible)
        {
            if (_rootModal != null)
                _rootModal.SetActive(visible);

            if (_backdropButton != null)
                _backdropButton.gameObject.SetActive(visible);

            if (_rootModal == null && _backdropButton == null)
                gameObject.SetActive(visible);
        }

        // ── Tab Management ────────────────────────────────────────────────────

        public void SwitchTab(LeaderboardTab tab)
        {
            _currentTab = tab;
            UpdateTabVisuals();

            bool isStage = tab == LeaderboardTab.Stage;

            if (_stageSelectorPanel != null)
                _stageSelectorPanel.SetActive(isStage);

            AdjustScrollViewForTab(isStage);

            if (isStage)
            {
                RefreshStageSelector();
            }

            FetchCurrentData();
        }

        // Shifts the ScrollView top edge down when StageSelectorPanel is visible.
        private void AdjustScrollViewForTab(bool stageTabActive)
        {
            if (_scrollViewRect == null) return;
            var v = _scrollViewRect.anchorMax;
            v.y = stageTabActive ? 0.745f : 0.81f;
            _scrollViewRect.anchorMax = v;
        }

        private void UpdateTabVisuals()
        {
            bool isGlobal = _currentTab == LeaderboardTab.Global;

            if (_globalTabBg != null)
                _globalTabBg.color = isGlobal ? _activeTabColor : _inactiveTabColor;

            if (_stageTabBg != null)
                _stageTabBg.color = !isGlobal ? _activeTabColor : _inactiveTabColor;

            if (_globalTabText != null)
            {
                _globalTabText.color = isGlobal ? _activeTabTextColor : _inactiveTabTextColor;
                _globalTabText.fontStyle = isGlobal ? FontStyles.Bold : FontStyles.Normal;
            }

            if (_stageTabText != null)
            {
                _stageTabText.color = !isGlobal ? _activeTabTextColor : _inactiveTabTextColor;
                _stageTabText.fontStyle = !isGlobal ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        // ── Stage Selector Logic ──────────────────────────────────────────────

        public void SelectDifficulty(Difficulty difficulty)
        {
            _currentDifficulty = difficulty;
            _currentStageIndex = 0;
            UpdateDifficultyVisuals();
            RefreshStageSelector();
            FetchCurrentData();
        }

        private void UpdateDifficultyVisuals()
        {
            // Easy: Cyan theme (Light Hero)
            Color easyActive = new Color(0.12f, 0.45f, 0.65f, 0.95f);
            Color easyInactive = new Color(0.08f, 0.12f, 0.18f, 0.6f);

            // Normal: Sapphire blue theme (Crystal)
            Color normalActive = new Color(0.20f, 0.38f, 0.75f, 0.95f);
            Color normalInactive = new Color(0.08f, 0.12f, 0.18f, 0.6f);

            // Hard: Crimson theme (Shadow Hero)
            Color hardActive = new Color(0.65f, 0.18f, 0.28f, 0.95f);
            Color hardInactive = new Color(0.08f, 0.12f, 0.18f, 0.6f);

            if (_easyDiffBg != null)
                _easyDiffBg.color = _currentDifficulty == Difficulty.Easy ? easyActive : easyInactive;
            if (_normalDiffBg != null)
                _normalDiffBg.color = _currentDifficulty == Difficulty.Normal ? normalActive : normalInactive;
            if (_hardDiffBg != null)
                _hardDiffBg.color = _currentDifficulty == Difficulty.Hard ? hardActive : hardInactive;

            if (_easyDiffText != null)
                _easyDiffText.color = _currentDifficulty == Difficulty.Easy ? Color.white : new Color(0.6f, 0.7f, 0.8f, 1f);
            if (_normalDiffText != null)
                _normalDiffText.color = _currentDifficulty == Difficulty.Normal ? Color.white : new Color(0.6f, 0.7f, 0.8f, 1f);
            if (_hardDiffText != null)
                _hardDiffText.color = _currentDifficulty == Difficulty.Hard ? Color.white : new Color(0.6f, 0.7f, 0.8f, 1f);
        }

        private void SetSelectedStage(StageAsset stage)
        {
            if (stage == null) return;

            _currentDifficulty = stage.Difficulty;
            UpdateDifficultyVisuals();
            LoadStagesForDifficulty(_currentDifficulty);

            int foundIdx = _currentStages.FindIndex(s => s != null && s.Id == stage.Id);
            _currentStageIndex = foundIdx >= 0 ? foundIdx : 0;

            UpdateStageTitle();
            FetchCurrentData();
        }

        private void RefreshStageSelector()
        {
            UpdateDifficultyVisuals();
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
                _stageTitleText.text = $"{_currentDifficulty.ToString().ToUpper()} - (NO STAGES)";
                return;
            }

            var stage = _currentStages[_currentStageIndex];
            _stageTitleText.text = $"{_currentDifficulty.ToString().ToUpper()} - STAGE {stage.StageNumber}";
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
                    ShowEmpty(true, "No valid stage selected.");
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
                ShowError(true, result.ErrorMessage ?? "Failed to load leaderboard.");
                return;
            }

            var list = result.Data ?? new List<GlobalLeaderboardEntryDto>();
            if (list.Count == 0)
            {
                ShowEmpty(true, "No records yet on the global leaderboard!");
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
                ShowError(true, result.ErrorMessage ?? "Failed to load leaderboard.");
                return;
            }

            var list = result.Data ?? new List<StageLeaderboardEntryDto>();
            if (list.Count == 0)
            {
                ShowEmpty(true, "No players have completed this stage yet!");
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
                SetGuestFooterNotice("Sign in to compete on the leaderboard!");
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
                    displayName = "You",
                    avatarUrl = null,
                    totalStars = localStars,
                    rank = 0
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
                SetGuestFooterNotice("Sign in to save your stage records!");
                return;
            }

            if (myEntry != null)
            {
                ShowMyRankItem(true);
                _myRankItemView.BindStage(myEntry, true);
            }
            else
            {
                var currentStage = GetCurrentStage();
                var localRecord = currentStage != null && SaveManager.Instance != null
                    ? SaveManager.Instance.GetStageRecord(currentStage.Id)
                    : null;

                if (localRecord != null)
                {
                    var fallbackEntry = new StageLeaderboardEntryDto
                    {
                        userId = userId,
                        displayName = "You",
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
                    SetGuestFooterNotice("You haven't completed this stage yet.");
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
