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

        [Header("Top Dock - Auto Fit & Dimensions")]
        [SerializeField] private RectTransform topDockRect;
        [SerializeField] private Image dockBackground;
        [SerializeField] private Sprite dockBackgroundSprite;
        [SerializeField] private float slotSize = 52f;
        [SerializeField] private float slotSpacing = 8f;
        [SerializeField] private float crestSize = 60f;
        [SerializeField] private float dockPaddingX = 20f;
        [SerializeField] private float dockPaddingY = 8f;

        [Header("Top Dock - Shadow Crest")]
        [SerializeField] private Image shadowCrest;

        [Header("Top Dock - Command Timeline ")]
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private Image slotTemplate;     
        [SerializeField] private Image[] timelineSlots;      // Dự phòng  tự kéo tay

        [Header("Single Shared Arrow Sprite (Tự động xoay góc)")]
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private ArrowOrientation defaultArrowOrientation = ArrowOrientation.PointsUp;

        [Header("Separate Sprites (Dự phòng nếu không dùng chung)")]
        [SerializeField] private Sprite slotEmptySprite;
        [SerializeField] private Sprite arrowUp;
        [SerializeField] private Sprite arrowDown;
        [SerializeField] private Sprite arrowLeft;
        [SerializeField] private Sprite arrowRight;

        [Header("Status & Banner Text")]
        [SerializeField] private TextMeshProUGUI stepCountText;
        [SerializeField] private TextMeshProUGUI bannerText;
        
        private readonly List<Image> _spawnedSlots = new();

        // Sự kiện phím bấm D-pad bắn ra cho StageController
        public event Action<EntityCommand> OnCommandInput;

        private void Awake()
        {
            RegisterButtonEvents();

            if (slotTemplate != null)
                slotTemplate.gameObject.SetActive(false);
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

        /// <summary>
        /// Khóa hoặc mở tương tác các nút bấm (ngăn người chơi spam phím khi hoạt ảnh đang chạy).
        /// </summary>
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
        /// Cập nhật dải Timeline gồm chính xác N ô (tương ứng N bước trước khi Shadow xuất hiện).
        /// Khi chưa bước tới: hiện ô rỗng (slotEmptySprite).
        /// Khi đã bước: hiện mũi tên hướng đi tương ứng.
        /// Tự động căn giữa màn hình và định vị huy hiệu đếm ngược.
        /// </summary>
        public void UpdateCommandTimeline(IReadOnlyList<EntityCommand> history, int shadowDelaySteps)
        {
            int playerSteps = history?.Count ?? 0;
            int capacity = shadowDelaySteps > 0 ? shadowDelaySteps : playerSteps;

            // 1. Luôn duy trì chính xác N ô hiển thị
            if (capacity > 0)
            {
                EnsureSlotsCapacity(capacity);
            }

            // 2. Tự động căn giữa SlotsContainer và định vị ShadowCrest sang cạnh trái
            AlignDockLayout(capacity);

            IList<Image> activeSlots = _spawnedSlots.Count > 0 ? _spawnedSlots : timelineSlots;

            if (activeSlots != null && capacity > 0)
            {
                if (shadowDelaySteps > 0 && playerSteps >= shadowDelaySteps)
                {
                    // Giai đoạn Shadow đã thức tỉnh: Hiển thị cửa sổ trượt N bước chân đang bị đuổi theo
                    int startIndex = playerSteps - shadowDelaySteps;
                    for (int i = 0; i < capacity; i++)
                    {
                        if (i >= activeSlots.Count || activeSlots[i] == null) continue;
                        int cmdIndex = startIndex + i;
                        if (cmdIndex < playerSteps)
                        {
                            activeSlots[i].gameObject.SetActive(true);
                            activeSlots[i].sprite = GetArrowSprite(history[cmdIndex]);
                            activeSlots[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, GetCommandAngle(history[cmdIndex]));
                            
                            // Ô đầu tiên (vị trí Shadow sắp nuốt tiếp theo) có thể đánh dấu khác màu
                            activeSlots[i].color = (i == 0) ? new Color(0.85f, 0.2f, 1f, 1f) : Color.white;
                        }
                    }
                }
                else
                {
                    // Giai đoạn đếm ngược trước khi Shadow xuất hiện (playerSteps < shadowDelaySteps)
                    for (int i = 0; i < capacity; i++)
                    {
                        if (i >= activeSlots.Count || activeSlots[i] == null) continue;

                        if (i < playerSteps)
                        {
                            // Ô đã bước qua -> hiện mũi tên
                            activeSlots[i].gameObject.SetActive(true);
                            activeSlots[i].sprite = GetArrowSprite(history[i]);
                            activeSlots[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, GetCommandAngle(history[i]));
                            activeSlots[i].color = Color.white;
                        }
                        else
                        {
                            // Ô phía trước chưa bước tới -> hiện ô rỗng
                            activeSlots[i].gameObject.SetActive(true);
                            if (slotEmptySprite != null)
                            {
                                activeSlots[i].sprite = slotEmptySprite;
                                activeSlots[i].color = Color.white;
                            }
                            else
                            {
                                activeSlots[i].color = new Color(1f, 1f, 1f, 0.25f);
                            }

                            activeSlots[i].rectTransform.localEulerAngles = Vector3.zero;
                        }
                    }
                }
            }

            // Cập nhật màu sắc huy hiệu Shadow:
            if (shadowCrest != null)
            {
                bool isAwake = shadowDelaySteps > 0 && playerSteps >= shadowDelaySteps;
                shadowCrest.color = isAwake ? Color.white : new Color(0.8f, 0.8f, 0.8f, 0.9f);
            }

            // Cập nhật Text số bước (đếm lùi số bước còn lại trước khi Shadow xuất hiện):
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

            // Cập nhật Text banner thông báo phía trên:
            if (bannerText != null)
            {
                if (shadowDelaySteps > 0)
                {
                    int remaining = Mathf.Max(0, shadowDelaySteps - playerSteps);
                    if (remaining > 0)
                    {
                        bannerText.text = $"Sau {remaining} bước, bản thân trong quá khứ sẽ biến thành kẻ địch";
                    }
                    else
                    {
                        bannerText.text = "Bản thân trong quá khứ đã thức tỉnh!";
                    }
                }
                else
                {
                    bannerText.text = "";
                }
            }
        }

        private void AlignDockLayout(int capacity)
        {
            if (capacity <= 0) return;

            // 1. Tự tìm TopDock nếu chưa gán
            if (topDockRect == null && slotsContainer != null && slotsContainer.parent != null)
            {
                topDockRect = slotsContainer.parent as RectTransform;
            }

            // 2. Tính toán kích thước chuẩn auto-fit ôm khít dữ liệu
            float totalSlotsWidth = capacity * slotSize + (capacity - 1) * slotSpacing;
            float totalDockWidth = dockPaddingX * 2f + crestSize + 10f + totalSlotsWidth;
            float totalDockHeight = Mathf.Max(crestSize, slotSize) + dockPaddingY * 2f;

            // 3. Tự động co giãn khung nền TopDock ôm khít và căn giữa màn hình
            if (topDockRect != null)
            {
                topDockRect.anchorMin = new Vector2(0.5f, 1f);
                topDockRect.anchorMax = new Vector2(0.5f, 1f);
                topDockRect.pivot = new Vector2(0.5f, 1f);
                topDockRect.anchoredPosition = new Vector2(0f, -44f);
                topDockRect.sizeDelta = new Vector2(totalDockWidth, totalDockHeight);

                // Cấu hình nền viên nang nếu có
                if (dockBackground == null)
                    dockBackground = topDockRect.GetComponent<Image>();

                if (dockBackground == null)
                    dockBackground = topDockRect.gameObject.AddComponent<Image>();

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

            // 3b. Tự động khởi tạo & định vị Banner thông báo ngay phía trên Dock nếu chưa có
            if (bannerText == null && topDockRect != null && topDockRect.parent != null)
            {
                var bannerObj = new GameObject("BannerText", typeof(RectTransform), typeof(TextMeshProUGUI));
                bannerObj.transform.SetParent(topDockRect.parent, false);
                bannerText = bannerObj.GetComponent<TextMeshProUGUI>();
                bannerText.fontSize = 17;
                bannerText.fontStyle = FontStyles.Bold;
                bannerText.alignment = TextAlignmentOptions.Center;
                bannerText.color = new Color(0.9f, 0.92f, 1f, 0.95f);
                if (stepCountText != null && stepCountText.font != null)
                    bannerText.font = stepCountText.font;

                var rt = bannerText.rectTransform;
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(700f, 30f);
                rt.anchoredPosition = new Vector2(0f, -10f);
            }

            // 4. Định vị Huy hiệu ShadowCrest ở góc trái bên trong khung Dock
            if (shadowCrest != null)
            {
                shadowCrest.rectTransform.anchorMin = new Vector2(0f, 0.5f);
                shadowCrest.rectTransform.anchorMax = new Vector2(0f, 0.5f);
                shadowCrest.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                shadowCrest.rectTransform.sizeDelta = new Vector2(crestSize, crestSize);
                shadowCrest.rectTransform.anchoredPosition = new Vector2(dockPaddingX + crestSize * 0.5f, 0f);

                // Gắn StepCountText vào góc dưới bên phải của ShadowCrest (như hình mẫu)
                if (stepCountText != null)
                {
                    if (stepCountText.transform.parent != shadowCrest.transform)
                        stepCountText.transform.SetParent(shadowCrest.transform, false);

                    stepCountText.rectTransform.anchorMin = new Vector2(1f, 0f);
                    stepCountText.rectTransform.anchorMax = new Vector2(1f, 0f);
                    stepCountText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    stepCountText.rectTransform.anchoredPosition = new Vector2(-2f, 8f);
                    stepCountText.fontSize = 20;
                    stepCountText.fontStyle = FontStyles.Bold;
                    stepCountText.alignment = TextAlignmentOptions.Center;
                    stepCountText.color = new Color(1f, 0.92f, 0.55f, 1f); // Màu vàng kim nhạt nổi bật
                }
            }

            // 5. Định vị SlotsContainer ngay sau Huy hiệu và căn đều các ô
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

            // 6. Đảm bảo kích thước template và các ô đã sinh to đẹp (slotSize x slotSize)
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

            // 1. Sinh thêm nếu chưa đủ số ô yêu cầu
            while (_spawnedSlots.Count < exactCount)
            {
                Image newSlot = Instantiate(slotTemplate, slotsContainer);
                newSlot.preserveAspect = true;
                _spawnedSlots.Add(newSlot);
            }

            // 2. Kích hoạt đúng exactCount ô, các ô thừa tắt đi
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
