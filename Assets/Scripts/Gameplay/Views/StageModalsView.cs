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
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        [Header("Pause Controls")]
        [SerializeField] private Button btnResume;
        [SerializeField] private Button btnPauseRestart;
        [SerializeField] private Button btnPauseExit;

        [Header("Victory Controls & Display")]
        [SerializeField] private TextMeshProUGUI txtVictoryTitle;
        [SerializeField] private Image[] starIcons;
        [SerializeField] private Color starActiveColor = Color.white;
        [SerializeField] private Color starInactiveColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private TextMeshProUGUI txtVictoryTime;
        [SerializeField] private TextMeshProUGUI txtVictorySteps;
        [SerializeField] private Button btnNextStage;
        [SerializeField] private TextMeshProUGUI txtNextButtonLabel;
        [SerializeField] private Button btnVictoryReplay;
        [SerializeField] private Button btnVictoryExit;

        [Header("Defeat Controls & Display")]
        [SerializeField] private TextMeshProUGUI txtDefeatReason;
        [SerializeField] private Button btnDefeatRetry;
        [SerializeField] private Button btnDefeatExit;

        public event Action OnResumeRequested;
        public event Action OnRestartRequested;
        public event Action OnNextStageRequested;
        public event Action OnExitMenuRequested;

        private Coroutine _starSequenceCoroutine;

        private void Awake()
        {
            RegisterButtons();
            HideAll();
        }

        private void OnDestroy()
        {
            UnregisterButtons();
            if (_starSequenceCoroutine != null)
                StopCoroutine(_starSequenceCoroutine);
        }

        private void RegisterButtons()
        {
            if (btnResume != null)
                btnResume.onClick.AddListener(() => OnButtonClicked(OnResumeRequested));

            if (btnPauseRestart != null)
                btnPauseRestart.onClick.AddListener(() => OnButtonClicked(OnRestartRequested));

            if (btnPauseExit != null)
                btnPauseExit.onClick.AddListener(() => OnButtonClicked(OnExitMenuRequested));

            if (btnNextStage != null)
                btnNextStage.onClick.AddListener(() => OnButtonClicked(OnNextStageRequested));

            if (btnVictoryReplay != null)
                btnVictoryReplay.onClick.AddListener(() => OnButtonClicked(OnRestartRequested));

            if (btnVictoryExit != null)
                btnVictoryExit.onClick.AddListener(() => OnButtonClicked(OnExitMenuRequested));

            if (btnDefeatRetry != null)
                btnDefeatRetry.onClick.AddListener(() => OnButtonClicked(OnRestartRequested));

            if (btnDefeatExit != null)
                btnDefeatExit.onClick.AddListener(() => OnButtonClicked(OnExitMenuRequested));
        }

        private void UnregisterButtons()
        {
            if (btnResume != null) btnResume.onClick.RemoveAllListeners();
            if (btnPauseRestart != null) btnPauseRestart.onClick.RemoveAllListeners();
            if (btnPauseExit != null) btnPauseExit.onClick.RemoveAllListeners();
            if (btnNextStage != null) btnNextStage.onClick.RemoveAllListeners();
            if (btnVictoryReplay != null) btnVictoryReplay.onClick.RemoveAllListeners();
            if (btnVictoryExit != null) btnVictoryExit.onClick.RemoveAllListeners();
            if (btnDefeatRetry != null) btnDefeatRetry.onClick.RemoveAllListeners();
            if (btnDefeatExit != null) btnDefeatExit.onClick.RemoveAllListeners();
        }

        private void OnButtonClicked(Action callback)
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            callback?.Invoke();
        }

        public void ShowPause()
        {
            HideAll();
            if (overlayDim != null) overlayDim.SetActive(true);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        public void ShowVictory(int stars, int steps, int timeSeconds, StageAsset nextStage, Difficulty currentDiff)
        {
            HideAll();
            if (overlayDim != null) overlayDim.SetActive(true);
            if (victoryPanel != null) victoryPanel.SetActive(true);

            AudioManager.Instance?.PlayUI(SoundID.UI_VictoryModal);

            if (txtVictoryTime != null)
            {
                int mins = timeSeconds / 60;
                int secs = timeSeconds % 60;
                txtVictoryTime.text = $"Thời gian: {mins:00}:{secs:00}";
            }

            if (txtVictorySteps != null)
                txtVictorySteps.text = $"Số bước: {steps}";

            // Kiểm tra trạng thái phá đảo (nextStage == null)
            if (nextStage != null)
            {
                if (btnNextStage != null) btnNextStage.gameObject.SetActive(true);
                if (txtNextButtonLabel != null)
                {
                    txtNextButtonLabel.text = nextStage.Difficulty != currentDiff
                        ? $"Qua {nextStage.Difficulty} 1"
                        : "Màn tiếp theo";
                }

                if (txtVictoryTitle != null) txtVictoryTitle.text = "CHIẾN THẮNG!";
            }
            else
            {
                if (btnNextStage != null) btnNextStage.gameObject.SetActive(false);
                if (txtVictoryTitle != null) txtVictoryTitle.text = "XUẤT SẮC!\nBẠN ĐÃ PHÁ ĐẢO GAME!";
            }

            if (_starSequenceCoroutine != null)
                StopCoroutine(_starSequenceCoroutine);

            _starSequenceCoroutine = StartCoroutine(PlayStarSequenceCoroutine(stars));
        }

        public void ShowDefeat(string reason)
        {
            HideAll();
            if (overlayDim != null) overlayDim.SetActive(true);
            if (defeatPanel != null) defeatPanel.SetActive(true);

            AudioManager.Instance?.PlayUI(SoundID.UI_DefeatModal);

            if (txtDefeatReason != null)
                txtDefeatReason.text = string.IsNullOrEmpty(reason) ? "Bạn đã thất bại!" : reason;
        }

        public void HideAll()
        {
            if (_starSequenceCoroutine != null)
            {
                StopCoroutine(_starSequenceCoroutine);
                _starSequenceCoroutine = null;
            }

            if (overlayDim != null) overlayDim.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);
            if (defeatPanel != null) defeatPanel.SetActive(false);
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
