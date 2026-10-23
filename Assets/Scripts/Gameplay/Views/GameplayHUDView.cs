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

        [Header("Top Dock - Shadow Crest")]
        [SerializeField] private Image shadowCrest;

        [Header("Top Dock - Command Timeline ")]
        [SerializeField] private Transform slotsContainer;
        [SerializeField] private Image slotTemplate;     
        [SerializeField] private Image[] timelineSlots;      // Dự phòng  tự kéo tay

        [Header("Single Shared Arrow Sprite (Tự động xoay góc)")]
        [SerializeField] private Sprite arrowSprite;
        [SerializeField] private ArrowOrientation defaultArrowOrientation = ArrowOrientation.PointsRight;

        [Header("Separate Sprites (Dự phòng nếu không dùng chung)")]
        [SerializeField] private Sprite slotEmptySprite;
        [SerializeField] private Sprite arrowUp;
        [SerializeField] private Sprite arrowDown;
        [SerializeField] private Sprite arrowLeft;
        [SerializeField] private Sprite arrowRight;

        [Header("Status Text")]
        [SerializeField] private TextMeshProUGUI stepCountText;
        
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
        /// Cập nhật dải Timeline mũi tên theo chuỗi History và vị trí rượt đuổi của Shadow.
        /// Tự động sinh thêm slot bằng code nếu người chơi đi nhiều bước hơn.
        /// </summary>
        public void UpdateCommandTimeline(IReadOnlyList<EntityCommand> history, int shadowDelaySteps)
        {
            int playerSteps = history?.Count ?? 0;
            int shadowStepIndex = (shadowDelaySteps > 0 && playerSteps >= shadowDelaySteps)
                ? playerSteps - shadowDelaySteps
                : -1;

            // 1. Tự động sinh slot bằng code nếu có slotsContainer và slotTemplate
            EnsureSlotsCapacity(Mathf.Max(playerSteps, shadowDelaySteps));

            IList<Image> activeSlots = _spawnedSlots.Count > 0 ? _spawnedSlots : timelineSlots;

            if (activeSlots != null)
            {
                for (int i = 0; i < activeSlots.Count; i++)
                {
                    if (activeSlots[i] is null) continue;

                    if (i < playerSteps)
                    {
                        // Gán hình mũi tên đúng theo lệnh đã đi
                        activeSlots[i].gameObject.SetActive(true);
                        activeSlots[i].sprite = GetArrowSprite(history[i]);

                        // Xoay góc mũi tên theo hướng di chuyển
                        activeSlots[i].rectTransform.localEulerAngles = new Vector3(0f, 0f, GetCommandAngle(history[i]));

                        // Trạng thái nuốt bước chân của Shadow:
                        if (shadowStepIndex >= 0 && i < shadowStepIndex)
                        {
                            // Đã bị Shadow nuốt qua -> Tím tối mờ
                            activeSlots[i].color = new Color(0.45f, 0.15f, 0.55f, 0.45f);
                        }
                        else if (shadowStepIndex >= 0 && i == shadowStepIndex)
                        {
                            // Vị trí Shadow đang đứng -> Tím sáng rực rỡ
                            activeSlots[i].color = new Color(0.85f, 0.2f, 1f, 1f);
                        }
                        else
                        {
                            // Bước chân an toàn của Player -> Trắng / Vàng sáng
                            activeSlots[i].color = Color.white;
                        }
                    }
                    else
                    {
                        // Ô phía trước chưa bước tới -> Ô rỗng mờ
                        activeSlots[i].gameObject.SetActive(true);
                        if (slotEmptySprite != null)
                            activeSlots[i].sprite = slotEmptySprite;

                        activeSlots[i].rectTransform.localEulerAngles = Vector3.zero;
                        activeSlots[i].color = new Color(1f, 1f, 1f, 0.2f);
                    }
                }
            }

            //  Cập nhật Huy hiệu Shadow ở bên trái Dock:
            if (shadowCrest != null)
            {
                bool isAwake = shadowDelaySteps > 0 && playerSteps >= shadowDelaySteps;
                shadowCrest.color = isAwake ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }

            // Cập nhật Text số bước
            if (stepCountText != null)
            {
                stepCountText.text = $"{playerSteps}";
            }
        }

        private void EnsureSlotsCapacity(int requiredCount)
        {
            if (slotsContainer == null || slotTemplate == null) return;

            while (_spawnedSlots.Count < requiredCount)
            {
                Image newSlot = Instantiate(slotTemplate, slotsContainer);
                newSlot.gameObject.SetActive(true);
                _spawnedSlots.Add(newSlot);
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
