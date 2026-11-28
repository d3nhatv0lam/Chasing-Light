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
                Debug.LogWarning("[GuestSyncDialog] _dialogPanel chưa được gán trong Inspector. Mặc định đồng bộ.");
                HandleSyncClicked();
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
    }
}
