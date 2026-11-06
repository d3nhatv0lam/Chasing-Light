using UnityEngine;

namespace ChasingLight
{
    public class SwitchView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Sprites")]
        [SerializeField] private Sprite unpressedSprite;
        [SerializeField] private Sprite pressedSprite;

        [Header("Fallback Colors (khi không có sprite)")]
        [SerializeField] private Color offColor = new Color(0.85f, 0.25f, 0.25f, 1f); // Đỏ khi chưa kích hoạt
        [SerializeField] private Color onColor = new Color(0.25f, 0.85f, 0.35f, 1f);  // Xanh lá khi đã kích hoạt

        public int Id { get; private set; }
        public bool IsActivated { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(int id, bool initialActivated)
        {
            Id = id;
            SetActivated(initialActivated);
        }

        public void SetSprites(Sprite unpressed, Sprite pressed)
        {
            unpressedSprite = unpressed;
            pressedSprite = pressed;
            SetActivated(IsActivated);
        }

        public void SetActivated(bool activated)
        {
            IsActivated = activated;
            if (spriteRenderer == null) return;

            if (activated && pressedSprite != null)
            {
                spriteRenderer.sprite = pressedSprite;
                spriteRenderer.color = Color.white;
            }
            else if (!activated && unpressedSprite != null)
            {
                spriteRenderer.sprite = unpressedSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                spriteRenderer.color = activated ? onColor : offColor;
            }
        }
    }
}
