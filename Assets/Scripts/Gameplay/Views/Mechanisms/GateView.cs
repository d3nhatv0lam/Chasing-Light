using UnityEngine;
using UnityEngine.Serialization;

namespace ChasingLight
{
    public class GateView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Sprites")] [SerializeField] private Sprite nonSolidGateSprite;
        [SerializeField] private Sprite solidGateSprite;

        [Header("Fallback Colors (khi không có sprite)")] [SerializeField]
        private Color closedColor = new Color(0.7f, 0.2f, 0.2f, 1f); // Đỏ sẫm

        [SerializeField] private Color openColor = new Color(0.7f, 0.2f, 0.2f, 0.25f); // Bán trong suốt khi mở

        public int Id { get; private set; }
        public bool IsSolid { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(int id, bool initialSolid)
        {
            Id = id;
            SetOpened(initialSolid);
        }

        public void SetSprites(Sprite closed, Sprite opened)
        {
            nonSolidGateSprite = closed;
            solidGateSprite = opened;
            SetOpened(IsSolid);
        }

        public void SetOpened(bool isSolid)
        {
            IsSolid = isSolid;
            if (spriteRenderer is null) return;

            if (isSolid && solidGateSprite is not null)
            {
                spriteRenderer.sprite = solidGateSprite;
                spriteRenderer.color = Color.white;
            }
            else if (!isSolid && nonSolidGateSprite is not null)
            {
                spriteRenderer.sprite = nonSolidGateSprite;
                spriteRenderer.color = Color.white;
            }
            else
            {
                // Fallback màu nếu chưa gán sprite
                spriteRenderer.color = isSolid ? openColor : closedColor;
            }
        }
    }
}