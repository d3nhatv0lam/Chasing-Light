using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChasingLight
{
    /// <summary>
    /// Điều hướng và xử lý toàn bộ giao diện Login / Register / ForgotPassword.
    /// <para>Gắn script này lên <c>LoginCanvas</c> (hoặc root GameObject của login scene).</para>
    /// <para>Tham chiếu UI được wire trực tiếp qua Inspector theo cấu trúc Scene đã dựng.</para>
    /// </summary>
    public class LoginTabController : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────
        //  Service Reference
        // ─────────────────────────────────────────────────────────────────────

        [Header("Service")]
        [Tooltip("AuthService đã gắn trên GameObject trong Scene (hoặc cùng GameObject)")]
        [SerializeField] private AuthService _authService;

        // ─────────────────────────────────────────────────────────────────────
        //  Tab Buttons (TabContainer)
        // ─────────────────────────────────────────────────────────────────────

        [Header("Tab Buttons")]
        [SerializeField] private Button _logInTab;
        [SerializeField] private Button _signUpTab;
        [SerializeField] private Button _forgotPasswordTab;

        // ─────────────────────────────────────────────────────────────────────
        //  Panels
        // ─────────────────────────────────────────────────────────────────────

        [Header("Panels")]
        [SerializeField] private GameObject _logInPanel;
        [SerializeField] private GameObject _signUpPanel;
        [SerializeField] private GameObject _forgotPasswordPanel;

        // ─────────────────────────────────────────────────────────────────────
        //  Log-In Panel Fields
        // ─────────────────────────────────────────────────────────────────────

        [Header("Log-In Fields")]
        [SerializeField] private TMP_InputField _loginEmailInput;
        [SerializeField] private TMP_InputField _loginPasswordInput;
        [SerializeField] private Button         _showPasswordButton;
        [SerializeField] private Button         _loginButton;
        [SerializeField] private Button         _googleLoginButton;
        [SerializeField] private Button         _switchToRegisterButton;
        [SerializeField] private TMP_Text       _loginMessageText;

        // ─────────────────────────────────────────────────────────────────────
        //  Sign-Up Panel Fields
        // ─────────────────────────────────────────────────────────────────────

        [Header("Sign-Up Fields")]
        [SerializeField] private TMP_InputField _regEmailInput;
        [SerializeField] private TMP_InputField _regPasswordInput;
        [SerializeField] private Button         _regShowPasswordButton;
        [SerializeField] private TMP_InputField _regConfirmPasswordInput;
        [SerializeField] private Button         _regShowConfirmPasswordButton;
        [SerializeField] private Button         _registerButton;
        [SerializeField] private Button         _googleRegisterButton;
        [SerializeField] private Button         _switchToLoginButton;
        [SerializeField] private TMP_Text       _regMessageText;

        // ─────────────────────────────────────────────────────────────────────
        //  Forgot-Password Panel Fields
        // ─────────────────────────────────────────────────────────────────────

        [Header("Forgot-Password Fields")]
        [SerializeField] private TMP_InputField _fpEmailInput;
        [SerializeField] private Button         _sendResetButton;
        [SerializeField] private Button         _backToLoginButton;
        [SerializeField] private TMP_Text       _fpMessageText;

        // ─────────────────────────────────────────────────────────────────────
        //  Tab Color Config (optional – highlight active tab)
        // ─────────────────────────────────────────────────────────────────────

        [Header("Tab Highlight (tuỳ chọn)")]
        [SerializeField] private Color _activeTabColor   = new Color(0.2f, 0.6f, 1f, 1f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        // ─────────────────────────────────────────────────────────────────────
        //  Scene to load after login (tuỳ chọn)
        // ─────────────────────────────────────────────────────────────────────

        [Header("Navigation")]
        [Tooltip("Tên Scene sẽ load sau khi đăng nhập thành công. Để trống nếu chưa cần.")]
        [SerializeField] private string _nextSceneName = "MainMenu";

        // ─────────────────────────────────────────────────────────────────────
        //  Private state
        // ─────────────────────────────────────────────────────────────────────

        private bool _loginPasswordVisible      = false;
        private bool _regPasswordVisible        = false;
        private bool _regConfirmPasswordVisible = false;

        // ─────────────────────────────────────────────────────────────────────
        //  Lifecycle
        // ─────────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // Tự tìm AuthService nếu không gán tay
            if (_authService == null)
                _authService = GetComponentInChildren<AuthService>()
                               ?? FindFirstObjectByType<AuthService>();

            if (_authService == null)
                Debug.LogError("[LoginTabController] Không tìm thấy AuthService trong Scene!", this);
        }

        private void OnEnable()
        {
            SubscribeAuthEvents();
            BindButtons();
            ShowTab(AuthTab.LogIn);
        }

        private void OnDisable()
        {
            UnsubscribeAuthEvents();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Tab Navigation
        // ─────────────────────────────────────────────────────────────────────

        private enum AuthTab { LogIn, SignUp, ForgotPassword }

        private void ShowTab(AuthTab tab)
        {
            bool isLogIn = tab == AuthTab.LogIn;
            bool isSignUp = tab == AuthTab.SignUp;
            bool isFP    = tab == AuthTab.ForgotPassword;

            SetActive(_logInPanel,          isLogIn);
            SetActive(_signUpPanel,         isSignUp);
            SetActive(_forgotPasswordPanel, isFP);

            SetTabHighlight(_logInTab,          isLogIn);
            SetTabHighlight(_signUpTab,         isSignUp);
            SetTabHighlight(_forgotPasswordTab, isFP);

            // Xoá message cũ khi đổi tab
            SetText(_loginMessageText, "");
            SetText(_regMessageText,   "");
            SetText(_fpMessageText,    "");
        }

        private void SetTabHighlight(Button btn, bool active)
        {
            if (btn == null) return;
            var img = btn.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                img.color = active ? _activeTabColor : _inactiveTabColor;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Button Binding
        // ─────────────────────────────────────────────────────────────────────

        private void BindButtons()
        {
            // ── Tab bar ──
            AddListener(_logInTab,          () => ShowTab(AuthTab.LogIn));
            AddListener(_signUpTab,         () => ShowTab(AuthTab.SignUp));
            AddListener(_forgotPasswordTab, () => ShowTab(AuthTab.ForgotPassword));

            // ── Log-In panel ──
            AddListener(_loginButton,            OnLoginClicked);
            AddListener(_showPasswordButton,     OnToggleLoginPassword);
            AddListener(_switchToRegisterButton, () => ShowTab(AuthTab.SignUp));
            AddListener(_googleLoginButton,      OnGoogleAuthClicked);

            // ── Sign-Up panel ──
            AddListener(_registerButton,              OnRegisterClicked);
            AddListener(_regShowPasswordButton,       OnToggleRegPassword);
            AddListener(_regShowConfirmPasswordButton, OnToggleRegConfirmPassword);
            AddListener(_switchToLoginButton,         () => ShowTab(AuthTab.LogIn));
            AddListener(_googleRegisterButton,        OnGoogleAuthClicked);

            // ── Forgot-Password panel ──
            AddListener(_sendResetButton,  OnSendResetClicked);
            AddListener(_backToLoginButton, () => ShowTab(AuthTab.LogIn));
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Log-In Handlers
        // ─────────────────────────────────────────────────────────────────────

        private void OnLoginClicked()
        {
            string email = _loginEmailInput?.text?.Trim() ?? "";
            string pass  = _loginPasswordInput?.text ?? "";

            if (!IsValidEmail(email))
            {
                ShowMessage(_loginMessageText, "Vui lòng nhập email hợp lệ.", MessageType.Error);
                return;
            }

            if (string.IsNullOrEmpty(pass))
            {
                ShowMessage(_loginMessageText, "Vui lòng nhập mật khẩu.", MessageType.Error);
                return;
            }

            SetInteractable(false);
            ShowMessage(_loginMessageText, "Đang đăng nhập…", MessageType.Info);
            _authService.Login(email, pass);
        }

        private void OnToggleLoginPassword()
        {
            if (_loginPasswordInput == null) return;
            _loginPasswordVisible = !_loginPasswordVisible;
            _loginPasswordInput.contentType = _loginPasswordVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            _loginPasswordInput.ForceLabelUpdate();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Sign-Up Handlers
        // ─────────────────────────────────────────────────────────────────────

        private void OnRegisterClicked()
        {
            string email   = _regEmailInput?.text?.Trim() ?? "";
            string pass    = _regPasswordInput?.text ?? "";
            string confirm = _regConfirmPasswordInput?.text ?? "";

            if (!IsValidEmail(email))
            {
                ShowMessage(_regMessageText, "Vui lòng nhập email hợp lệ.", MessageType.Error);
                return;
            }

            if (pass.Length < 6)
            {
                ShowMessage(_regMessageText, "Mật khẩu phải có ít nhất 6 ký tự.", MessageType.Error);
                return;
            }

            if (pass != confirm)
            {
                ShowMessage(_regMessageText, "Xác nhận mật khẩu không khớp.", MessageType.Error);
                return;
            }

            SetInteractable(false);
            ShowMessage(_regMessageText, "Đang đăng ký…", MessageType.Info);
            _authService.Register(email, pass);
        }

        private void OnToggleRegPassword()
        {
            if (_regPasswordInput == null) return;
            _regPasswordVisible = !_regPasswordVisible;
            _regPasswordInput.contentType = _regPasswordVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            _regPasswordInput.ForceLabelUpdate();
        }

        private void OnToggleRegConfirmPassword()
        {
            if (_regConfirmPasswordInput == null) return;
            _regConfirmPasswordVisible = !_regConfirmPasswordVisible;
            _regConfirmPasswordInput.contentType = _regConfirmPasswordVisible
                ? TMP_InputField.ContentType.Standard
                : TMP_InputField.ContentType.Password;
            _regConfirmPasswordInput.ForceLabelUpdate();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Google OAuth Handler (dùng chung cho cả Login và Register)
        // ─────────────────────────────────────────────────────────────────────

        private void OnGoogleAuthClicked()
        {
            if (_authService == null) return;

            // Hiển thị thông báo trên cả hai panel (người dùng đang ở panel nào thì thấy)
            ShowMessage(_loginMessageText, "Đang mở Google Sign-In…", MessageType.Info);
            ShowMessage(_regMessageText,   "Đang mở Google Sign-In…", MessageType.Info);

            SetInteractable(false);
            _authService.LoginWithGoogle();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Forgot-Password Handlers
        // ─────────────────────────────────────────────────────────────────────

        private void OnSendResetClicked()
        {
            string email = _fpEmailInput?.text?.Trim() ?? "";

            if (!IsValidEmail(email))
            {
                ShowMessage(_fpMessageText, "Vui lòng nhập email hợp lệ.", MessageType.Error);
                return;
            }

            SetInteractable(false);
            ShowMessage(_fpMessageText, "Đang gửi email đặt lại mật khẩu…", MessageType.Info);
            _authService.SendResetPassword(email);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  AuthService Event Subscriptions
        // ─────────────────────────────────────────────────────────────────────

        private void SubscribeAuthEvents()
        {
            if (_authService == null) return;

            _authService.OnLoginSuccess         += HandleLoginSuccess;
            _authService.OnLoginFailed          += HandleLoginFailed;
            _authService.OnRegisterSuccess      += HandleRegisterSuccess;
            _authService.OnRegisterFailed       += HandleRegisterFailed;
            _authService.OnResetPasswordSuccess += HandleResetSuccess;
            _authService.OnResetPasswordFailed  += HandleResetFailed;
            _authService.OnGoogleLoginSuccess   += HandleGoogleLoginSuccess;
            _authService.OnGoogleLoginFailed    += HandleGoogleLoginFailed;
        }

        private void UnsubscribeAuthEvents()
        {
            if (_authService == null) return;

            _authService.OnLoginSuccess         -= HandleLoginSuccess;
            _authService.OnLoginFailed          -= HandleLoginFailed;
            _authService.OnRegisterSuccess      -= HandleRegisterSuccess;
            _authService.OnRegisterFailed       -= HandleRegisterFailed;
            _authService.OnResetPasswordSuccess -= HandleResetSuccess;
            _authService.OnResetPasswordFailed  -= HandleResetFailed;
            _authService.OnGoogleLoginSuccess   -= HandleGoogleLoginSuccess;
            _authService.OnGoogleLoginFailed    -= HandleGoogleLoginFailed;
        }

        // ── Success / Failure Callbacks ────────────────────────────────────

        private void HandleLoginSuccess(string token)
        {
            SetInteractable(true);
            ShowMessage(_loginMessageText, "Đăng nhập thành công!", MessageType.Success);
            StartCoroutine(LoadNextSceneDelay(1.2f));
        }

        private void HandleLoginFailed(string error)
        {
            SetInteractable(true);
            ShowMessage(_loginMessageText, error, MessageType.Error);
        }

        private void HandleRegisterSuccess(string msg)
        {
            SetInteractable(true);
            ShowMessage(_regMessageText,
                "Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.",
                MessageType.Success);
        }

        private void HandleRegisterFailed(string error)
        {
            SetInteractable(true);
            ShowMessage(_regMessageText, error, MessageType.Error);
        }

        private void HandleResetSuccess()
        {
            SetInteractable(true);
            ShowMessage(_fpMessageText,
                "Email đặt lại mật khẩu đã được gửi. Vui lòng kiểm tra hộp thư.",
                MessageType.Success);
        }

        private void HandleResetFailed(string error)
        {
            SetInteractable(true);
            ShowMessage(_fpMessageText, error, MessageType.Error);
        }

        private void HandleGoogleLoginSuccess(string token)
        {
            SetInteractable(true);
            ShowMessage(_loginMessageText, "Đăng nhập Google thành công!", MessageType.Success);
            ShowMessage(_regMessageText,   "Đăng ký Google thành công!",   MessageType.Success);
            StartCoroutine(LoadNextSceneDelay(1.2f));
        }

        private void HandleGoogleLoginFailed(string error)
        {
            SetInteractable(true);
            ShowMessage(_loginMessageText, error, MessageType.Error);
            ShowMessage(_regMessageText,   error, MessageType.Error);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Scene Loading
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator LoadNextSceneDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (!string.IsNullOrEmpty(_nextSceneName))
                UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UI Helpers
        // ─────────────────────────────────────────────────────────────────────

        private enum MessageType { Info, Success, Error }

        private void ShowMessage(TMP_Text label, string msg, MessageType type)
        {
            if (label == null) return;
            label.text = msg;
            label.color = type switch
            {
                MessageType.Success => new Color(0.18f, 0.72f, 0.34f),
                MessageType.Error   => new Color(0.92f, 0.25f, 0.25f),
                _                   => new Color(0.8f,  0.8f,  0.8f),
            };
        }

        /// <summary>Bật/tắt toàn bộ nút tương tác để tránh double-submit.</summary>
        private void SetInteractable(bool value)
        {
            SetButtonInteractable(_loginButton,    value);
            SetButtonInteractable(_registerButton, value);
            SetButtonInteractable(_sendResetButton, value);
            SetButtonInteractable(_googleLoginButton,   value);
            SetButtonInteractable(_googleRegisterButton, value);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Validation
        // ─────────────────────────────────────────────────────────────────────

        private static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Null-safe UI shortcuts
        // ─────────────────────────────────────────────────────────────────────

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null) label.text = text;
        }

        private static void AddListener(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveListener(action);
            btn.onClick.AddListener(action);
        }

        private static void SetButtonInteractable(Button btn, bool value)
        {
            if (btn != null) btn.interactable = value;
        }
    }
}
