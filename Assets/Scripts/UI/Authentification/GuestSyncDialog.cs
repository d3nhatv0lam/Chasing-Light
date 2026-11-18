using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChasingLight
{
    /// <summary>
    /// Dialog hiển thị khi người chơi có dữ liệu tiến trình Khách cũ
    /// và vừa đăng nhập/đăng ký tài khoản thành công.
    /// </summary>
    public class GuestSyncDialog : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Root panel của Dialog (chứa popup background, text và buttons)")]
        [SerializeField] private GameObject _dialogPanel;

        [Tooltip("Text hiển thị nội dung câu hỏi")]
        [SerializeField] private TMP_Text _messageText;

        [Tooltip("Nút Đồng ý lưu / đồng bộ tiến trình vào tài khoản")]
        [SerializeField] private Button _syncButton;

        [Tooltip("Nút Bỏ qua / Không lưu (khởi tạo tiến trình mới)")]
        [SerializeField] private Button _discardButton;

        private Action _onConfirmSync;
        private Action _onDiscard;

        private void Awake()
        {
            if (_dialogPanel != null)
            {
                _dialogPanel.SetActive(false);
            }

            if (_syncButton != null)
            {
                _syncButton.onClick.AddListener(HandleSyncClicked);
            }

            if (_discardButton != null)
            {
                _discardButton.onClick.AddListener(HandleDiscardClicked);
            }
        }

        private void OnDestroy()
        {
            if (_syncButton != null) _syncButton.onClick.RemoveListener(HandleSyncClicked);
            if (_discardButton != null) _discardButton.onClick.RemoveListener(HandleDiscardClicked);
        }

        /// <summary>
        /// Hiển thị popup hỏi người chơi có muốn đồng bộ tiến trình Guest hay không.
        /// </summary>
        public void Show(Action onConfirmSync, Action onDiscard, string message = null)
        {
            _onConfirmSync = onConfirmSync;
            _onDiscard = onDiscard;

            if (_messageText != null)
            {
                _messageText.text = message ?? "Previous local progress detected.\nWould you like to sync your progress to this account?";
            }

            if (_dialogPanel != null)
            {
                _dialogPanel.SetActive(true);
            }
            else
            {
                // Fallback: tạo một dialog tạm thời trên Canvas để không chặn luồng game
                CreateFallbackDialog();
            }
        }

        public void Hide()
        {
            if (_dialogPanel != null)
            {
                _dialogPanel.SetActive(false);
            }
        }

        private void HandleSyncClicked()
        {
            Hide();
            _onConfirmSync?.Invoke();
        }

        private void HandleDiscardClicked()
        {
            Hide();
            _onDiscard?.Invoke();
        }

        private void CreateFallbackDialog()
        {
            var canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[GuestSyncDialog] Không tìm thấy Canvas để hiển thị dialog. Mặc định đồng bộ.");
                HandleSyncClicked();
                return;
            }

            var dialogGo = new GameObject("RuntimeGuestSyncDialog", typeof(RectTransform), typeof(Image));
            dialogGo.transform.SetParent(canvas.transform, false);

            var rect = (RectTransform)dialogGo.transform;
            rect.anchorMin = new Vector2(0.2f, 0.3f);
            rect.anchorMax = new Vector2(0.8f, 0.7f);
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var bgImg = dialogGo.GetComponent<Image>();
            bgImg.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

            _dialogPanel = dialogGo;

            // Text
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGo.transform.SetParent(dialogGo.transform, false);
            var textRect = (RectTransform)textGo.transform;
            textRect.anchorMin = new Vector2(0.1f, 0.45f);
            textRect.anchorMax = new Vector2(0.9f, 0.9f);
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            var tmp = textGo.GetComponent<TextMeshProUGUI>();
            tmp.text = "Previous local progress detected.\nWould you like to sync your progress to this account?";
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 24;
            tmp.color = Color.white;
            _messageText = tmp;

            // Button Sync
            var btnSyncGo = new GameObject("BtnSync", typeof(RectTransform), typeof(Image), typeof(Button));
            btnSyncGo.transform.SetParent(dialogGo.transform, false);
            var btnSyncRect = (RectTransform)btnSyncGo.transform;
            btnSyncRect.anchorMin = new Vector2(0.1f, 0.15f);
            btnSyncRect.anchorMax = new Vector2(0.48f, 0.38f);
            btnSyncRect.offsetMin = btnSyncRect.offsetMax = Vector2.zero;
            btnSyncGo.GetComponent<Image>().color = new Color(0.2f, 0.7f, 0.3f);

            _syncButton = btnSyncGo.GetComponent<Button>();
            _syncButton.onClick.AddListener(HandleSyncClicked);
            CreateButtonText(btnSyncGo.transform, "Sync to Account");

            // Button Discard
            var btnDiscardGo = new GameObject("BtnDiscard", typeof(RectTransform), typeof(Image), typeof(Button));
            btnDiscardGo.transform.SetParent(dialogGo.transform, false);
            var btnDiscardRect = (RectTransform)btnDiscardGo.transform;
            btnDiscardRect.anchorMin = new Vector2(0.52f, 0.15f);
            btnDiscardRect.anchorMax = new Vector2(0.9f, 0.38f);
            btnDiscardRect.offsetMin = btnDiscardRect.offsetMax = Vector2.zero;
            btnDiscardGo.GetComponent<Image>().color = new Color(0.8f, 0.25f, 0.25f);

            _discardButton = btnDiscardGo.GetComponent<Button>();
            _discardButton.onClick.AddListener(HandleDiscardClicked);
            CreateButtonText(btnDiscardGo.transform, "Start Fresh (Discard)");
        }

        private static void CreateButtonText(Transform parent, string text)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = 20;
            tmp.color = Color.white;
        }
    }
}
