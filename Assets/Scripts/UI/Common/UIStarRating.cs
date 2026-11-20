using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Component tái sử dụng quản lý hiển thị và animation nảy 3 sao (hoặc N sao).
    /// Dùng cho cả Victory Modal và thẻ màn chơi StageButton trên danh sách màn.
    /// </summary>
    public class UIStarRating : MonoBehaviour
    {
        [Header("Star Icons (Thứ tự từ trái qua phải)")]
        [SerializeField] private Image[] starIcons = new Image[3];

        [Header("Colors")]
        [SerializeField] private Color starActiveColor = Color.white;
        [SerializeField] private Color starInactiveColor = new Color(0.25f, 0.25f, 0.3f, 0.5f);

        [Header("Animation Settings")]
        [SerializeField] private float starPopDuration = 0.22f;
        [SerializeField] private float starDelayBetween = 0.15f;
        [SerializeField] private float starOvershootScale = 1.35f;

        private Coroutine _starCoroutine;

        /// <summary>
        /// Hiển thị số sao tức thì (không animation), phù hợp cho UI tĩnh như danh sách màn chơi.
        /// </summary>
        public void SetStarsInstant(int starsEarned)
        {
            StopAnimation();

            if (starIcons == null) return;

            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] == null) continue;

                starIcons[i].transform.localScale = Vector3.one;
                starIcons[i].color = (i < starsEarned) ? starActiveColor : starInactiveColor;
            }
        }

        /// <summary>
        /// Chạy animation nảy từng sao lần lượt kèm âm thanh UI_StarCount.
        /// </summary>
        public void AnimateStars(int starsEarned, Action onComplete = null)
        {
            StopAnimation();
            _starCoroutine = StartCoroutine(AnimateStarsCoroutine(starsEarned, onComplete));
        }

        private IEnumerator AnimateStarsCoroutine(int starsEarned, Action onComplete)
        {
            if (starIcons == null || starIcons.Length == 0)
            {
                onComplete?.Invoke();
                yield break;
            }

            // Khởi tạo trạng thái ban đầu: Tất cả sao chưa đạt là màu inactive, sao đạt ẩn scale về 0
            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] == null) continue;

                if (i < starsEarned)
                {
                    starIcons[i].color = starActiveColor;
                    starIcons[i].transform.localScale = Vector3.zero;
                }
                else
                {
                    starIcons[i].color = starInactiveColor;
                    starIcons[i].transform.localScale = Vector3.one;
                }
            }

            // Nảy từng sao đạt được theo nhịp lò xo
            for (int i = 0; i < starsEarned && i < starIcons.Length; i++)
            {
                var star = starIcons[i];
                if (star == null) continue;

                AudioManager.Instance?.PlayUI(SoundID.UI_StarCount);

                float elapsed = 0f;
                while (elapsed < starPopDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / starPopDuration);

                    // Đường cong lò xo bật nảy (Overshoot bounce)
                    float currentScale = (t < 0.65f)
                        ? Mathf.Lerp(0f, starOvershootScale, t / 0.65f)
                        : Mathf.Lerp(starOvershootScale, 1f, (t - 0.65f) / 0.35f);

                    star.transform.localScale = Vector3.one * currentScale;
                    yield return null;
                }

                star.transform.localScale = Vector3.one;

                if (starDelayBetween > 0f)
                    yield return new WaitForSecondsRealtime(starDelayBetween);
            }

            _starCoroutine = null;
            onComplete?.Invoke();
        }

        public void StopAnimation()
        {
            if (_starCoroutine != null)
            {
                StopCoroutine(_starCoroutine);
                _starCoroutine = null;
            }
        }

        private void OnDisable()
        {
            StopAnimation();
        }
    }
}
