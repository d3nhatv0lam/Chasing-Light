using System;
using System.Collections;
using ChasingLight.Network.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight.UI.Leaderboard
{
    /// <summary>
    /// Component quản lý hiển thị một dòng trong danh sách bảng xếp hạng:
    /// - Hạng với màu sắc đặc trưng cho Top 1 (Gold), Top 2 (Silver), Top 3 (Bronze)
    /// - Avatar người chơi (tải async qua AvatarImageLoader)
    /// - Tên người chơi
    /// - Thành tích (Tổng sao với Global, hoặc Thời gian + Bước đi + Sao với Stage)
    /// - Highlight phát sáng khi dòng là của chính người chơi hiện tại
    /// </summary>
    public class LeaderboardItemView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private Image _avatarImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _subScoreText;

        [Header("Layout Settings")]
        [SerializeField] private Vector2 _scorePosSingle = new Vector2(-20f, 0f);
        [SerializeField] private Vector2 _scorePosDouble = new Vector2(-20f, 8f);
        [SerializeField] private Vector2 _subScorePos = new Vector2(-20f, -11f);

        [Header("Highlight / Background")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private GameObject _selfHighlightIndicator;

        [Header("Colors & Styles")]
        [SerializeField] private Color _top1Color = new Color(1f, 0.84f, 0.15f, 1f);     // Radiant Gold (#FFD726)
        [SerializeField] private Color _top2Color = new Color(0.88f, 0.91f, 0.95f, 1f); // Pristine Silver (#E2E8F0)
        [SerializeField] private Color _top3Color = new Color(0.96f, 0.62f, 0.22f, 1f); // Warm Bronze (#F59E38)
        [SerializeField] private Color _defaultRankColor = new Color(0.58f, 0.64f, 0.72f, 1f); // Muted Slate (#94A3B8)
        [SerializeField] private Color _selfRowBgColor = new Color(0.12f, 0.24f, 0.42f, 0.92f); // Luminous Midnight Sapphire
        [SerializeField] private Color _normalRowBgColor = new Color(0.08f, 0.11f, 0.16f, 0.78f); // Translucent Dark Slate

        private Coroutine _avatarCoroutine;

        // ── Global Leaderboard Binding ────────────────────────────────────────

        public void BindGlobal(GlobalLeaderboardEntryDto data, bool isSelf)
        {
            if (data == null) return;

            SetRank(data.rank);
            SetName(data.displayName, data.userId);
            SetAvatar(data.avatarUrl);

            if (_scoreText != null)
            {
                _scoreText.rectTransform.anchoredPosition = _scorePosSingle;
                _scoreText.text = $"{data.TotalStarsFormatted()} Stars";
            }

            if (_subScoreText != null)
                _subScoreText.gameObject.SetActive(false);

            ApplyHighlight(isSelf);
        }

        // ── Stage Leaderboard Binding ─────────────────────────────────────────

        public void BindStage(StageLeaderboardEntryDto data, bool isSelf)
        {
            if (data == null) return;

            SetRank(data.rank);
            SetName(data.displayName, data.userId);
            SetAvatar(data.avatarUrl);

            string timeStr = FormatTime(data.bestTimeSeconds);
            string starsStr = $"{Mathf.Clamp(data.starsEarned, 0, 3)}/3 Stars";

            if (_scoreText != null)
            {
                _scoreText.rectTransform.anchoredPosition = _scorePosDouble;
                _scoreText.text = $"{timeStr}  |  {data.bestStepCount} Steps";
            }

            if (_subScoreText != null)
            {
                _subScoreText.gameObject.SetActive(true);
                _subScoreText.rectTransform.anchoredPosition = _subScorePos;
                _subScoreText.text = starsStr;
            }

            ApplyHighlight(isSelf);
        }

        // ── UI Helpers ────────────────────────────────────────────────────────

        private void SetRank(int rank)
        {
            if (_rankText == null) return;

            _rankText.text = rank > 0 ? rank.ToString() : "-";

            switch (rank)
            {
                case 1:
                    _rankText.color = _top1Color;
                    _rankText.fontStyle = FontStyles.Bold;
                    break;
                case 2:
                    _rankText.color = _top2Color;
                    _rankText.fontStyle = FontStyles.Bold;
                    break;
                case 3:
                    _rankText.color = _top3Color;
                    _rankText.fontStyle = FontStyles.Bold;
                    break;
                default:
                    _rankText.color = _defaultRankColor;
                    _rankText.fontStyle = FontStyles.Normal;
                    break;
            }
        }

        private void SetName(string displayName, string userId)
        {
            if (_nameText == null) return;

            if (!string.IsNullOrWhiteSpace(displayName))
            {
                _nameText.text = displayName;
            }
            else if (!string.IsNullOrEmpty(userId) && userId.Length >= 6)
            {
                _nameText.text = $"Player_{userId.Substring(0, 6)}";
            }
            else
            {
                _nameText.text = "Player";
            }
        }

        private void SetAvatar(string avatarUrl)
        {
            if (_avatarImage == null) return;

            if (_avatarCoroutine != null)
                StopCoroutine(_avatarCoroutine);

            _avatarImage.sprite = AvatarImageLoader.DefaultPlaceholder;

            if (!string.IsNullOrWhiteSpace(avatarUrl))
            {
                _avatarCoroutine = StartCoroutine(
                    AvatarImageLoader.LoadAvatar(avatarUrl, sprite =>
                    {
                        if (_avatarImage != null && sprite != null)
                            _avatarImage.sprite = sprite;
                    }));
            }
        }

        private void ApplyHighlight(bool isSelf)
        {
            if (_selfHighlightIndicator != null)
                _selfHighlightIndicator.SetActive(isSelf);

            if (_backgroundImage != null)
                _backgroundImage.color = isSelf ? _selfRowBgColor : _normalRowBgColor;
        }

        private static string FormatTime(int seconds)
        {
            if (seconds < 0) seconds = 0;
            int mins = seconds / 60;
            int secs = seconds % 60;
            return $"{mins:00}:{secs:00}";
        }

        private void OnDisable()
        {
            if (_avatarCoroutine != null)
            {
                StopCoroutine(_avatarCoroutine);
                _avatarCoroutine = null;
            }
        }
    }

    public static class LeaderboardDtoExtensions
    {
        public static string TotalStarsFormatted(this GlobalLeaderboardEntryDto dto)
        {
            return dto != null ? dto.totalStars.ToString("N0") : "0";
        }
    }
}
