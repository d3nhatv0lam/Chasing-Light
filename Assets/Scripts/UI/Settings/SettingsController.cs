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

        private float _lastSampleSoundTime;

        private void Start()
        {
            AudioManager.Instance?.PlayBGM(SoundID.BGM_Main);

            InitializeTabs();
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

        private void OnEnable()
        {
            if (_backButton != null) _backButton.onClick.AddListener(OnBackClicked);

            if (_audioTabButton != null) _audioTabButton.onClick.AddListener(OnAudioTabClicked);
            if (_userTabButton != null)  _userTabButton.onClick.AddListener(OnUserTabClicked);

            BindRowEvents(_masterRow,
                onSlider: val =>
                {
                    AudioManager.Instance?.SetMasterVolume(val);
                    OnSliderDragged(_masterRow, AudioManager.Instance.SetMasterMute);
                },
                onToggle: () => ToggleMute(_masterRow, AudioManager.Instance.IsMasterMuted(), AudioManager.Instance.SetMasterMute));

            BindRowEvents(_bgmRow,
                onSlider: val =>
                {
                    AudioManager.Instance?.SetBGMVolume(val);
                    OnSliderDragged(_bgmRow, AudioManager.Instance.SetBGMMute);
                },
                onToggle: () => ToggleMute(_bgmRow, AudioManager.Instance.IsBGMMuted(), AudioManager.Instance.SetBGMMute));

            BindRowEvents(_uiRow,
                onSlider: val =>
                {
                    AudioManager.Instance?.SetUIVolume(val);
                    OnSliderDragged(_uiRow, AudioManager.Instance.SetUIMute);
                    PlaySampleFeedback(SoundID.UI_Click, isUI: true);
                },
                onToggle: () => ToggleMute(_uiRow, AudioManager.Instance.IsUIMuted(), AudioManager.Instance.SetUIMute));

            BindRowEvents(_sfxRow,
                onSlider: val =>
                {
                    AudioManager.Instance?.SetSFXVolume(val);
                    OnSliderDragged(_sfxRow, AudioManager.Instance.SetSFXMute);
                    PlaySampleFeedback(SoundID.SFX_Move, isUI: false);
                },
                onToggle: () => ToggleMute(_sfxRow, AudioManager.Instance.IsSFXMuted(), AudioManager.Instance.SetSFXMute));
        }

        private void OnDisable()
        {
            if (_backButton != null) _backButton.onClick.RemoveListener(OnBackClicked);

            if (_audioTabButton != null) _audioTabButton.onClick.RemoveListener(OnAudioTabClicked);
            if (_userTabButton != null)  _userTabButton.onClick.RemoveListener(OnUserTabClicked);

            UnbindRowEvents(_masterRow);
            UnbindRowEvents(_bgmRow);
            UnbindRowEvents(_uiRow);
            UnbindRowEvents(_sfxRow);
        }

        //  TAB SWITCHING

        private void InitializeTabs()
        {
            SetTabVisuals(isAudioActive: true);
        }

        private void OnAudioTabClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            SetTabVisuals(isAudioActive: true);
        }

        private void OnUserTabClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            SetTabVisuals(isAudioActive: false);
        }

        private void SetTabVisuals(bool isAudioActive)
        {
            if (_audioSettingsPanel != null) _audioSettingsPanel.SetActive(isAudioActive);
            if (_userSettingsPanel != null)  _userSettingsPanel.SetActive(!isAudioActive);

            if (_audioTabImage != null)
                _audioTabImage.sprite = isAudioActive ? _audioTabSelectedSprite : _audioTabNormalSprite;

            if (_userTabImage != null)
                _userTabImage.sprite = isAudioActive ? _userTabNormalSprite : _userTabSelectedSprite;
        }

        //  VOLUME & TOGGLE BINDING

        private void InitializeVolumeRows()
        {
            if (AudioManager.Instance == null) return;

            InitRowValue(_masterRow, AudioManager.Instance.GetMasterVolume(), AudioManager.Instance.IsMasterMuted());
            InitRowValue(_bgmRow,    AudioManager.Instance.GetBGMVolume(),    AudioManager.Instance.IsBGMMuted());
            InitRowValue(_uiRow,     AudioManager.Instance.GetUIVolume(),     AudioManager.Instance.IsUIMuted());
            InitRowValue(_sfxRow,    AudioManager.Instance.GetSFXVolume(),    AudioManager.Instance.IsSFXMuted());
        }

        private void InitRowValue(VolumeRow row, float volume, bool isMuted)
        {
            if (row == null) return;
            if (row.slider != null) row.slider.value = volume;
            UpdateToggleVisual(row, isMuted);
        }

        private void BindRowEvents(VolumeRow row, Action<float> onSlider, Action onToggle)
        {
            if (row == null) return;
            if (row.slider != null) row.slider.onValueChanged.AddListener(val => onSlider?.Invoke(val));
            if (row.toggleButton != null) row.toggleButton.onClick.AddListener(() => onToggle?.Invoke());
        }

        private void UnbindRowEvents(VolumeRow row)
        {
            if (row == null) return;
            if (row.slider != null) row.slider.onValueChanged.RemoveAllListeners();
            if (row.toggleButton != null) row.toggleButton.onClick.RemoveAllListeners();
        }

        private void ToggleMute(VolumeRow row, bool currentlyMuted, Action<bool> setMuteAction)
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);

            bool newMutedState = !currentlyMuted;
            setMuteAction?.Invoke(newMutedState);
            UpdateToggleVisual(row, newMutedState);
            RefreshAllDimmingStates();
        }

        private void OnSliderDragged(VolumeRow row, Action<bool> setMuteAction)
        {
            if (row == null || row.slider == null || AudioManager.Instance == null) return;
            if (row.slider.value > 0.0001f)
            {
                bool isMuted = row == _masterRow ? AudioManager.Instance.IsMasterMuted() :
                               row == _bgmRow    ? AudioManager.Instance.IsBGMMuted() :
                               row == _uiRow     ? AudioManager.Instance.IsUIMuted() :
                               AudioManager.Instance.IsSFXMuted();

                if (isMuted)
                {
                    setMuteAction?.Invoke(false);
                    UpdateToggleVisual(row, false);
                    RefreshAllDimmingStates();
                }
            }
        }

        private void UpdateToggleVisual(VolumeRow row, bool isMuted)
        {
            if (row?.toggleImage == null) return;
            row.toggleImage.sprite = isMuted ? _toggleOffSprite : _toggleOnSprite;
        }

        private void PlaySampleFeedback(SoundID sound, bool isUI)
        {
            if (Time.unscaledTime - _lastSampleSoundTime < 0.15f) return;
            _lastSampleSoundTime = Time.unscaledTime;

            if (isUI)
                AudioManager.Instance?.PlayUI(sound);
            else
                AudioManager.Instance?.PlaySFX(sound);
        }

        //  DIMMING HIERARCHY LOGIC

        private void RefreshAllDimmingStates()
        {
            if (AudioManager.Instance == null) return;

            bool masterMuted = AudioManager.Instance.IsMasterMuted();

            SetGroupVisual(_masterRow.sliderCanvasGroup, !masterMuted);

            if (masterMuted)
            {
                SetGroupVisual(_bgmRow.rowCanvasGroup, false);
                SetGroupVisual(_uiRow.rowCanvasGroup, false);
                SetGroupVisual(_sfxRow.rowCanvasGroup, false);
            }
            else
            {
                SetGroupVisual(_bgmRow.rowCanvasGroup, true);
                SetGroupVisual(_uiRow.rowCanvasGroup, true);
                SetGroupVisual(_sfxRow.rowCanvasGroup, true);

                SetGroupVisual(_bgmRow.sliderCanvasGroup, !AudioManager.Instance.IsBGMMuted());
                SetGroupVisual(_uiRow.sliderCanvasGroup, !AudioManager.Instance.IsUIMuted());
                SetGroupVisual(_sfxRow.sliderCanvasGroup, !AudioManager.Instance.IsSFXMuted());
            }
        }

        private void SetGroupVisual(CanvasGroup group, bool active)
        {
            if (group == null) return;
            group.alpha = active ? _activeAlpha : _dimmedAlpha;
            group.interactable = active;
            group.blocksRaycasts = active;
        }

        private void OnBackClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }
}
