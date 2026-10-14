using UnityEngine;

namespace ChasingLight.Gameplay.Views
{
    public class SwitchView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
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

        public void SetActivated(bool activated)
        {
            IsActivated = activated;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = activated ? onColor : offColor;
            }
        }
    }
}
