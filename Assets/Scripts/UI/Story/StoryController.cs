using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Điều khiển cutscene cốt truyện kiểu visual novel:
    /// ảnh fade, lời dẫn chạy từng chữ, click 2 nấc (hiện đủ chữ -> sang trang), Skip trực tiếp.
    /// </summary>
    public class StoryController : MonoBehaviour
    {
        [Serializable]
        public class StorySlide
        {
            public Sprite storyImage;
            [TextArea(3, 8)] public string storyText;
        }

        private enum State
        {
            Transitioning, // đang fade ảnh
            Typing,        // đang gõ chữ
            WaitingInput,  // gõ xong, chờ người chơi
            Finished
        }

        [Header("Nội dung")]
        [SerializeField] private List<StorySlide> slides = new List<StorySlide>
        {
            new StorySlide { storyText =
                "The Garden of Rainbow is a realm where fae witches live joyfully, brimming with laughter and boundless happiness. It is guarded by the power of the ten Dawnstones." },
            new StorySlide { storyText =
                "Just as everyone was lost in joyous song and dance, a dark force struck. They were The Fallen Soul, possessing the power to reflect the deepest darkness within every heart. Sowing fear and despair wherever they went, they ruthlessly scoured the land for the Dawnstones." },
            new StorySlide { storyText =
                "To temporarily shield her people from the onslaught, the Queen of the Rainbow Garden scattered the stones across the realm. Now, a grand quest has been laid forth: to retrieve the Dawnstones and return them to the sacred altar. And you are the chosen one." },
            new StorySlide { storyText =
                "This is the ancient tome detailing how to awaken the power of the Dawnstones.\nThe book reads: Return the divine stones to their rightful place. Beware of your own subconscious, for if you are unwary, you shall be forever consumed by it." }
        };

        // TODO: điền tên scene đích khi nhóm thảo luận xong (nhớ thêm vào Build Settings).
        [Tooltip("TODO: scene sẽ load sau khi hết cốt truyện hoặc bấm Skip. Để trống = chưa load scene nào.")]
        [SerializeField] private string nextSceneName = "";

        [Header("UI")]
        [SerializeField] private Image displayImage;
        [SerializeField] private CanvasGroup displayCanvasGroup;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private Button nextButton;
        [SerializeField] private CanvasGroup nextIndicatorGroup;
        [SerializeField] private Button skipButton;
        [Tooltip("Button trong suốt phủ toàn màn hình (đặt trên StoryDisplay) để bấm đâu cũng tiếp tục.")]
        [SerializeField] private Button clickCatcherButton;

        [Header("Tốc độ")]
        [SerializeField] private float typingSpeed = 0.035f;
        [SerializeField] private float imageFadeDuration = 0.4f;

        [Header("Nút ▼ nảy")]
        [SerializeField] private float bounceAmplitude = 6f;
        [SerializeField] private float bounceSpeed = 4f;

        private State state = State.Transitioning;
        private int currentIndex;
        private int lastAdvanceFrame = -1;
        private Coroutine slideRoutine;
        private Coroutine bounceRoutine;
        private RectTransform indicatorRect;
        private Vector2 indicatorOriginPos;

        private void Awake()
        {
            if (nextIndicatorGroup != null)
            {
                indicatorRect = nextIndicatorGroup.GetComponent<RectTransform>();
                if (indicatorRect != null)
                    indicatorOriginPos = indicatorRect.anchoredPosition;
            }
        }

        private void Start()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            if (nextButton != null) nextButton.onClick.AddListener(OnAdvanceButtonClicked);
            if (clickCatcherButton != null) clickCatcherButton.onClick.AddListener(OnAdvanceButtonClicked);
            if (skipButton != null) skipButton.onClick.AddListener(OnSkipClicked);

            SetNextIndicator(false);
            ShowSlide(0);
        }

        private void OnDestroy()
        {
            if (nextButton != null) nextButton.onClick.RemoveListener(OnAdvanceButtonClicked);
            if (clickCatcherButton != null) clickCatcherButton.onClick.RemoveListener(OnAdvanceButtonClicked);
            if (skipButton != null) skipButton.onClick.RemoveListener(OnSkipClicked);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                HandleAdvance();
        }

        // ---------------------------------------------------------------- Input

        private void OnAdvanceButtonClicked()
        {
            // Bỏ chọn button để Space/Enter không kích hoạt Submit lần nữa (tránh nhảy 2 trang).
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);

            HandleAdvance();
        }

        private void OnSkipClicked()
        {
            FinishStory();
        }

        /// <summary>Click 2 nấc: đang gõ -> hiện đủ chữ; gõ xong -> sang trang.</summary>
        private void HandleAdvance()
        {
            // Chặn nhiều nguồn input cùng kích hoạt trong một frame.
            if (lastAdvanceFrame == Time.frameCount) return;
            lastAdvanceFrame = Time.frameCount;

            switch (state)
            {
                case State.Typing:
                    CompleteTyping();
                    break;
                case State.WaitingInput:
                    if (currentIndex + 1 < slides.Count) ShowSlide(currentIndex + 1);
                    else FinishStory();
                    break;
                // Transitioning / Finished: bỏ qua
            }
        }

        // ---------------------------------------------------------------- Slides

        private void ShowSlide(int index)
        {
            currentIndex = index;

            if (slideRoutine != null) StopCoroutine(slideRoutine);
            slideRoutine = StartCoroutine(PlaySlideRoutine(slides[index], isFirst: index == 0));
        }

        private IEnumerator PlaySlideRoutine(StorySlide slide, bool isFirst)
        {
            state = State.Transitioning;
            SetNextIndicator(false);
            dialogueText.text = string.Empty;

            yield return FadeImageRoutine(slide.storyImage, isFirst);

            state = State.Typing;
            yield return TypeTextRoutine(slide.storyText);

            state = State.WaitingInput;
            SetNextIndicator(true);
            slideRoutine = null;
        }

        private IEnumerator FadeImageRoutine(Sprite newSprite, bool fadeInOnly)
        {
            float half = Mathf.Max(0.01f, imageFadeDuration * 0.5f);

            if (!fadeInOnly)
            {
                for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
                {
                    displayCanvasGroup.alpha = Mathf.Lerp(1f, 0f, t / half);
                    yield return null;
                }
            }

            displayCanvasGroup.alpha = 0f;
            if (newSprite != null) displayImage.sprite = newSprite;

            for (float t = 0f; t < half; t += Time.unscaledDeltaTime)
            {
                displayCanvasGroup.alpha = Mathf.Lerp(0f, 1f, t / half);
                yield return null;
            }

            displayCanvasGroup.alpha = 1f;
        }

        private IEnumerator TypeTextRoutine(string fullText)
        {
            dialogueText.text = fullText;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();

            int total = dialogueText.textInfo.characterCount;
            for (int i = 1; i <= total; i++)
            {
                dialogueText.maxVisibleCharacters = i;
                yield return new WaitForSecondsRealtime(typingSpeed);
            }
        }

        private void CompleteTyping()
        {
            if (slideRoutine != null)
            {
                StopCoroutine(slideRoutine);
                slideRoutine = null;
            }

            dialogueText.maxVisibleCharacters = int.MaxValue;
            state = State.WaitingInput;
            SetNextIndicator(true);
        }

        // ---------------------------------------------------------------- Finish

        private void FinishStory()
        {
            if (state == State.Finished) return;

            if (string.IsNullOrEmpty(nextSceneName))
            {
                // TODO: chưa có scene đích, giữ nguyên màn hình hiện tại.
                Debug.LogWarning("[StoryController] TODO: chưa đặt nextSceneName, chưa chuyển scene.");
                return;
            }

            state = State.Finished;
            StopAllCoroutines();
            SceneManager.LoadScene(nextSceneName);
        }

        // ---------------------------------------------------------------- Indicator

        private void SetNextIndicator(bool visible)
        {
            if (bounceRoutine != null)
            {
                StopCoroutine(bounceRoutine);
                bounceRoutine = null;
            }

            if (indicatorRect != null)
                indicatorRect.anchoredPosition = indicatorOriginPos;

            if (nextIndicatorGroup != null)
            {
                nextIndicatorGroup.alpha = visible ? 1f : 0f;
                nextIndicatorGroup.interactable = visible;
                nextIndicatorGroup.blocksRaycasts = visible;
            }

            if (visible && indicatorRect != null)
                bounceRoutine = StartCoroutine(BounceRoutine());
        }

        private IEnumerator BounceRoutine()
        {
            float startTime = Time.unscaledTime;
            while (true)
            {
                float offset = Mathf.Sin((Time.unscaledTime - startTime) * bounceSpeed) * bounceAmplitude;
                indicatorRect.anchoredPosition = indicatorOriginPos + new Vector2(0f, offset);
                yield return null;
            }
        }

        // ---------------------------------------------------------------- Validate

        private bool ValidateReferences()
        {
            bool ok = true;

            if (slides == null || slides.Count == 0)
            {
                Debug.LogError("[StoryController] Danh sách slides trống.", this);
                ok = false;
            }
            if (displayImage == null) { Debug.LogError("[StoryController] Thiếu Display Image.", this); ok = false; }
            if (displayCanvasGroup == null) { Debug.LogError("[StoryController] Thiếu Display Canvas Group.", this); ok = false; }
            if (dialogueText == null) { Debug.LogError("[StoryController] Thiếu Dialogue Text.", this); ok = false; }

            if (nextButton == null) Debug.LogWarning("[StoryController] Chưa gán Next Button.", this);
            if (nextIndicatorGroup == null) Debug.LogWarning("[StoryController] Chưa gán Next Indicator Group.", this);
            if (skipButton == null) Debug.LogWarning("[StoryController] Chưa gán Skip Button.", this);
            if (clickCatcherButton == null) Debug.LogWarning("[StoryController] Chưa gán Click Catcher Button (không bấm toàn màn hình được).", this);

            if (slides != null)
            {
                for (int i = 0; i < slides.Count; i++)
                {
                    if (slides[i].storyImage == null)
                        Debug.LogWarning($"[StoryController] Slide {i + 1} chưa có ảnh.", this);
                }
            }

            return ok;
        }
    }
}
