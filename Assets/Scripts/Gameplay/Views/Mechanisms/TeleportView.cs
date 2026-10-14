using UnityEngine;

namespace ChasingLight.Gameplay.Views
{
    public class TeleportView : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Color enabledColor = new Color(0.75f, 0.2f, 0.95f, 1f); // Tím sáng
        [SerializeField] private Color disabledColor = new Color(0.4f, 0.3f, 0.45f, 0.5f); // Tím xỉn mờ

        public int Id { get; private set; }
        public bool IsEnabledPortal { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Initialize(int id, bool initialEnabled)
        {
            Id = id;
            SetEnabled(initialEnabled);
        }

        public void SetEnabled(bool isEnabled)
        {
            IsEnabledPortal = isEnabled;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isEnabled ? enabledColor : disabledColor;
            }
        }
    }
}
