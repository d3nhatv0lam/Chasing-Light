using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight
{
    public sealed class StageModalsView : MonoBehaviour
    {
        [Header("Root & Overlays")]
        [SerializeField] private GameObject overlayDim;
        [SerializeField] private UIFireBurnTransition fireTransition;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        [Header("Reusable Modal Cards")]
        [SerializeField] private UIModalCard pauseCard;
        [SerializeField] private UIModalCard victoryCard;
        [SerializeField] private UIModalCard defeatCard;

        [Header("Pause Controls")]
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnPauseRestart;
        [SerializeField] private Button btnPauseHome;

        [Header("Victory Controls & Display")]
        [SerializeField] private UIStarRating starRating;
        [SerializeField] private Image[] starIcons;
        [SerializeField] private Color starActiveColor = Color.white;
        [SerializeField] private Color starInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private TextMeshProUGUI txtVictoryTime;
        [SerializeField] private TextMeshProUGUI txtVictorySteps;
        [SerializeField] private Button btnNextStage;
        [SerializeField] private Button btnVictoryHome;
        [SerializeField] private Button btnVictoryReplay;

        [Header("Defeat Controls & Display")]
        [SerializeField] private TextMeshProUGUI txtDefeatReason;
        [SerializeField] private Button btnDefeatRetry;
        [SerializeField] private Button btnDefeatHome;

        public event Action OnResumeRequested;
        public event Action OnRestartRequested;
        public event Action OnNextStageRequested;
        public event Action OnHomeRequested;

        private Coroutine _starSequenceCoroutine;
        private Coroutine _pulseCoroutine;

        private void Awake()
        {
            RegisterButtons();
            HideAll();
        }

        private void OnDestroy()
        {
            StopAllModalCoroutines();
        }

        private void RegisterButtons()
        {
            // Pause
            btnResume.onClick.AddListener(() => OnButtonClicked(OnResumeRequested));
            btnPauseRestart.onClick.AddListener(() => OnButtonClicked(OnRestartRequested));
            btnPauseHome.onClick.AddListener(() => OnButtonClicked(OnHomeRequested));

            // Victory
            btnNextStage.onClick.AddListener(() => OnButtonClicked(OnNextStageRequested));
            btnVictoryHome.onClick.AddListener(() => OnButtonClicked(OnHomeRequested));
            btnVictoryReplay.onClick.AddListener(() => OnButtonClicked(OnRestartRequested));

            // Defeat
            btnDefeatRetry.onClick.AddListener(() => OnButtonClicked(OnRestartRequested));
            btnDefeatHome.onClick.AddListener(() => OnButtonClicked(OnHomeRequested));
        }

        private void OnButtonClicked(Action callback)
        {
            // UIButtonAudio on the button handles sound, fallback here if not present
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            callback?.Invoke();
        }

        public void ShowPause()
        {
            HideAll();
            overlayDim.SetActive(true);

            if (pauseCard != null)
                pauseCard.PopIn();
            else if (pausePanel != null)
                pausePanel.SetActive(true);
        }

        public void ShowVictory(int stars, int steps, int timeSeconds, StageAsset nextStage, Difficulty currentDiff)
        {
            HideAll();

            int mins = timeSeconds / 60;
            int secs = timeSeconds % 60;
            txtVictoryTime.text = $"{mins:00}:{secs:00}";
            txtVictorySteps.text = $"{steps}";

            // Kiểm tra trạng thái phá đảo (nextStage == null)
            if (nextStage != null)
            {
                btnNextStage.gameObject.SetActive(true);
                StartBreathingPulse(btnNextStage.GetComponent<RectTransform>());
            }
            else
            {
                btnNextStage.gameObject.SetActive(false);
            }

            void OnVictoryPresentation()
            {
                overlayDim.SetActive(true);

                if (victoryCard != null)
                    victoryCard.PopIn();
                else if (victoryPanel != null)
                    victoryPanel.SetActive(true);

                AudioManager.Instance?.PlayUI(SoundID.UI_VictoryModal);

                // Kích hoạt animation sao
                if (starRating != null)
                    starRating.AnimateStars(stars);
                else
                {
                    if (_starSequenceCoroutine != null)
                        StopCoroutine(_starSequenceCoroutine);
                    _starSequenceCoroutine = StartCoroutine(PlayStarSequenceCoroutine(stars));
                }
            }

            // Nếu có hiệu ứng lửa cháy lan tỏa, chờ hiệu ứng kết thúc mới hiện popup
            if (fireTransition != null)
                fireTransition.PlayFireWipe(OnVictoryPresentation);
            else
                OnVictoryPresentation();
        }

        public void ShowDefeat(string reason)
        {
            HideAll();
            overlayDim.SetActive(true);

            if (defeatCard != null)
                defeatCard.PopIn();
            else if (defeatPanel != null)
                defeatPanel.SetActive(true);

            AudioManager.Instance?.PlayUI(SoundID.UI_DefeatModal);
            txtDefeatReason.text = string.IsNullOrEmpty(reason) ? "Stage Failed!" : reason;

            if (btnDefeatRetry != null)
                StartBreathingPulse(btnDefeatRetry.GetComponent<RectTransform>());
        }

        public void HideAll()
        {
            StopAllModalCoroutines();

            overlayDim.SetActive(false);
            if (fireTransition != null) fireTransition.ResetTransition();

            if (pauseCard != null) pauseCard.PopOut();
            else if (pausePanel != null) pausePanel.SetActive(false);

            if (victoryCard != null) victoryCard.PopOut();
            else if (victoryPanel != null) victoryPanel.SetActive(false);

            if (defeatCard != null) defeatCard.PopOut();
            else if (defeatPanel != null) defeatPanel.SetActive(false);
        }

        private void StopAllModalCoroutines()
        {
            if (_starSequenceCoroutine != null)
            {
                StopCoroutine(_starSequenceCoroutine);
                _starSequenceCoroutine = null;
            }

            if (_pulseCoroutine != null)
            {
                StopCoroutine(_pulseCoroutine);
                _pulseCoroutine = null;
            }

            if (starRating != null)
                starRating.StopAnimation();
        }

        private void StartBreathingPulse(RectTransform target)
        {
            if (target == null) return;
            if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = StartCoroutine(BreathingPulseCoroutine(target));
        }

        private IEnumerator BreathingPulseCoroutine(RectTransform target)
        {
            Vector3 baseScale = Vector3.one;
            const float pulsePeriod = 1.8f;
            float elapsed = 0f;

            while (target != null && target.gameObject.activeInHierarchy)
            {
                elapsed += Time.unscaledDeltaTime;
                float wave = Mathf.Sin((elapsed / pulsePeriod) * Mathf.PI * 2f);
                float factor = 1f + wave * 0.035f; // Scale 1.0 -> 1.035 -> 0.965 -> 1.0

                target.localScale = baseScale * factor;
                yield return null;
            }

            if (target != null)
                target.localScale = baseScale;
        }

        private IEnumerator PlayStarSequenceCoroutine(int earnedStars)
        {
            if (starIcons == null || starIcons.Length == 0) yield break;

            for (int i = 0; i < starIcons.Length; i++)
            {
                if (starIcons[i] != null)
                    starIcons[i].color = starInactiveColor;
            }

            earnedStars = Mathf.Clamp(earnedStars, 0, starIcons.Length);

            yield return new WaitForSecondsRealtime(0.2f);

            for (int i = 0; i < earnedStars; i++)
            {
                if (starIcons[i] != null)
                {
                    starIcons[i].color = starActiveColor;
                    AudioManager.Instance?.PlayUI(SoundID.UI_StarCount);
                }
                yield return new WaitForSecondsRealtime(0.28f);
            }
        }
    }
}
