using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Gắn vào bất kỳ GameObject nào có Button để tự động phát âm thanh Click và Hover.
    /// Hoạt động độc lập, không cần can thiệp vào Controller của UI đó.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class UIButtonAudio : MonoBehaviour, IPointerEnterHandler
    {
        [Header("Sound Settings")]
        [SerializeField] private SoundID clickSound = SoundID.UI_Click;
        [SerializeField] private SoundID hoverSound = SoundID.UI_Hover;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            if (_button != null && _button.interactable && clickSound != SoundID.None)
            {
                AudioManager.Instance?.PlayUI(clickSound);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_button != null && _button.interactable && hoverSound != SoundID.None)
            {
                AudioManager.Instance?.PlayUI(hoverSound);
            }
        }
    }
}
