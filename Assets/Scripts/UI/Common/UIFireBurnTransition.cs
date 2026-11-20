using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Component điều khiển hiệu ứng chuyển cảnh Lửa Bùng Cháy Lan Tỏa (Fire Burn Dissolve Wipe).
    /// Tái sử dụng độc lập cho màn chuyển cảnh chiến thắng hoặc các hiệu ứng chuyển cảnh cinematic.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIFireBurnTransition : MonoBehaviour
    {
        [Header("Target & Material")]
        [SerializeField] private Image targetImage;
        [SerializeField] private Material burnMaterial;

        [Header("Timing & Tuning")]
        [SerializeField] private float transitionDuration = 0.85f;
        [SerializeField] private Vector2 burnCenter = new Vector2(0.5f, 0.5f);

        [Header("Audio")]
        [SerializeField] private SoundID fireSoundId = SoundID.SFX_CharAppearance;
        [SerializeField] private AudioClip customFireClip;
        [SerializeField] private AudioSource localAudioSource;

        private Material _runtimeMaterial;
        private CanvasGroup _canvasGroup;
        private Coroutine _activeCoroutine;
        private static readonly int BurnProgressId = Shader.PropertyToID("_BurnProgress");
        private static readonly int BurnCenterId = Shader.PropertyToID("_BurnCenter");

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            EnsureRuntimeMaterial();
        }

        private void EnsureRuntimeMaterial()
        {
            if (_runtimeMaterial != null) return;

            Material sourceMat = burnMaterial != null ? burnMaterial : (targetImage != null ? targetImage.material : null);
            if (sourceMat != null && sourceMat.shader != null)
            {
                _runtimeMaterial = new Material(sourceMat);
                if (targetImage != null)
                {
                    targetImage.material = _runtimeMaterial;
                }
            }
        }

        /// <summary>
        /// Bắt đầu hiệu ứng lửa cháy lan tỏa từ tâm ra toàn màn hình.
        /// </summary>
        /// <param name="onComplete">Callback kích hoạt khi ngọn lửa đã thiêu đốt và bao phủ hoàn toàn màn hình</param>
        public void PlayFireWipe(Action onComplete = null)
        {
            gameObject.SetActive(true);
            EnsureRuntimeMaterial();

            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
            }

            _activeCoroutine = StartCoroutine(BurnCoroutine(onComplete));
        }

        private IEnumerator BurnCoroutine(Action onComplete)
        {
            // 1. Phát âm thanh tiếng lửa bùng cháy
            PlayFireAudio();

            // 2. Thiết lập trạng thái ban đầu
            if (_runtimeMaterial != null)
            {
                _runtimeMaterial.SetVector(BurnCenterId, new Vector4(burnCenter.x, burnCenter.y, 0, 0));
                _runtimeMaterial.SetFloat(BurnProgressId, 0f);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            // 3. Cháy lan tỏa bằng unscaledDeltaTime (đảm bảo chạy mượt cả khi Time.timeScale = 0)
            float elapsed = 0f;
            float duration = Mathf.Max(0.1f, transitionDuration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Smooth step curve cho ngọn lửa bùng lên tự nhiên
                float curve = t * t * (3f - 2f * t);
                float progress = Mathf.Lerp(0f, 1.15f, curve);

                if (_runtimeMaterial != null)
                {
                    _runtimeMaterial.SetFloat(BurnProgressId, progress);
                }

                yield return null;
            }

            // Đảm bảo kết thúc phủ 100% màn hình
            if (_runtimeMaterial != null)
            {
                _runtimeMaterial.SetFloat(BurnProgressId, 1.2f);
            }

            _activeCoroutine = null;

            // 4. Kích hoạt callback khi ngọn lửa đã cháy lan xong
            onComplete?.Invoke();
        }

        private void PlayFireAudio()
        {
            if (customFireClip != null && localAudioSource != null)
            {
                localAudioSource.PlayOneShot(customFireClip);
            }
            else if (AudioManager.Instance != null && fireSoundId != SoundID.None)
            {
                AudioManager.Instance.PlaySFX(fireSoundId);
            }
        }

        /// <summary>
        /// Reset trạng thái hiệu ứng về ban đầu.
        /// </summary>
        public void ResetTransition()
        {
            if (_activeCoroutine != null)
            {
                StopCoroutine(_activeCoroutine);
                _activeCoroutine = null;
            }

            if (_runtimeMaterial != null)
            {
                _runtimeMaterial.SetFloat(BurnProgressId, 0f);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }

            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_runtimeMaterial != null)
            {
                Destroy(_runtimeMaterial);
                _runtimeMaterial = null;
            }
        }
    }
}
