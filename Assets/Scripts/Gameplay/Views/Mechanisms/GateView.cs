using UnityEngine;

namespace ChasingLight.Gameplay.Views
{
    public class GateView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
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

        public void SetOpened(bool opened)
        {
            IsOpen = opened;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = opened ? openColor : closedColor;
            }
        }
    }
}
