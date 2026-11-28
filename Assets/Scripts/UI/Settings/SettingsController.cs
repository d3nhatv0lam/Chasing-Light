using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChasingLight
{
    public class SettingsController : MonoBehaviour
    {
        [Serializable]
        public class VolumeRow
        {
            public string name;
            public TMP_Text labelText;
            public Slider slider;
            public TMP_Text valueText;
            public Button toggleButton;
            public Image toggleImage;
            public CanvasGroup rowCanvasGroup;
            public CanvasGroup sliderCanvasGroup;

            [NonSerialized] public AudioChannel channel;
        }

        [Header("Scene Navigation")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        [SerializeField] private Button _backButton;
        [SerializeField] private UIModalCard _modalCard;

        [Header("Tab Buttons & Sprites")]
        [SerializeField] private Button _audioTabButton;
        [SerializeField] private Image _audioTabImage;
        [SerializeField] private TMP_Text _audioTabText;
        [SerializeField] private Sprite _audioTabNormalSprite;
        [SerializeField] private Sprite _audioTabSelectedSprite;

        [SerializeField] private Button _userTabButton;
        [SerializeField] private Image _userTabImage;
        [SerializeField] private TMP_Text _userTabText;
        [SerializeField] private Sprite _userTabNormalSprite;
        [SerializeField] private Sprite _userTabSelectedSprite;

        [Header("Tab Colors (Leaderboard Style)")]
        [SerializeField] private Color _activeTabColor = new Color(0.18f, 0.28f, 0.44f, 0.95f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.08f, 0.10f, 0.15f, 0.65f);
        [SerializeField] private Color _activeTabTextColor = new Color(1f, 0.95f, 0.82f, 1f);
        [SerializeField] private Color _inactiveTabTextColor = new Color(0.58f, 0.64f, 0.72f, 1f);

        [Header("Tab Panels")]
        [SerializeField] private GameObject _audioSettingsPanel;
        [SerializeField] private GameObject _userSettingsPanel;

        [Header("Toggle Sprites & Colors")]
        [SerializeField] private Sprite _toggleOnSprite;
        [SerializeField] private Sprite _toggleOffSprite;
        [SerializeField] private Color _toggleOnColor = new Color(0.96f, 0.82f, 0.26f, 1f);
        [SerializeField] private Color _toggleOffColor = new Color(0.45f, 0.52f, 0.62f, 1f);

        [Header("Volume Rows")]
        [SerializeField] private VolumeRow _masterRow;
        [SerializeField] private VolumeRow _bgmRow;
        [SerializeField] private VolumeRow _uiRow;
        [SerializeField] private VolumeRow _sfxRow;

        [Header("Dimming Settings")]
        [SerializeField] private float _dimmedAlpha = 0.4f;
        [SerializeField] private float _activeAlpha = 1.0f;

        private VolumeRow[] _allRows;
        private VolumeRow[] _subRows;
        private Coroutine _tabTransitionCoroutine;

        private void Awake()
        {
            _masterRow.channel = AudioChannel.Master;
            _bgmRow.channel = AudioChannel.BGM;
            _uiRow.channel = AudioChannel.UI;
            _sfxRow.channel = AudioChannel.SFX;

            _allRows = new[] { _masterRow, _bgmRow, _uiRow, _sfxRow };
            _subRows = new[] { _bgmRow, _uiRow, _sfxRow };
        }

        private void Start()
        {
            AudioManager.Instance.PlayBGM(SoundID.BGM_Main);

            _backButton.onClick.AddListener(OnBackClicked);
            _audioTabButton.onClick.AddListener(OnAudioTabClicked);
            _userTabButton.onClick.AddListener(OnUserTabClicked);

            SetTabVisuals(isAudioActive: true);
            InitializeVolumeRows();
            RefreshAllDimmingStates();

            if (_modalCard != null)
            {
                _modalCard.PopIn();
            }
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                OnBackClicked();
            }
        }

        private void OnDisable()
        {
            AudioManager.Instance?.SavePreferences();
        }

        private void InitializeVolumeRows()
        {
            foreach (var row in _allRows)
            {
                float vol = AudioManager.Instance.GetVolume(row.channel);
                // SetValueWithoutNotify tranh kich hoat onValueChanged de khong de trang thai mute khi mo scene
                row.slider.SetValueWithoutNotify(vol);
                UpdateValueText(row, vol);
                UpdateToggleVisual(row, AudioManager.Instance.IsMuted(row.channel));

                row.slider.onValueChanged.AddListener(val => OnSliderMoved(row, val));
                row.toggleButton.onClick.AddListener(() => OnToggleClicked(row));
            }
        }

        private void OnSliderMoved(VolumeRow row, float value)
        {
            AudioManager.Instance.SetVolume(row.channel, value);
            UpdateValueText(row, value);

            if (value > 0.0001f && AudioManager.Instance.IsMuted(row.channel))
            {
                AudioManager.Instance.SetMute(row.channel, false);
                UpdateToggleVisual(row, false);
                RefreshAllDimmingStates();
            }
        }

        private static void UpdateValueText(VolumeRow row, float value)
        {
            if (row.valueText != null)
            {
                row.valueText.text = $"{Mathf.RoundToInt(Mathf.Clamp01(value) * 100f)}%";
            }
        }

        private void OnToggleClicked(VolumeRow row)
        {
            AudioManager.Instance.PlayUI(SoundID.UI_Click);

            bool newMuted = !AudioManager.Instance.IsMuted(row.channel);
            AudioManager.Instance.SetMute(row.channel, newMuted);
            UpdateToggleVisual(row, newMuted);
            RefreshAllDimmingStates();
        }

        private void UpdateToggleVisual(VolumeRow row, bool isMuted)
        {
            if (row.toggleImage == null) return;

            row.toggleImage.sprite = isMuted ? _toggleOffSprite : _toggleOnSprite;
            row.toggleImage.color = isMuted ? _toggleOffColor : _toggleOnColor;
        }

        private void RefreshAllDimmingStates()
        {
            bool masterMuted = AudioManager.Instance.IsMuted(AudioChannel.Master);
            SetGroupVisual(_masterRow.sliderCanvasGroup, !masterMuted);

            foreach (var row in _subRows)
            {
                SetGroupVisual(row.rowCanvasGroup, !masterMuted);
                SetGroupVisual(row.sliderCanvasGroup, !masterMuted && !AudioManager.Instance.IsMuted(row.channel));
            }
        }

        private void SetGroupVisual(CanvasGroup group, bool active)
        {
            group.alpha = active ? _activeAlpha : _dimmedAlpha;
            group.interactable = active;
            group.blocksRaycasts = active;
        }

        private void OnAudioTabClicked()
        {
            AudioManager.Instance.PlayUI(SoundID.UI_Click);
            SetTabVisuals(isAudioActive: true);
        }

        private void OnUserTabClicked()
        {
            AudioManager.Instance.PlayUI(SoundID.UI_Click);
            SetTabVisuals(isAudioActive: false);
        }

        private void SetTabVisuals(bool isAudioActive)
        {
            _audioSettingsPanel.SetActive(isAudioActive);
            _userSettingsPanel.SetActive(!isAudioActive);

            if (_audioTabImage != null)
            {
                var targetSprite = isAudioActive ? _audioTabSelectedSprite : _audioTabNormalSprite;
                if (targetSprite != null) _audioTabImage.sprite = targetSprite;
            }

            if (_userTabImage != null)
            {
                var targetSprite = isAudioActive ? _userTabNormalSprite : _userTabSelectedSprite;
                if (targetSprite != null) _userTabImage.sprite = targetSprite;
            }

            Color targetAudioBg = isAudioActive ? _activeTabColor : _inactiveTabColor;
            Color targetUserBg = isAudioActive ? _inactiveTabColor : _activeTabColor;
            Color targetAudioTxt = isAudioActive ? _activeTabTextColor : _inactiveTabTextColor;
            Color targetUserTxt = isAudioActive ? _inactiveTabTextColor : _activeTabTextColor;

            if (!gameObject.activeInHierarchy)
            {
                ApplyTabColors(targetAudioBg, targetUserBg, targetAudioTxt, targetUserTxt);
                return;
            }

            if (_tabTransitionCoroutine != null) StopCoroutine(_tabTransitionCoroutine);
            _tabTransitionCoroutine = StartCoroutine(AnimateTabColors(targetAudioBg, targetUserBg, targetAudioTxt, targetUserTxt));
        }

        private System.Collections.IEnumerator AnimateTabColors(Color audioBg, Color userBg, Color audioTxt, Color userTxt)
        {
            Color startAudioBg = _audioTabImage != null ? _audioTabImage.color : audioBg;
            Color startUserBg = _userTabImage != null ? _userTabImage.color : userBg;
            Color startAudioTxt = _audioTabText != null ? _audioTabText.color : audioTxt;
            Color startUserTxt = _userTabText != null ? _userTabText.color : userTxt;

            const float duration = 0.14f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                ApplyTabColors(
                    Color.Lerp(startAudioBg, audioBg, t),
                    Color.Lerp(startUserBg, userBg, t),
                    Color.Lerp(startAudioTxt, audioTxt, t),
                    Color.Lerp(startUserTxt, userTxt, t)
                );
                yield return null;
            }

            ApplyTabColors(audioBg, userBg, audioTxt, userTxt);
            _tabTransitionCoroutine = null;
        }

        private void ApplyTabColors(Color audioBg, Color userBg, Color audioTxt, Color userTxt)
        {
            if (_audioTabImage != null) _audioTabImage.color = audioBg;
            if (_userTabImage != null) _userTabImage.color = userBg;
            if (_audioTabText != null) _audioTabText.color = audioTxt;
            if (_userTabText != null) _userTabText.color = userTxt;
        }

        private void OnBackClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }
}
