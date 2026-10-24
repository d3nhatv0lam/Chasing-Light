using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight
{
    public enum ArrowOrientation
    {
        PointsRight = 0,
        PointsUp = 1,
        PointsLeft = 2,
        PointsDown = 3
    }

    public class GameplayHUDView : MonoBehaviour
    {
        [Header("D-pad Directional Buttons")]
        [SerializeField] private Button btnUp;
        [SerializeField] private Button btnDown;
        [SerializeField] private Button btnLeft;
        [SerializeField] private Button btnRight;

        [Header("Canvas Control")]
        [SerializeField] private CanvasGroup dpadCanvasGroup;

        [Header("Top Dock - Layout & Dimensions")]
        [SerializeField] private RectTransform topDockRect;
        [SerializeField] private Image dockBackground;
        [SerializeField] private Sprite dockBackgroundSprite;
        [SerializeField] private float slotSize = 52f;
        [SerializeField] private float slotSpacing = 8f;
        [SerializeField] private float crestSize = 60f;
        [SerializeField] private float dockPaddingX = 20f;
        [SerializeField] private float dockPaddingY = 8f;

        [Header("Top Dock - Shadow Crest & Countdown")]
        [SerializeField] private Image shadowCrest;
        [SerializeField] private TextMeshProUGUI stepCountText;

        [Header("Top Dock - Shadow Step Indicator (ui2_23)")]
        [SerializeField] private RectTransform shadowStepIndicator;

        [Header("Top Dock - Command Timeline")]
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private Image slotTemplate;
        [SerializeField] private Image[] timelineSlots;

        [Header("Arrow Sprite & Orientation")]
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private ArrowOrientation defaultArrowOrientation = ArrowOrientation.PointsUp;
        [SerializeField] private Sprite slotEmptySprite;
        [SerializeField] private Sprite arrowUp;
        [SerializeField] private Sprite arrowDown;
        [SerializeField] private Sprite arrowLeft;
        [SerializeField] private Sprite arrowRight;

        [Header("Status & Banner Text")]
        [SerializeField] private TextMeshProUGUI bannerText;
        [SerializeField] private RectTransform bannerContainer;

        private readonly List<Image> _spawnedSlots = new();

        public event Action<EntityCommand> OnCommandInput;

        private void Awake()
        {
            RegisterButtonEvents();

            if (slotTemplate != null)
                slotTemplate.gameObject.SetActive(false);

            if (shadowStepIndicator != null)
                shadowStepIndicator.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            UnregisterButtonEvents();
        }

        private void RegisterButtonEvents()
        {
            if (btnUp != null) btnUp.onClick.AddListener(() => HandleDirectionClicked(EntityCommand.Up));
            if (btnDown != null) btnDown.onClick.AddListener(() => HandleDirectionClicked(EntityCommand.Down));
            if (btnLeft != null) btnLeft.onClick.AddListener(() => HandleDirectionClicked(EntityCommand.Left));
            if (btnRight != null) btnRight.onClick.AddListener(() => HandleDirectionClicked(EntityCommand.Right));
        }

        private void UnregisterButtonEvents()
        {
            if (btnUp != null) btnUp.onClick.RemoveAllListeners();
            if (btnDown != null) btnDown.onClick.RemoveAllListeners();
            if (btnLeft != null) btnLeft.onClick.RemoveAllListeners();
            if (btnRight != null) btnRight.onClick.RemoveAllListeners();
        }

        private void HandleDirectionClicked(EntityCommand command)
        {
            OnCommandInput?.Invoke(command);
        }

        public void SetInputInteractable(bool interactable)
        {
            if (dpadCanvasGroup != null)
            {
                dpadCanvasGroup.interactable = interactable;
                dpadCanvasGroup.blocksRaycasts = interactable;
            }
            else
            {
                if (btnUp != null) btnUp.interactable = interactable;
                if (btnDown != null) btnDown.interactable = interactable;
                if (btnLeft != null) btnLeft.interactable = interactable;
                if (btnRight != null) btnRight.interactable = interactable;
            }
        }

        /// <summary>
        /// Cập nhật dải Timeline gồm chính xác N ô theo kịch bản Honkai: Star Rail:
        /// - Pha 1 (0..N bước): Người chơi đi, các ô được lấp đầy mũi tên từ trái sang phải, số đếm ngược giảm về 0.
        /// - Pha 2 (Sau N bước): Dock CHỐT CỐ ĐỊNH N mũi tên đã ghi; Shadow bắt đầu thực thi từ ô 1 -> N theo từng bước tiếp theo của Player;
        ///   Con trỏ Shadow (ui2_23) nhảy từ ô 1 -> N bên dưới Dock.
        /// - Pha 3: Sau khi Shadow đi hết N bước, Shadow dừng hẳn; Player tiếp tục đi về đích.
        /// </summary>
        public void UpdateCommandTimeline(IReadOnlyList<EntityCommand> history, int shadowDelaySteps)
        {
            int playerSteps = history?.Count ?? 0;
            int capacity = shadowDelaySteps > 0 ? shadowDelaySteps : playerSteps;
            if (capacity <= 0) return;

            // 1. Luôn duy trì chính xác đúng N ô hiển thị
            EnsureSlotsCapacity(capacity);

            // 2. Canh giữa và auto-fit kích thước Dock theo N ô
            AlignDockLayout(capacity);

            IList<Image> activeSlots = _spawnedSlots.Count > 0 ? _spawnedSlots : timelineSlots;
            if (activeSlots != null)
            {
                // Dock luôn cố định N bước đầu tiên đã ghi (Không cuộn trượt)
                for (int i = 0; i < capacity; i++)
                {
                    if (i >= activeSlots.Count || activeSlots[i] == null) continue;

                    if (i < playerSteps && i < history.Count)
                    {
                        ApplySlotCommand(activeSlots[i], history[i]);
                    }
                    else
                    {
                        ApplySlotEmpty(activeSlots[i]);
                    }
                }
            }

            // 3. Cập nhật con trỏ bước Shadow (ui2_23) bên dưới Dock
            UpdateShadowStepIndicator(playerSteps, capacity);

            // 4. Cập nhật số đếm ngược trên huy hiệu:
            if (stepCountText != null)
            {
                if (shadowDelaySteps > 0)
                {
                    int remaining = Mathf.Max(0, shadowDelaySteps - playerSteps);
                    stepCountText.text = $"{remaining}";
                }
                else
                {
                    stepCountText.text = $"{playerSteps}";
                }
            }

            // 5. Cập nhật thông báo Banner phía trên Dock:
            if (bannerText != null && shadowDelaySteps > 0)
            {
                bannerText.text = $"Sau {shadowDelaySteps} bước, bản thân trong quá khứ sẽ biến thành kẻ địch";
            }
        }

        private void ApplySlotCommand(Image slotImg, EntityCommand command)
        {
            slotImg.gameObject.SetActive(true);

            // Kiểm tra cấu trúc đa tầng (Layered Slot) nếu có
            Transform emptyDots = slotImg.transform.Find("EmptyDots");
            Transform arrowIcon = slotImg.transform.Find("ArrowIcon");

            if (emptyDots != null) emptyDots.gameObject.SetActive(false);

            if (arrowIcon != null)
            {
                arrowIcon.gameObject.SetActive(true);
                arrowIcon.localEulerAngles = new Vector3(0f, 0f, GetCommandAngle(command));
            }
            else
            {
                // Fallback trường hợp slot đơn
                slotImg.sprite = GetArrowSprite(command);
                slotImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, GetCommandAngle(command));
                slotImg.color = Color.white;
            }
        }

        private void ApplySlotEmpty(Image slotImg)
        {
            slotImg.gameObject.SetActive(true);

            Transform emptyDots = slotImg.transform.Find("EmptyDots");
            Transform arrowIcon = slotImg.transform.Find("ArrowIcon");

            if (emptyDots != null) emptyDots.gameObject.SetActive(true);
            if (arrowIcon != null) arrowIcon.gameObject.SetActive(false);

            if (arrowIcon == null && emptyDots == null)
            {
                if (slotEmptySprite != null)
                {
                    slotImg.sprite = slotEmptySprite;
                    slotImg.color = Color.white;
                }
                else
                {
                    slotImg.color = new Color(1f, 1f, 1f, 0.25f);
                }
                slotImg.rectTransform.localEulerAngles = Vector3.zero;
            }
        }

        private void UpdateShadowStepIndicator(int playerSteps, int capacity)
        {
            if (shadowStepIndicator == null) return;

            // Chỉ hiển thị khi Shadow bắt đầu thực thi kịch bản (playerSteps > capacity)
            if (playerSteps <= capacity)
            {
                shadowStepIndicator.gameObject.SetActive(false);
                return;
            }

            int shadowIndex = playerSteps - capacity - 1;
            if (shadowIndex < capacity)
            {
                // Shadow đang thực thi ô thứ shadowIndex (từ 0 đến capacity - 1)
                shadowStepIndicator.gameObject.SetActive(true);
                float slotCenterX = dockPaddingX + crestSize + 10f + shadowIndex * (slotSize + slotSpacing) + slotSize * 0.5f;
                shadowStepIndicator.anchorMin = new Vector2(0f, 0f);
                shadowStepIndicator.anchorMax = new Vector2(0f, 0f);
                shadowStepIndicator.pivot = new Vector2(0.5f, 1f);
                shadowStepIndicator.anchoredPosition = new Vector2(slotCenterX, -6f);
            }
            else
            {
                // Shadow đã đi hết toàn bộ N bước và dừng hẳn -> ẩn con trỏ
                shadowStepIndicator.gameObject.SetActive(false);
            }
        }

        private void AlignDockLayout(int capacity)
        {
            if (capacity <= 0) return;

            if (topDockRect == null && slotsContainer != null && slotsContainer.parent != null)
            {
                topDockRect = slotsContainer.parent as RectTransform;
            }

            float totalSlotsWidth = capacity * slotSize + (capacity - 1) * slotSpacing;
            float totalDockWidth = dockPaddingX * 2f + crestSize + 10f + totalSlotsWidth;
            float totalDockHeight = Mathf.Max(crestSize, slotSize) + dockPaddingY * 2f;

            if (topDockRect != null)
            {
                topDockRect.anchorMin = new Vector2(0.5f, 1f);
                topDockRect.anchorMax = new Vector2(0.5f, 1f);
                topDockRect.pivot = new Vector2(0.5f, 1f);
                topDockRect.anchoredPosition = new Vector2(0f, -44f);
                topDockRect.sizeDelta = new Vector2(totalDockWidth, totalDockHeight);

                if (dockBackground == null)
                    dockBackground = topDockRect.GetComponent<Image>();

                if (dockBackground != null)
                {
                    if (dockBackgroundSprite != null)
                    {
                        dockBackground.sprite = dockBackgroundSprite;
                        dockBackground.type = Image.Type.Sliced;
                    }
                    dockBackground.color = Color.white;
                }
            }

            // Banner Container & Text
            if (bannerText != null)
            {
                var bannerTransform = bannerContainer != null ? bannerContainer : bannerText.rectTransform;
                if (topDockRect != null && bannerTransform.parent != topDockRect.parent)
                    bannerTransform.SetParent(topDockRect.parent, false);

                bannerTransform.anchorMin = new Vector2(0.5f, 1f);
                bannerTransform.anchorMax = new Vector2(0.5f, 1f);
                bannerTransform.pivot = new Vector2(0.5f, 1f);
                bannerTransform.anchoredPosition = new Vector2(0f, -10f);
            }

            // Huy hiệu ShadowCrest
            if (shadowCrest != null)
            {
                shadowCrest.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                shadowCrest.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                shadowCrest.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                shadowCrest.rectTransform.sizeDelta = new Vector2(crestSize, crestSize);
                shadowCrest.rectTransform.anchoredPosition = new Vector2(dockPaddingX + crestSize * 0.5f, 0f);

                if (stepCountText != null)
                {
                    if (stepCountText.transform.parent != shadowCrest.transform)
                        stepCountText.transform.SetParent(shadowCrest.transform, false);

                    stepCountText.rectTransform.anchorMin = new Vector2(1f, 0f);
                    stepCountText.rectTransform.anchorMax = new Vector2(1f, 0f);
                    stepCountText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    stepCountText.rectTransform.anchoredPosition = new Vector2(-2f, 8f);
                }
            }

            // Slots Container
            if (slotsContainer is RectTransform rtContainer)
            {
                rtContainer.anchorMin = new Vector2(0f, 0.5f);
                rtContainer.anchorMax = new Vector2(0f, 0.5f);
                rtContainer.pivot = new Vector2(0f, 0.5f);
                rtContainer.anchoredPosition = new Vector2(dockPaddingX + crestSize + 10f, 0f);
                rtContainer.sizeDelta = new Vector2(totalSlotsWidth, slotSize);
            }

            if (slotsContainer != null && slotsContainer.TryGetComponent<HorizontalLayoutGroup>(out var hlg))
            {
                hlg.spacing = slotSpacing;
                hlg.childAlignment = TextAnchor.MiddleLeft;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;
                hlg.childForceExpandWidth = false;
                hlg.childForceExpandHeight = false;
            }

            if (slotTemplate != null)
            {
                slotTemplate.rectTransform.sizeDelta = new Vector2(slotSize, slotSize);
            }

            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                if (_spawnedSlots[i] != null)
                {
                    _spawnedSlots[i].rectTransform.sizeDelta = new Vector2(slotSize, slotSize);
                }
            }
        }

        private void EnsureSlotsCapacity(int exactCount)
        {
            if (slotsContainer == null || slotTemplate == null) return;

            while (_spawnedSlots.Count < exactCount)
            {
                Image newSlot = Instantiate(slotTemplate, slotsContainer);
                newSlot.preserveAspect = true;
                _spawnedSlots.Add(newSlot);
            }

            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                _spawnedSlots[i].gameObject.SetActive(i < exactCount);
            }
        }

        private float GetCommandAngle(EntityCommand command)
        {
            float targetAngle = command switch
            {
                EntityCommand.Right => 0f,
                EntityCommand.Up => 90f,
                EntityCommand.Left => 180f,
                EntityCommand.Down => 270f,
                _ => 0f
            };

            float offset = defaultArrowOrientation switch
            {
                ArrowOrientation.PointsUp => -90f,
                ArrowOrientation.PointsLeft => -180f,
                ArrowOrientation.PointsDown => -270f,
                _ => 0f
            };

            return targetAngle + offset;
        }

        private Sprite GetArrowSprite(EntityCommand command)
        {
            if (arrowSprite != null) return arrowSprite;

            return command switch
            {
                EntityCommand.Up => arrowUp != null ? arrowUp : slotEmptySprite,
                EntityCommand.Down => arrowDown != null ? arrowDown : slotEmptySprite,
                EntityCommand.Left => arrowLeft != null ? arrowLeft : slotEmptySprite,
                EntityCommand.Right => arrowRight != null ? arrowRight : slotEmptySprite,
                _ => slotEmptySprite
            };
        }

        public void UpdateTimeline(int currentSteps, int targetSteps)
        {
            if (stepCountText != null)
                stepCountText.text = $"{currentSteps}";
        }
    }
}
