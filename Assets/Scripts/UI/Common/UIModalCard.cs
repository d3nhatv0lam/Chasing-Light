using System;
using System.Collections;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Component tái sử dụng gắn vào bất kỳ khung Card/Modal nào để tạo hiệu ứng Pop-in nảy đàn hồi (Elastic Entrance).
    /// Hoạt động bằng Time.unscaledDeltaTime nên chuyển động mượt mà ngay cả khi Time.timeScale = 0 lúc Pause.
    /// </summary>
    public class UIModalCard : MonoBehaviour
    {
        [Header("Pop-In Settings")]
        [SerializeField] private float popInDuration = 0.26f;
        [SerializeField] private float startScale = 0.82f;
        [SerializeField] private float overshootScale = 1.06f;

        [Header("Pop-Out Settings")]
        [SerializeField] private float popOutDuration = 0.16f;

        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Coroutine _motionCoroutine;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void PopIn(Action onComplete = null)
        {
            if (_motionCoroutine != null)
                StopCoroutine(_motionCoroutine);

            gameObject.SetActive(true);
            _motionCoroutine = StartCoroutine(PopInCoroutine(onComplete));
        }

        public void PopOut(Action onComplete = null)
        {
            if (_motionCoroutine != null)
            {
                StopCoroutine(_motionCoroutine);
                _motionCoroutine = null;
            }

            if (!gameObject.activeInHierarchy)
            {
                if (_rectTransform != null) _rectTransform.localScale = Vector3.one;
                gameObject.SetActive(false);
                onComplete?.Invoke();
                return;
            }

            _motionCoroutine = StartCoroutine(PopOutCoroutine(onComplete));
        }

        private IEnumerator PopInCoroutine(Action onComplete)
        {
            _rectTransform.localScale = Vector3.one * startScale;
            bool hasCanvas = _canvasGroup != null;
            if (hasCanvas) _canvasGroup.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < popInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popInDuration);

                // Đường cong nảy lò xo (Overshoot bounce curve)
                float currentScale = (t < 0.65f)
                    ? Mathf.Lerp(startScale, overshootScale, t / 0.65f)
                    : Mathf.Lerp(overshootScale, 1f, (t - 0.65f) / 0.35f);

                _rectTransform.localScale = Vector3.one * currentScale;

                if (hasCanvas)
                    _canvasGroup.alpha = Mathf.Clamp01(t / 0.5f);

                yield return null;
            }

            _rectTransform.localScale = Vector3.one;
            if (hasCanvas) _canvasGroup.alpha = 1f;

            _motionCoroutine = null;
            onComplete?.Invoke();
        }

        private IEnumerator PopOutCoroutine(Action onComplete)
        {
            Vector3 initialScale = _rectTransform.localScale;
            bool hasCanvas = _canvasGroup != null;
            float initialAlpha = hasCanvas ? _canvasGroup.alpha : 1f;

            float elapsed = 0f;
            while (elapsed < popOutDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / popOutDuration);

                _rectTransform.localScale = Vector3.Lerp(initialScale, Vector3.one * 0.7f, t);

                if (hasCanvas)
                    _canvasGroup.alpha = Mathf.Lerp(initialAlpha, 0f, t);

                yield return null;
            }

            _rectTransform.localScale = Vector3.one;
            gameObject.SetActive(false);

            _motionCoroutine = null;
            onComplete?.Invoke();
        }

        private void OnDisable()
        {
            if (_motionCoroutine != null)
            {
                StopCoroutine(_motionCoroutine);
                _motionCoroutine = null;
            }
        }
    }
}
