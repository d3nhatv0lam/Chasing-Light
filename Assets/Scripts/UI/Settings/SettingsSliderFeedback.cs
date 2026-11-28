using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Hiệu ứng phản hồi xúc giác (tactile feedback) cho thanh trượt âm lượng trong Settings:
    /// phóng nhẹ nút kéo kim cương, làm sáng viền/nền hàng khi chạm/kéo, và phát âm thanh preview khi thả.
    /// </summary>
    public class SettingsSliderFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _handleRect;
        [SerializeField] private Image _rowBackground;
        [SerializeField] private bool _playPreviewOnRelease;

        private static readonly Color NormalRowColor = new Color(0.08f, 0.11f, 0.17f, 0.85f);
        private static readonly Color ActiveRowColor = new Color(0.13f, 0.20f, 0.32f, 0.95f);

        private Coroutine _animCoroutine;
        private bool _isHovered;
        private bool _isDragging;

        public void Configure(RectTransform handleRect, Image rowBackground, bool playPreviewOnRelease)
        {
            _handleRect = handleRect;
            _rowBackground = rowBackground;
            _playPreviewOnRelease = playPreviewOnRelease;
        }

        private void OnDisable()
        {
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
            }
            if (_handleRect != null) _handleRect.localScale = Vector3.one;
            if (_rowBackground != null) _rowBackground.color = NormalRowColor;
            _isHovered = false;
            _isDragging = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _isHovered = true;
            RefreshTarget();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _isHovered = false;
            RefreshTarget();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            RefreshTarget();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            RefreshTarget();

            if (_playPreviewOnRelease)
            {
                AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            }
        }

        private void RefreshTarget()
        {
            float targetScale = _isDragging ? 1.25f : (_isHovered ? 1.12f : 1f);
            Color targetColor = (_isDragging || _isHovered) ? ActiveRowColor : NormalRowColor;

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateFeedback(targetScale, targetColor));
        }

        private IEnumerator AnimateFeedback(float targetScale, Color targetColor)
        {
            Vector3 startScale = _handleRect != null ? _handleRect.localScale : Vector3.one;
            Vector3 endScale = Vector3.one * targetScale;
            Color startColor = _rowBackground != null ? _rowBackground.color : NormalRowColor;

            const float duration = 0.1f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

                if (_handleRect != null)
                    _handleRect.localScale = Vector3.Lerp(startScale, endScale, t);
                if (_rowBackground != null)
                    _rowBackground.color = Color.Lerp(startColor, targetColor, t);

                yield return null;
            }

            if (_handleRect != null) _handleRect.localScale = endScale;
            if (_rowBackground != null) _rowBackground.color = targetColor;
            _animCoroutine = null;
        }
    }
}
