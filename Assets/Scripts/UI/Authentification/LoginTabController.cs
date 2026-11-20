using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace ChasingLight
{
    /// <summary>
    /// Điều hướng và xử lý toàn bộ giao diện Login / SignUp / ForgotPassword / EnterOtp / ResetPassword
    /// <para>Gắn script này lên <c>LoginCanvas</c> (hoặc root GameObject của login scene).</para>
    /// <para>Tham chiếu UI được wire trực tiếp qua Inspector theo cấu trúc Scene đã dựng.</para>
    /// </summary>
    public class LoginTabController : MonoBehaviour
    {
        public const int OtpLength = 8;

        // ─────────────────────────────────────────────────────────────────────
        //  Service Reference
        // ─────────────────────────────────────────────────────────────────────

        [Header("Service")]
        [Tooltip("AuthService đã gắn trên GameObject trong Scene (hoặc cùng GameObject)")]
        [SerializeField] private AuthService _authService;

        // ─────────────────────────────────────────────────────────────────────
        //  Panels
        // ─────────────────────────────────────────────────────────────────────

        [Header("Panels")]
        [SerializeField] private GameObject _logInPanel;
        [SerializeField] private GameObject _signUpPanel;
        [SerializeField] private GameObject _forgotPasswordPanel;
        [SerializeField] private GameObject _enterOtpPanel;
        [SerializeField] private GameObject _resetPasswordPanel;

        // ─────────────────────────────────────────────────────────────────────
        //  Log-In Panel Fields
        // ─────────────────────────────────────────────────────────────────────

        [Header("Log-In Fields")]
        [SerializeField] private TMP_InputField _loginEmailInput;
        [SerializeField] private TMP_InputField _loginPasswordInput;
        [SerializeField] private Button         _showPasswordButton;
        [SerializeField] private Button         _forgotPasswordButton;
        [SerializeField] private Button         _loginButton;
        [SerializeField] private Button         _googleLoginButton;
        [SerializeField] private Button         _switchToRegisterButton;
        [SerializeField] private Button         _playAsGuestButton;
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
        [SerializeField] private Button         _regPlayAsGuestButton;
        [SerializeField] private Button         _switchToLoginButton;
        [SerializeField] private TMP_Text       _regMessageText;

        // ─────────────────────────────────────────────────────────────────────
        //  Forgot-Password Panel Fields
        // ─────────────────────────────────────────────────────────────────────

        [Header("Forgot-Password Fields")]
        [SerializeField] private TMP_InputField _fpEmailInput;
        [SerializeField] private Button         _sendResetButton;
        [SerializeField] private Button         _fpPlayAsGuestButton;
        [SerializeField] private Button         _backToLoginButton;
        [SerializeField] private TMP_Text       _fpMessageText;

        // ─────────────────────────────────────────────────────────────────────
        //  Enter-OTP Panel Fields (8 chữ số)
        // ─────────────────────────────────────────────────────────────────────

        [Header("Enter-OTP Fields (8 Digits)")]
        [SerializeField] private TMP_InputField[] _otpDigitInputs = new TMP_InputField[OtpLength];
        [SerializeField] private Button           _otpVerifyButton;
        [SerializeField] private TMP_Text         _otpMessageText;

        // ─────────────────────────────────────────────────────────────────────
        //  Reset-Password Panel Fields
        // ─────────────────────────────────────────────────────────────────────

        [Header("Reset-Password Fields")]
        [SerializeField] private TMP_InputField _rpNewPasswordInput;
        [SerializeField] private Button         _rpShowNewPasswordButton;
        [SerializeField] private TMP_InputField _rpConfirmNewPasswordInput;
        [SerializeField] private Button         _rpShowConfirmNewPasswordButton;
        [SerializeField] private Button         _rpUpdatePasswordButton;
        [SerializeField] private TMP_Text       _rpMessageText;

        // ─────────────────────────────────────────────────────────────────────
        //  Password Visibility Icons
        // ─────────────────────────────────────────────────────────────────────

        [Header("Password Visibility Icons")]
        [Tooltip("Icon con mắt khi mật khẩu đang hiển thị (Show)")]
        [SerializeField] private Sprite _eyeShowSprite;
        [Tooltip("Icon con mắt khi mật khẩu đang bị ẩn (Un-show / Hide)")]
        [SerializeField] private Sprite _eyeHideSprite;

        // ─────────────────────────────────────────────────────────────────────
        //  Guest Mode & Sync
        // ─────────────────────────────────────────────────────────────────────

        [Header("Guest Mode & Sync")]
        [Tooltip("Dialog xác nhận đồng bộ tiến trình Guest khi đăng nhập tài khoản")]
        [SerializeField] private GuestSyncDialog _guestSyncDialog;

        // ─────────────────────────────────────────────────────────────────────
        //  Scene to load after login (tuỳ chọn)
        // ─────────────────────────────────────────────────────────────────────

        [Header("Navigation")]
        [Tooltip("Tên Scene sẽ load sau khi đăng nhập thành công. Để trống nếu chưa cần.")]
        [SerializeField] private string _nextSceneName = "MainMenu";

        // ─────────────────────────────────────────────────────────────────────
        //  Private state
        // ─────────────────────────────────────────────────────────────────────

        private bool   _loginPasswordVisible      = false;
        private bool   _regPasswordVisible        = false;
        private bool   _regConfirmPasswordVisible = false;
        private bool   _rpNewPasswordVisible      = false;
        private bool   _rpConfirmPasswordVisible  = false;
        private bool   _updatingOtpFields         = false;
        private string _pendingRecoveryEmail      = "";
        private string _pendingRecoveryToken      = "";

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

            if (_guestSyncDialog == null)
                _guestSyncDialog = GetComponentInChildren<GuestSyncDialog>(true)
                                   ?? FindFirstObjectByType<GuestSyncDialog>(FindObjectsInactive.Include);
        }

        private void Start()
        {
            AudioManager.Instance?.PlayBGM(SoundID.BGM_Main);
        }

        private void OnEnable()
        {
            SubscribeAuthEvents();
            BindButtons();
            BindOtpInputs();
            SyncAllPasswordVisibilityUI();
            ShowTab(AuthTab.LogIn, playSound: false);
        }

        private void OnDisable()
        {
            UnsubscribeAuthEvents();
            UnbindOtpInputs();
        }

        private void Update()
        {
            HandleOtpBackspaceNavigation();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Tab / Panel Navigation
        // ─────────────────────────────────────────────────────────────────────

        private enum AuthTab
        {
            LogIn,
            SignUp,
            ForgotPassword,
            EnterOtp,
            ResetPassword
        }

        private void ShowTab(AuthTab tab, bool playSound = true)
        {
            if (playSound)
            {
                AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            }

            bool isLogIn  = tab == AuthTab.LogIn;
            bool isSignUp = tab == AuthTab.SignUp;
            bool isFP     = tab == AuthTab.ForgotPassword;
            bool isOtp    = tab == AuthTab.EnterOtp;
            bool isRP     = tab == AuthTab.ResetPassword;

            SetActive(_logInPanel,          isLogIn);
            SetActive(_signUpPanel,         isSignUp);
            SetActive(_forgotPasswordPanel, isFP);
            SetActive(_enterOtpPanel,       isOtp);
            SetActive(_resetPasswordPanel,  isRP);

            // Xoá message cũ khi đổi panel
            SetText(_loginMessageText, "");
            SetText(_regMessageText,   "");
            SetText(_fpMessageText,    "");
            SetText(_otpMessageText,   "");
            SetText(_rpMessageText,    "");

            if (isOtp)
            {
                ClearOtpInputs();
                FocusOtpBox(0);
            }
            else if (isRP)
            {
                if (_rpNewPasswordInput != null) _rpNewPasswordInput.text = "";
                if (_rpConfirmNewPasswordInput != null) _rpConfirmNewPasswordInput.text = "";
                _rpNewPasswordVisible = false;
                _rpConfirmPasswordVisible = false;
                ApplyPasswordVisibility(_rpNewPasswordInput, _rpShowNewPasswordButton, _rpNewPasswordVisible);
                ApplyPasswordVisibility(_rpConfirmNewPasswordInput, _rpShowConfirmNewPasswordButton, _rpConfirmPasswordVisible);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Button & Input Binding
        // ─────────────────────────────────────────────────────────────────────

        private void BindButtons()
        {
            // ── Log-In panel ──
            AddListener(_loginButton,            OnLoginClicked);
            AddListener(_showPasswordButton,     OnToggleLoginPassword);
            AddListener(_forgotPasswordButton,   () => ShowTab(AuthTab.ForgotPassword));
            AddListener(_switchToRegisterButton, () => ShowTab(AuthTab.SignUp));
            AddListener(_googleLoginButton,      OnGoogleAuthClicked);
            AddListener(_playAsGuestButton,      OnPlayAsGuestClicked);

            // ── Sign-Up panel ──
            AddListener(_registerButton,              OnRegisterClicked);
            AddListener(_regShowPasswordButton,       OnToggleRegPassword);
            AddListener(_regShowConfirmPasswordButton, OnToggleRegConfirmPassword);
            AddListener(_switchToLoginButton,         () => ShowTab(AuthTab.LogIn));
            AddListener(_googleRegisterButton,        OnGoogleAuthClicked);
            AddListener(_regPlayAsGuestButton,        OnPlayAsGuestClicked);

            // ── Forgot-Password panel ──
            AddListener(_sendResetButton,  OnSendResetClicked);
            AddListener(_backToLoginButton, () => ShowTab(AuthTab.LogIn));
            AddListener(_fpPlayAsGuestButton, OnPlayAsGuestClicked);

            // ── Enter-OTP panel ──
            AddListener(_otpVerifyButton, OnVerifyOtpClicked);

            // ── Reset-Password panel ──
            AddListener(_rpShowNewPasswordButton,        OnToggleRpNewPassword);
            AddListener(_rpShowConfirmNewPasswordButton, OnToggleRpConfirmNewPassword);
            AddListener(_rpUpdatePasswordButton,         OnUpdatePasswordClicked);
        }

        private void BindOtpInputs()
        {
            if (_otpDigitInputs == null) return;

            for (int i = 0; i < _otpDigitInputs.Length; i++)
            {
                int index = i;
                var input = _otpDigitInputs[index];
                if (input == null) continue;

                input.characterLimit = 1;
                input.onValueChanged.RemoveAllListeners();
                input.onValueChanged.AddListener(val => OnOtpDigitValueChanged(index, val));
            }
        }

        private void UnbindOtpInputs()
        {
            if (_otpDigitInputs == null) return;

            for (int i = 0; i < _otpDigitInputs.Length; i++)
            {
                if (_otpDigitInputs[i] != null)
                    _otpDigitInputs[i].onValueChanged.RemoveAllListeners();
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Guest Mode Handler
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Cho phép người chơi vào thẳng game mà không cần đăng nhập.
        /// </summary>
        public void OnPlayAsGuestClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            SaveManager.Instance.InitializeForGuest();
            if (!string.IsNullOrEmpty(_nextSceneName))
                UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Log-In Handlers
        // ─────────────────────────────────────────────────────────────────────

        private void OnLoginClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            string email = _loginEmailInput?.text?.Trim() ?? "";
            string pass  = _loginPasswordInput?.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
            {
                ShowMessage(_loginMessageText, "Email and password are required.", MessageType.Error);
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowMessage(_loginMessageText, "Please enter a valid email address.", MessageType.Error);
                return;
            }

            SetInteractable(false);
            ShowMessage(_loginMessageText, "Logging in...", MessageType.Info);
            _authService.Login(email, pass);
        }

        private void OnToggleLoginPassword()
        {
            if (_loginPasswordInput == null) return;
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            _loginPasswordVisible = !_loginPasswordVisible;
            ApplyPasswordVisibility(_loginPasswordInput, _showPasswordButton, _loginPasswordVisible);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Sign-Up Handlers
        // ─────────────────────────────────────────────────────────────────────

        private void OnRegisterClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            string email   = _regEmailInput?.text?.Trim() ?? "";
            string pass    = _regPasswordInput?.text ?? "";
            string confirm = _regConfirmPasswordInput?.text ?? "";

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass) || string.IsNullOrEmpty(confirm))
            {
                ShowMessage(_regMessageText, "Please enter both email and password.", MessageType.Error);
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowMessage(_regMessageText, "Please enter a valid email address.", MessageType.Error);
                return;
            }

            if (pass.Length < 6)
            {
                ShowMessage(_regMessageText, "Password must be at least 6 characters.", MessageType.Error);
                return;
            }

            if (pass != confirm)
            {
                ShowMessage(_regMessageText, "Passwords do not match.", MessageType.Error);
                return;
            }

            SetInteractable(false);
            ShowMessage(_regMessageText, "Creating account...", MessageType.Info);
            _authService.Register(email, pass);
        }

        private void OnToggleRegPassword()
        {
            if (_regPasswordInput == null) return;
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            _regPasswordVisible = !_regPasswordVisible;
            ApplyPasswordVisibility(_regPasswordInput, _regShowPasswordButton, _regPasswordVisible);
        }

        private void OnToggleRegConfirmPassword()
        {
            if (_regConfirmPasswordInput == null) return;
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            _regConfirmPasswordVisible = !_regConfirmPasswordVisible;
            ApplyPasswordVisibility(_regConfirmPasswordInput, _regShowConfirmPasswordButton, _regConfirmPasswordVisible);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Google OAuth Handler (dùng chung cho cả Login và Register)
        // ─────────────────────────────────────────────────────────────────────

        private void OnGoogleAuthClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            if (_authService == null) return;

            // Hiển thị thông báo trên cả hai panel (người dùng đang ở panel nào thì thấy)
            ShowMessage(_loginMessageText, "Opening Google Sign-In...", MessageType.Info);
            ShowMessage(_regMessageText,   "Opening Google Sign-In...", MessageType.Info);

            SetInteractable(false);
            _authService.LoginWithGoogle();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Forgot-Password Handlers
        // ─────────────────────────────────────────────────────────────────────

        private void OnSendResetClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            string email = _fpEmailInput?.text?.Trim() ?? "";

            if (string.IsNullOrEmpty(email))
            {
                ShowMessage(_fpMessageText, "Please enter your email address.", MessageType.Error);
                return;
            }

            if (!IsValidEmail(email))
            {
                ShowMessage(_fpMessageText, "Please enter a valid email address.", MessageType.Error);
                return;
            }

            _pendingRecoveryEmail = email;
            _pendingRecoveryToken = "";
            SetInteractable(false);
            ShowMessage(_fpMessageText, "Sending OTP to email...", MessageType.Info);
            _authService.SendResetPassword(email);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Enter-OTP (8 Digits) Handlers
        // ─────────────────────────────────────────────────────────────────────

        private void OnOtpDigitValueChanged(int index, string value)
        {
            if (_updatingOtpFields || _otpDigitInputs == null) return;

            // Hỗ trợ paste nhiều số cùng lúc
            if (!string.IsNullOrEmpty(value) && value.Length > 1)
            {
                _updatingOtpFields = true;
                int cursor = index;
                foreach (char c in value)
                {
                    if (!char.IsDigit(c)) continue;
                    if (cursor >= _otpDigitInputs.Length) break;
                    if (_otpDigitInputs[cursor] != null)
                        _otpDigitInputs[cursor].text = c.ToString();
                    cursor++;
                }
                _updatingOtpFields = false;
                FocusOtpBox(Mathf.Min(cursor, _otpDigitInputs.Length - 1));
                return;
            }

            // Tự động nhảy sang ô tiếp theo khi nhập 1 chữ số
            if (!string.IsNullOrEmpty(value) && index + 1 < _otpDigitInputs.Length)
            {
                FocusOtpBox(index + 1);
            }
        }

        private void HandleOtpBackspaceNavigation()
        {
            if (_enterOtpPanel == null || !_enterOtpPanel.activeInHierarchy) return;
            if (_otpDigitInputs == null || EventSystem.current == null) return;

            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.backspaceKey.wasPressedThisFrame) return;

            GameObject selected = EventSystem.current.currentSelectedGameObject;
            if (selected == null) return;

            for (int i = 1; i < _otpDigitInputs.Length; i++)
            {
                var input = _otpDigitInputs[i];
                if (input != null && input.gameObject == selected && string.IsNullOrEmpty(input.text))
                {
                    if (_otpDigitInputs[i - 1] != null)
                        _otpDigitInputs[i - 1].text = "";
                    FocusOtpBox(i - 1);
                    break;
                }
            }
        }

        private void FocusOtpBox(int index)
        {
            if (_otpDigitInputs == null || index < 0 || index >= _otpDigitInputs.Length) return;
            var target = _otpDigitInputs[index];
            if (target == null) return;

            target.Select();
            target.ActivateInputField();
        }

        private void ClearOtpInputs()
        {
            if (_otpDigitInputs == null) return;
            _updatingOtpFields = true;
            for (int i = 0; i < _otpDigitInputs.Length; i++)
            {
                if (_otpDigitInputs[i] != null)
                    _otpDigitInputs[i].text = "";
            }
            _updatingOtpFields = false;
        }

        private string GetEnteredOtpCode()
        {
            if (_otpDigitInputs == null) return "";
            var sb = new StringBuilder(OtpLength);
            for (int i = 0; i < _otpDigitInputs.Length; i++)
            {
                string digit = _otpDigitInputs[i]?.text?.Trim() ?? "";
                sb.Append(digit);
            }
            return sb.ToString();
        }

        private void OnVerifyOtpClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            string otp = GetEnteredOtpCode();

            if (otp.Length < OtpLength)
            {
                ShowMessage(_otpMessageText, $"Please enter all {OtpLength} digits of the OTP code.", MessageType.Error);
                return;
            }

            SetInteractable(false);
            ShowMessage(_otpMessageText, "Verifying OTP...", MessageType.Info);
            _authService.VerifyOtp(_pendingRecoveryEmail, otp);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Reset-Password Handlers
        // ─────────────────────────────────────────────────────────────────────

        private void OnUpdatePasswordClicked()
        {
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            string newPass     = _rpNewPasswordInput?.text ?? "";
            string confirmPass = _rpConfirmNewPasswordInput?.text ?? "";

            if (string.IsNullOrEmpty(newPass) || string.IsNullOrEmpty(confirmPass))
            {
                ShowMessage(_rpMessageText, "Please enter and confirm your new password.", MessageType.Error);
                return;
            }

            if (newPass.Length < 6)
            {
                ShowMessage(_rpMessageText, "New password must be at least 6 characters.", MessageType.Error);
                return;
            }

            if (newPass != confirmPass)
            {
                ShowMessage(_rpMessageText, "Passwords do not match.", MessageType.Error);
                return;
            }

            SetInteractable(false);
            ShowMessage(_rpMessageText, "Updating password...", MessageType.Info);
            _authService.UpdatePassword(newPass, _pendingRecoveryToken);
        }

        private void OnToggleRpNewPassword()
        {
            if (_rpNewPasswordInput == null) return;
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            _rpNewPasswordVisible = !_rpNewPasswordVisible;
            ApplyPasswordVisibility(_rpNewPasswordInput, _rpShowNewPasswordButton, _rpNewPasswordVisible);
        }

        private void OnToggleRpConfirmNewPassword()
        {
            if (_rpConfirmNewPasswordInput == null) return;
            AudioManager.Instance?.PlayUI(SoundID.UI_Click);
            _rpConfirmPasswordVisible = !_rpConfirmPasswordVisible;
            ApplyPasswordVisibility(_rpConfirmNewPasswordInput, _rpShowConfirmNewPasswordButton, _rpConfirmPasswordVisible);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Password Visibility Helper
        // ─────────────────────────────────────────────────────────────────────

        private void SyncAllPasswordVisibilityUI()
        {
            ApplyPasswordVisibility(_loginPasswordInput,        _showPasswordButton,             _loginPasswordVisible);
            ApplyPasswordVisibility(_regPasswordInput,          _regShowPasswordButton,          _regPasswordVisible);
            ApplyPasswordVisibility(_regConfirmPasswordInput,   _regShowConfirmPasswordButton,   _regConfirmPasswordVisible);
            ApplyPasswordVisibility(_rpNewPasswordInput,        _rpShowNewPasswordButton,        _rpNewPasswordVisible);
            ApplyPasswordVisibility(_rpConfirmNewPasswordInput, _rpShowConfirmNewPasswordButton, _rpConfirmPasswordVisible);
        }

        private void ApplyPasswordVisibility(TMP_InputField inputField, Button toggleButton, bool isVisible)
        {
            if (inputField != null)
            {
                inputField.contentType = isVisible
                    ? TMP_InputField.ContentType.Standard
                    : TMP_InputField.ContentType.Password;
                inputField.ForceLabelUpdate();
            }

            if (toggleButton != null)
            {
                Image iconImage = toggleButton.image != null
                    ? toggleButton.image
                    : toggleButton.GetComponent<Image>();

                if (iconImage != null)
                {
                    Sprite targetSprite = isVisible ? _eyeShowSprite : _eyeHideSprite;
                    if (targetSprite != null)
                    {
                        iconImage.sprite = targetSprite;
                    }
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  AuthService Event Subscriptions
        // ─────────────────────────────────────────────────────────────────────

        private void SubscribeAuthEvents()
        {
            if (_authService == null) return;

            _authService.OnLoginSuccess          += HandleLoginSuccess;
            _authService.OnLoginFailed           += HandleLoginFailed;
            _authService.OnRegisterSuccess       += HandleRegisterSuccess;
            _authService.OnRegisterFailed        += HandleRegisterFailed;
            _authService.OnResetPasswordSuccess  += HandleResetSuccess;
            _authService.OnResetPasswordFailed   += HandleResetFailed;
            _authService.OnVerifyOtpSuccess      += HandleVerifyOtpSuccess;
            _authService.OnVerifyOtpFailed       += HandleVerifyOtpFailed;
            _authService.OnUpdatePasswordSuccess += HandleUpdatePasswordSuccess;
            _authService.OnUpdatePasswordFailed  += HandleUpdatePasswordFailed;
            _authService.OnGoogleLoginSuccess    += HandleGoogleLoginSuccess;
            _authService.OnGoogleLoginFailed     += HandleGoogleLoginFailed;
        }

        private void UnsubscribeAuthEvents()
        {
            if (_authService == null) return;

            _authService.OnLoginSuccess          -= HandleLoginSuccess;
            _authService.OnLoginFailed           -= HandleLoginFailed;
            _authService.OnRegisterSuccess       -= HandleRegisterSuccess;
            _authService.OnRegisterFailed        -= HandleRegisterFailed;
            _authService.OnResetPasswordSuccess  -= HandleResetSuccess;
            _authService.OnResetPasswordFailed   -= HandleResetFailed;
            _authService.OnVerifyOtpSuccess      -= HandleVerifyOtpSuccess;
            _authService.OnVerifyOtpFailed       -= HandleVerifyOtpFailed;
            _authService.OnUpdatePasswordSuccess -= HandleUpdatePasswordSuccess;
            _authService.OnUpdatePasswordFailed  -= HandleUpdatePasswordFailed;
            _authService.OnGoogleLoginSuccess    -= HandleGoogleLoginSuccess;
            _authService.OnGoogleLoginFailed     -= HandleGoogleLoginFailed;
        }

        // ── Success / Failure Callbacks ────────────────────────────────────

        private void HandleLoginSuccess(string token)
        {
            SetInteractable(true);
            ShowMessage(_loginMessageText, "Login successful!", MessageType.Success);
            HandlePostAuthProgression(token);
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
                "Registration successful! Please check your email to verify your account.",
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
                "OTP code has been sent to your email.",
                MessageType.Success);

            if (_enterOtpPanel != null)
            {
                ShowTab(AuthTab.EnterOtp);
            }
        }

        private void HandleResetFailed(string error)
        {
            SetInteractable(true);
            ShowMessage(_fpMessageText, error, MessageType.Error);
        }

        private void HandleVerifyOtpSuccess(string recoveryToken)
        {
            SetInteractable(true);
            _pendingRecoveryToken = recoveryToken;
            ShowMessage(_otpMessageText, "OTP verified successfully!", MessageType.Success);

            if (_resetPasswordPanel != null)
            {
                ShowTab(AuthTab.ResetPassword);
            }
        }

        private void HandleVerifyOtpFailed(string error)
        {
            SetInteractable(true);
            ShowMessage(_otpMessageText, error, MessageType.Error);
        }

        private void HandleUpdatePasswordSuccess()
        {
            SetInteractable(true);
            _pendingRecoveryToken = "";
            ShowMessage(_rpMessageText, "Password updated successfully! Returning to login...", MessageType.Success);
            StartCoroutine(ReturnToLoginDelay(1.0f));
        }

        private void HandleUpdatePasswordFailed(string error)
        {
            SetInteractable(true);
            ShowMessage(_rpMessageText, error, MessageType.Error);
        }

        private void HandleGoogleLoginSuccess(string token)
        {
            SetInteractable(true);
            ShowMessage(_loginMessageText, "Google Sign-In successful!", MessageType.Success);
            ShowMessage(_regMessageText,   "Google Sign-In successful!", MessageType.Success);
            HandlePostAuthProgression(token);
        }

        private void HandlePostAuthProgression(string token)
        {
            if (SaveManager.Instance.HasGuestProgress())
            {
                if (_guestSyncDialog == null)
                    _guestSyncDialog = GetComponentInChildren<GuestSyncDialog>(true)
                                       ?? FindFirstObjectByType<GuestSyncDialog>(FindObjectsInactive.Include)
                                       ?? gameObject.AddComponent<GuestSyncDialog>();

                _guestSyncDialog.Show(
                    onConfirmSync: () =>
                    {
                        ShowMessage(_loginMessageText, "Syncing progress to account...", MessageType.Info);
                        ShowMessage(_regMessageText,   "Syncing progress to account...", MessageType.Info);
                        SaveManager.Instance.SyncGuestProgressToAccount(token, _authService.CurrentUserId, (success, msg) =>
                        {
                            if (success)
                            {
                                ShowMessage(_loginMessageText, "Sync successful!", MessageType.Success);
                                ShowMessage(_regMessageText,   "Sync successful!", MessageType.Success);
                            }
                            StartCoroutine(LoadNextSceneDelay(0.8f));
                        });
                    },
                    onDiscard: () =>
                    {
                        SaveManager.Instance.DiscardGuestProgress();
                        SaveManager.Instance.FetchAndApplyCloudRecords(token, _authService.CurrentUserId, (success, msg) =>
                        {
                            StartCoroutine(LoadNextSceneDelay(0.5f));
                        });
                    }
                );
            }
            else
            {
                SaveManager.Instance.FetchAndApplyCloudRecords(token, _authService.CurrentUserId, (success, msg) =>
                {
                    StartCoroutine(LoadNextSceneDelay(1.0f));
                });
            }
        }

        private void HandleGoogleLoginFailed(string error)
        {
            SetInteractable(true);
            ShowMessage(_loginMessageText, error, MessageType.Error);
            ShowMessage(_regMessageText,   error, MessageType.Error);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Scene / Tab Delays
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerator LoadNextSceneDelay(float delay)
        {
            yield return new WaitForSeconds(delay);

            if (!string.IsNullOrEmpty(_nextSceneName))
                UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
        }

        private IEnumerator ReturnToLoginDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ShowTab(AuthTab.LogIn);

            if (_loginEmailInput != null && !string.IsNullOrEmpty(_pendingRecoveryEmail))
                _loginEmailInput.text = _pendingRecoveryEmail;
            if (_loginPasswordInput != null)
                _loginPasswordInput.text = "";

            ShowMessage(_loginMessageText, "Password updated successfully! Please log in.", MessageType.Success);
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
            SetButtonInteractable(_loginButton,            value);
            SetButtonInteractable(_registerButton,         value);
            SetButtonInteractable(_sendResetButton,        value);
            SetButtonInteractable(_otpVerifyButton,        value);
            SetButtonInteractable(_rpUpdatePasswordButton, value);
            SetButtonInteractable(_googleLoginButton,      value);
            SetButtonInteractable(_googleRegisterButton,   value);
            SetButtonInteractable(_playAsGuestButton,      value);
            SetButtonInteractable(_regPlayAsGuestButton,   value);
            SetButtonInteractable(_fpPlayAsGuestButton,    value);
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
