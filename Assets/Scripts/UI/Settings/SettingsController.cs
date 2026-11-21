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
            public Button toggleButton;
            public Image toggleImage;
            public CanvasGroup rowCanvasGroup;
            public CanvasGroup sliderCanvasGroup;

            [NonSerialized] public AudioChannel channel;
        }

        [Header("Scene Navigation")]
        [SerializeField] private string _mainMenuSceneName = "MainMenu";
        [SerializeField] private Button _backButton;

        [Header("Tab Buttons & Sprites")]
        [SerializeField] private Button _audioTabButton;
        [SerializeField] private Image _audioTabImage;
        [SerializeField] private Sprite _audioTabNormalSprite;
        [SerializeField] private Sprite _audioTabSelectedSprite;

        [SerializeField] private Button _userTabButton;
        [SerializeField] private Image _userTabImage;
        [SerializeField] private Sprite _userTabNormalSprite;
        [SerializeField] private Sprite _userTabSelectedSprite;

        [Header("Tab Panels")]
        [SerializeField] private GameObject _audioSettingsPanel;
        [SerializeField] private GameObject _userSettingsPanel;

        [Header("Toggle Sprites")]
        [SerializeField] private Sprite _toggleOnSprite;
        [SerializeField] private Sprite _toggleOffSprite;

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
                // SetValueWithoutNotify tranh kich hoat onValueChanged de khong de trang thai mute khi mo scene
                row.slider.SetValueWithoutNotify(AudioManager.Instance.GetVolume(row.channel));
                UpdateToggleVisual(row, AudioManager.Instance.IsMuted(row.channel));

                row.slider.onValueChanged.AddListener(val => OnSliderMoved(row, val));
                row.toggleButton.onClick.AddListener(() => OnToggleClicked(row));
            }
        }

        private void OnSliderMoved(VolumeRow row, float value)
        {
            AudioManager.Instance.SetVolume(row.channel, value);

            if (value > 0.0001f && AudioManager.Instance.IsMuted(row.channel))
            {
                AudioManager.Instance.SetMute(row.channel, false);
                UpdateToggleVisual(row, false);
                RefreshAllDimmingStates();
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
            row.toggleImage.sprite = isMuted ? _toggleOffSprite : _toggleOnSprite;
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
            _audioTabImage.sprite = isAudioActive ? _audioTabSelectedSprite : _audioTabNormalSprite;
            _userTabImage.sprite = isAudioActive ? _userTabNormalSprite : _userTabSelectedSprite;
        }

        private void OnBackClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }
}
