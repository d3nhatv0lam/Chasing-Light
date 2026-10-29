using UnityEngine;

namespace ChasingLight
{
    public class GateView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Sprites")]
        [SerializeField] private Sprite closedSprite;
        [SerializeField] private Sprite openedSprite;

        [Header("Fallback Colors (khi không có sprite)")]
        [SerializeField] private Color closedColor = new Color(0.7f, 0.2f, 0.2f, 1f); // Đỏ sẫm
        [SerializeField] private Color openColor = new Color(0.7f, 0.2f, 0.2f, 0.25f); // Bán trong suốt khi mở

        public int Id { get; private set; }
        public bool IsOpen { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(int id, bool initialOpened)
        {
            Id = id;
            SetOpened(initialOpened);
        }

        public void SetSprites(Sprite closed, Sprite opened)
        {
            closedSprite = closed;
            openedSprite = opened;
            SetOpened(IsOpen);
        }

        public void SetOpened(bool opened)
        {
            IsOpen = opened;
            if (spriteRenderer is null) return;

            if (opened && openedSprite is not null)
            {
                spriteRenderer.sprite = openedSprite;
                spriteRenderer.color = Color.white;
            }
            else if (!opened && closedSprite is not null)
            {
                spriteRenderer.sprite = closedSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                // Fallback màu nếu chưa gán sprite
                spriteRenderer.color = opened ? openColor : closedColor;
            }
        }
    }
}
