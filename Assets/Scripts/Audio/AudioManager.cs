using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace ChasingLight
{
    /// <summary>
    /// Audio manager
    /// Auto-Bootstrap
    /// </summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer Reference")]
        [SerializeField] private AudioMixer masterMixer;

        [Header("Sound Configurations")]
        [SerializeField] private List<SoundData> soundLibrary = new();

        [Header("SFX Pool Configuration")]
        [SerializeField] private int sfxPoolSize = 12;

        // BGM Dual-sources
        private AudioSource _bgmSourceA;
        private AudioSource _bgmSourceB;
        private bool _isSourceAActive = true;
        private Coroutine _crossFadeCoroutine;

        // UI AudioSource
        private AudioSource _uiSource;

        // SFX AudioSource Pool
        private readonly Queue<AudioSource> _sfxPool = new();
        private readonly Dictionary<SoundID, SoundData> _soundLookup = new();

        // PlayerPrefs Keys
        private const string KEY_MASTER = "VOL_MASTER";
        private const string KEY_BGM = "VOL_BGM";
        private const string KEY_SFX = "VOL_SFX";
        private const string KEY_UI = "VOL_UI";

        // AUTO-BOOTSTRAP
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoBootstrap()
        {
            if (Instance == null)
            {
                var prefab = Resources.Load<AudioManager>("[AudioManager]");
                if (prefab != null)
                {
                    var instance = Instantiate(prefab);
                    instance.name = "[AudioManager]";
                    DontDestroyOnLoad(instance.gameObject);
                }
                // Fallback
                else
                {
                    var go = new GameObject("[AudioManager]");
                    go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            InitializeLookup();
            LoadVolumePreferences();
        }

        private void InitializeAudioSources()
        {
            _bgmSourceA = CreateAudioSource("BGM_Source_A", isLooping: true);
            _bgmSourceB = CreateAudioSource("BGM_Source_B", isLooping: true);
            _uiSource   = CreateAudioSource("UI_Source", isLooping: false);

            var poolParent = new GameObject("SFX_Pool").transform;
            poolParent.SetParent(transform);

            for (int i = 0; i < sfxPoolSize; i++)
            {
                var sfxSrc = CreateAudioSource($"SFX_Source_{i}", isLooping: false);
                sfxSrc.transform.SetParent(poolParent);
                _sfxPool.Enqueue(sfxSrc);
            }
        }

        private AudioSource CreateAudioSource(string objName, bool isLooping)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = isLooping;
            src.spatialBlend = 0f; // 2D Audio
            return src;
        }

        public void InitializeLookup()
        {
            _soundLookup.Clear();
            foreach (var sound in soundLibrary)
            {
                if (sound == null) continue;
                if (!_soundLookup.TryAdd(sound.id, sound))
                {
                    Debug.LogWarning($"[AudioManager] Trùng lặp cấu hình SoundID: {sound.id}");
                }
            }
        }

        // BGM APIS

        public void PlayBGM(SoundID id, float fadeDuration = 0.8f)
        {
            if (!_soundLookup.TryGetValue(id, out var data)) return;
            var clip = data.GetRandomClip();
            if (clip == null) return;

            var currentActive = _isSourceAActive ? _bgmSourceA : _bgmSourceB;
            if (currentActive.clip == clip && currentActive.isPlaying) return;

            if (_crossFadeCoroutine != null) StopCoroutine(_crossFadeCoroutine);
            _crossFadeCoroutine = StartCoroutine(CrossFadeBGMCoroutine(data, clip, fadeDuration));
        }

        private IEnumerator CrossFadeBGMCoroutine(SoundData data, AudioClip newClip, float duration)
        {
            var currentSource = _isSourceAActive ? _bgmSourceA : _bgmSourceB;
            var nextSource = _isSourceAActive ? _bgmSourceB : _bgmSourceA;
            _isSourceAActive = !_isSourceAActive;

            nextSource.clip = newClip;
            nextSource.outputAudioMixerGroup = data.mixerGroup;
            nextSource.volume = 0f;
            nextSource.Play();

            float timer = 0f;
            float startVol = currentSource.isPlaying ? currentSource.volume : 0f;
            float targetVol = data.volume;

            while (timer < duration)
            {
                timer += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(timer / duration);

                if (currentSource.isPlaying)
                    currentSource.volume = Mathf.Lerp(startVol, 0f, t);

                nextSource.volume = Mathf.Lerp(0f, targetVol, t);
                yield return null;
            }

            currentSource.Stop();
            currentSource.volume = 0f;
            nextSource.volume = targetVol;
            _crossFadeCoroutine = null;
        }

        public void StopBGM()
        {
            if (_crossFadeCoroutine != null) StopCoroutine(_crossFadeCoroutine);
            _bgmSourceA.Stop();
            _bgmSourceB.Stop();
        }

        // SFX APIS

        public void PlaySFX(SoundID id)
        {
            if (!_soundLookup.TryGetValue(id, out var data)) return;
            if (Time.unscaledTime - data.lastPlayedTime < data.cooldown) return;

            var clip = data.GetRandomClip();
            if (clip == null) return;

            data.lastPlayedTime = Time.unscaledTime;

            var src = _sfxPool.Dequeue();
            _sfxPool.Enqueue(src);

            src.outputAudioMixerGroup = data.mixerGroup;
            src.pitch = Random.Range(data.minPitch, data.maxPitch);
            src.PlayOneShot(clip, data.volume);
        }

        // UI APIS

        public void PlayUI(SoundID id)
        {
            if (!_soundLookup.TryGetValue(id, out var data)) return;
            var clip = data.GetRandomClip();
            if (clip == null) return;

            _uiSource.outputAudioMixerGroup = data.mixerGroup;
            _uiSource.pitch = Random.Range(data.minPitch, data.maxPitch);
            _uiSource.PlayOneShot(clip, data.volume);
        }

        // VOLUME CONTROLS

        public void SetMasterVolume(float linear) => SetMixerVolume("MasterVolume", KEY_MASTER, linear);
        public void SetBGMVolume(float linear)    => SetMixerVolume("BGMVolume", KEY_BGM, linear);
        public void SetSFXVolume(float linear)    => SetMixerVolume("SFXVolume", KEY_SFX, linear);
        public void SetUIVolume(float linear)     => SetMixerVolume("UIVolume", KEY_UI, linear);

        public float GetMasterVolume() => PlayerPrefs.GetFloat(KEY_MASTER, 1f);
        public float GetBGMVolume()    => PlayerPrefs.GetFloat(KEY_BGM, 0.8f);
        public float GetSFXVolume()    => PlayerPrefs.GetFloat(KEY_SFX, 1f);
        public float GetUIVolume()     => PlayerPrefs.GetFloat(KEY_UI, 1f);

        private void SetMixerVolume(string exposedParam, string prefKey, float linear)
        {
            linear = Mathf.Clamp(linear, 0.0001f, 1f);
            float dB = Mathf.Log10(linear) * 20f;

            if (masterMixer != null)
            {
                masterMixer.SetFloat(exposedParam, dB);
            }

            PlayerPrefs.SetFloat(prefKey, linear);
        }

        private void LoadVolumePreferences()
        {
            SetMasterVolume(GetMasterVolume());
            SetBGMVolume(GetBGMVolume());
            SetSFXVolume(GetSFXVolume());
            SetUIVolume(GetUIVolume());
        }

        //  Used for Editor Setup Tool
        public List<SoundData> SoundLibrary => soundLibrary;
        public AudioMixer MasterMixer { get => masterMixer; set => masterMixer = value; }
    }
}
