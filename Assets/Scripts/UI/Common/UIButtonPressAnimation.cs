using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Component tái sử dụng gắn vào bất kỳ Button nào để tạo hiệu ứng nảy lún 3D cơ học (Tactile Press Feedback).
    /// Hoạt động độc lập, không phụ thuộc vào Time.timeScale (chạy mượt cả khi Pause).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonPressAnimation : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Animation Settings")]
        [Tooltip("Tỷ lệ scale khi ấn giữ")]
        [SerializeField] private float pressScale = 0.95f;

        [Tooltip("Độ dời trục Y (lún xuống) khi ấn giữ (pixel)")]
        [SerializeField] private float pressOffsetY = -3f;

        [Tooltip("Thời gian co lún (giây)")]
        [SerializeField] private float transitionDuration = 0.06f;

        [Tooltip("Có áp dụng độ lún trục Y không")]
        [SerializeField] private bool usePositionOffset = true;

        [Tooltip("Có áp dụng co giãn scale không")]
        [SerializeField] private bool useScaleEffect = true;

        private Button _button;
        private RectTransform _rectTransform;
        private Vector3 _originalScale = Vector3.one;
        private Vector2 _originalAnchoredPosition = Vector2.zero;
        private Coroutine _animCoroutine;
        private bool _isPressed;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _rectTransform = GetComponent<RectTransform>();

            if (_rectTransform != null)
            {
                _originalScale = _rectTransform.localScale;
                _originalAnchoredPosition = _rectTransform.anchoredPosition;
            }
        }

        private void OnEnable()
        {
            ResetVisualInstant();
        }

        private void OnDisable()
        {
            if (_animCoroutine != null)
            {
                StopCoroutine(_animCoroutine);
                _animCoroutine = null;
            }
            ResetVisualInstant();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_button == null || !_button.interactable) return;

            _isPressed = true;
            AnimateTo(pressScale, usePositionOffset ? _originalAnchoredPosition + new Vector2(0f, pressOffsetY) : _originalAnchoredPosition);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_isPressed) return;

            _isPressed = false;
            AnimateTo(1f, _originalAnchoredPosition);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!_isPressed) return;

            _isPressed = false;
            AnimateTo(1f, _originalAnchoredPosition);
        }

        private void AnimateTo(float targetScaleFactor, Vector2 targetAnchoredPos)
        {
            if (_rectTransform == null) return;

            if (_animCoroutine != null)
                StopCoroutine(_animCoroutine);

            _animCoroutine = StartCoroutine(TransitionCoroutine(targetScaleFactor, targetAnchoredPos));
        }

        private IEnumerator TransitionCoroutine(float targetScaleFactor, Vector2 targetAnchoredPos)
        {
            Vector3 startScale = _rectTransform.localScale;
            Vector2 startPos = _rectTransform.anchoredPosition;
            Vector3 targetScale = useScaleEffect ? _originalScale * targetScaleFactor : _originalScale;

            float elapsed = 0f;
            while (elapsed < transitionDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);

                // Đường cong mượt mà (SmoothStep)
                float smoothT = t * t * (3f - 2f * t);

                if (useScaleEffect)
                    _rectTransform.localScale = Vector3.Lerp(startScale, targetScale, smoothT);

                if (usePositionOffset)
                    _rectTransform.anchoredPosition = Vector2.Lerp(startPos, targetAnchoredPos, smoothT);

                yield return null;
            }

            if (useScaleEffect)
                _rectTransform.localScale = targetScale;

            if (usePositionOffset)
                _rectTransform.anchoredPosition = targetAnchoredPos;

            _animCoroutine = null;
        }

        private void ResetVisualInstant()
        {
            _isPressed = false;
            if (_rectTransform != null)
            {
                _rectTransform.localScale = _originalScale;
                _rectTransform.anchoredPosition = _originalAnchoredPosition;
            }
        }
    }
}
