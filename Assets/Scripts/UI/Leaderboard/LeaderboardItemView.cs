using System;
using System.Collections;
using ChasingLight.Network.DTOs;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight.UI.Leaderboard
{
    /// <summary>
    /// Component quản lý hiển thị một dòng (row) trong danh sách bảng xếp hạng:
    /// - Hạng (Rank) với màu sắc đặc trưng cho Top 1, 2, 3
    /// - Avatar người chơi (tải async qua AvatarImageLoader)
    /// - Tên người chơi
    /// - Thành tích (Tổng sao với Global, hoặc Thời gian + Bước đi + Sao với Stage)
    /// - Highlight nếu dòng đó là của chính người chơi hiện tại
    /// </summary>
    public class LeaderboardItemView : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private Image _avatarImage;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _subScoreText; // Dành cho số bước hoặc chi tiết phụ (tùy chọn)

        [Header("Highlight / Background")]
        [SerializeField] private Image _backgroundImage;
        [SerializeField] private GameObject _selfHighlightIndicator;

        [Header("Colors & Styles")]
        [SerializeField] private Color _top1Color = new Color(1f, 0.84f, 0f, 1f);     // Gold
        [SerializeField] private Color _top2Color = new Color(0.75f, 0.75f, 0.75f, 1f); // Silver
        [SerializeField] private Color _top3Color = new Color(0.80f, 0.50f, 0.20f, 1f); // Bronze
        [SerializeField] private Color _defaultRankColor = Color.white;
        [SerializeField] private Color _selfRowBgColor = new Color(0.2f, 0.45f, 0.7f, 0.6f);
        [SerializeField] private Color _normalRowBgColor = new Color(0.12f, 0.14f, 0.18f, 0.5f);

        private Coroutine _avatarCoroutine;

        // ── Global Leaderboard Binding ────────────────────────────────────────

        public void BindGlobal(GlobalLeaderboardEntryDto data, bool isSelf)
        {
            if (data == null) return;

            SetRank(data.rank);
            SetName(data.displayName, data.userId);
            SetAvatar(data.avatarUrl);

            if (_scoreText != null)
                _scoreText.text = $"{data.TotalStarsFormatted()} ⭐";

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
            string starsStr = new string('★', Mathf.Clamp(data.starsEarned, 0, 3));

            if (_scoreText != null)
            {
                _scoreText.text = $"⏱️ {timeStr}   👟 {data.bestStepCount}";
            }

            if (_subScoreText != null)
            {
                _subScoreText.gameObject.SetActive(!string.IsNullOrEmpty(starsStr));
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
                    break;
                case 2:
                    _rankText.color = _top2Color;
                    break;
                case 3:
                    _rankText.color = _top3Color;
                    break;
                default:
                    _rankText.color = _defaultRankColor;
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
