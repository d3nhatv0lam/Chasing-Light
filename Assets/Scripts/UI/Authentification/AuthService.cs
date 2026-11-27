using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Business-logic layer cho Authentication.
    /// <para>
    /// Chịu trách nhiệm:
    /// <list type="bullet">
    ///   <item>Gọi <see cref="AuthApiClient"/> thông qua coroutine.</item>
    ///   <item>Lưu / xoá JWT token trong <see cref="PlayerPrefs"/>.</item>
    ///   <item>Cung cấp trạng thái đăng nhập hiện tại qua <see cref="IsLoggedIn"/>.</item>
    ///   <item>Điều phối Google OAuth Local Browser Flow (PC / Editor) qua
    ///         <see cref="GoogleOAuthLocalListener"/>.</item>
    /// </list>
    /// </para>
    /// </summary>
    public class AuthService : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private AuthConfig _config;

        public AuthConfig Config => _config;

        // ── Public state ──────────────────────────────────────────────────────

        /// <summary>Token JWT hiện tại (null nếu chưa đăng nhập).</summary>
        public string CurrentToken  { get; private set; }

        /// <summary>Refresh token dùng để xin cấp lại accessToken khi hết hạn.</summary>
        public string CurrentRefreshToken { get; private set; }

        /// <summary>Thời điểm hết hạn của accessToken (UTC Unix seconds, 0 nếu chưa xác định).</summary>
        public long TokenExpiresAtUnix { get; private set; }

        /// <summary>Email của user đang đăng nhập.</summary>
        public string CurrentEmail  { get; private set; }

        /// <summary>ID người dùng (GUID dạng string) từ backend.</summary>
        public string CurrentUserId { get; private set; }

        /// <summary>Token khôi phục mật khẩu sau khi xác thực OTP thành công.</summary>
        public string CurrentRecoveryToken { get; private set; }

        public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentToken);

        /// <summary>Có session đã lưu trong máy (accessToken hoặc refreshToken).</summary>
        public bool HasSavedSession => !string.IsNullOrEmpty(CurrentToken) || !string.IsNullOrEmpty(CurrentRefreshToken);

        /// <summary>Kiểm tra accessToken hiện tại đã hết hạn (hoặc sắp hết hạn trong <paramref name="bufferSeconds"/> giây) hay chưa.</summary>
        public bool IsAccessTokenExpired(int bufferSeconds = 60)
        {
            if (string.IsNullOrEmpty(CurrentToken))
                return true;

            if (TokenExpiresAtUnix <= 0)
                return true;

            long nowUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            return nowUnix >= (TokenExpiresAtUnix - bufferSeconds);
        }

        // ── Events ────────────────────────────────────────────────────────────

        public event Action<string> OnLoginSuccess;
        public event Action<string> OnLoginFailed;

        public event Action<string> OnRegisterSuccess;
        public event Action<string> OnRegisterFailed;

        public event Action         OnResetPasswordSuccess;
        public event Action<string> OnResetPasswordFailed;

        public event Action<string> OnVerifyOtpSuccess;
        public event Action<string> OnVerifyOtpFailed;

        public event Action         OnUpdatePasswordSuccess;
        public event Action<string> OnUpdatePasswordFailed;

        public event Action<string> OnGoogleLoginSuccess;
        public event Action<string> OnGoogleLoginFailed;

        // ── Google OAuth (PC / Editor) ────────────────────────────────────────

        private GoogleOAuthLocalListener _googleListener;

        // Bridge: ThreadPool callback → Unity main thread
        private volatile bool _oauthResultReady    = false;
        private string        _pendingOAuthIdToken  = null;
        private string        _pendingOAuthError    = null;
        private string        _pendingOAuthNonce    = null;

        private bool _isRefreshing = false;
        private readonly List<Action<bool, string>> _pendingRefreshCallbacks = new List<Action<bool, string>>();

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            LoadSavedSession();
        }

        private void OnDestroy()
        {
            _googleListener?.Cancel();
            _googleListener?.Dispose();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Đăng nhập bằng email + password.
        /// Kết quả được thông báo qua <see cref="OnLoginSuccess"/> / <see cref="OnLoginFailed"/>.
        /// </summary>
        public void Login(string email, string password)
        {
            if (!ValidateConfig()) return;
            CurrentEmail = email;
            StartCoroutine(AuthApiClient.SignIn(_config, email, password, OnSignInComplete));
        }

        /// <summary>
        /// Đăng ký tài khoản mới.
        /// </summary>
        public void Register(string email, string password)
        {
            if (!ValidateConfig()) return;
            CurrentEmail = email;
            StartCoroutine(AuthApiClient.SignUp(_config, email, password, OnSignUpComplete));
        }

        /// <summary>
        /// Bước 1 Quên mật khẩu: Gửi yêu cầu nhận mã OTP qua email (<c>POST /api/v1/auth/forgot-password</c>).
        /// </summary>
        public void SendResetPassword(string email)
        {
            if (!ValidateConfig()) return;
            CurrentEmail = email;
            CurrentRecoveryToken = null;
            StartCoroutine(AuthApiClient.ForgotPassword(_config, email, OnForgotPasswordComplete));
        }

        /// <summary>
        /// Bước 2 Quên mật khẩu: Xác thực mã OTP 8 chữ số để lấy <c>recoveryToken</c> (<c>POST /api/v1/auth/verify-recovery-otp</c>).
        /// </summary>
        public void VerifyOtp(string email, string otp)
        {
            if (!ValidateConfig()) return;
            string targetEmail = !string.IsNullOrEmpty(email) ? email : CurrentEmail;
            StartCoroutine(AuthApiClient.VerifyRecoveryOtp(_config, targetEmail, otp, OnVerifyOtpComplete));
        }

        /// <summary>
        /// Bước 3 Quên mật khẩu: Dùng <c>recoveryToken</c> để đặt mật khẩu mới (<c>POST /api/v1/auth/reset-password</c>).
        /// </summary>
        public void UpdatePassword(string newPassword, string recoveryToken = null)
        {
            if (!ValidateConfig()) return;
            string token = !string.IsNullOrEmpty(recoveryToken) ? recoveryToken : CurrentRecoveryToken;
            if (string.IsNullOrEmpty(token))
            {
                OnUpdatePasswordFailed?.Invoke("Recovery session is invalid or expired. Please verify OTP again.");
                return;
            }
            StartCoroutine(AuthApiClient.ResetPassword(_config, token, newPassword, OnUpdatePasswordComplete));
        }

        /// <summary>
        /// Khởi động Google OAuth Local Browser Flow (PC / Editor).<br/>
        /// Mở trình duyệt hệ thống → người dùng chọn tài khoản Google →
        /// trình duyệt redirect về <c>http://localhost:{port}/callback/</c> →
        /// <see cref="GoogleOAuthLocalListener"/> bắt <c>id_token</c> →
        /// gọi <c>POST /api/v1/auth/oauth</c> lên backend →
        /// phát <see cref="OnGoogleLoginSuccess"/> / <see cref="OnGoogleLoginFailed"/>.
        /// </summary>
        public void LoginWithGoogle()
        {
            if (!ValidateConfig()) return;

            // Hủy listener cũ nếu đang chạy
            _googleListener?.Cancel();
            _googleListener?.Dispose();
            _googleListener = null;

            if (string.IsNullOrWhiteSpace(_config.GoogleClientId))
            {
                OnGoogleLoginFailed?.Invoke(
                    "Google Client ID chưa được cấu hình trong AuthConfig. " +
                    "Vui lòng điền vào Inspector.");
                return;
            }

            _googleListener = new GoogleOAuthLocalListener(
                _config.GoogleClientId,
                _config.LocalCallbackPort);

            string nonce = _googleListener.Nonce;

            _oauthResultReady   = false;
            _pendingOAuthIdToken = null;
            _pendingOAuthError   = null;
            _pendingOAuthNonce   = null;

            _googleListener.StartAndOpenBrowser(
                onComplete: (idToken, error) =>
                {
                    // Đến từ ThreadPool – không được gọi Unity API ở đây
                    _pendingOAuthIdToken = idToken;
                    _pendingOAuthError   = error;
                    _pendingOAuthNonce   = nonce;
                    _oauthResultReady    = true;
                },
                timeoutSeconds: _config.OAuthTimeoutSeconds);

            StartCoroutine(WaitForOAuthResult());
        }

        /// <summary>
        /// Thử khôi phục phiên đăng nhập đã lưu khi mở game:
        /// - Nếu accessToken còn hạn: cho vào luôn.
        /// - Nếu accessToken đã/sắp hết hạn: tự động gọi <c>POST /api/v1/auth/refresh</c> để xin cặp token mới.
        /// - Nếu refreshToken hết hạn/bị thu hồi (401/403/400): xoá session và trả về false để hiện màn Login.
        /// - Nếu mất mạng (HTTP 0) nhưng máy vẫn có session đã lưu: cho phép tiếp tục chế độ offline.
        /// </summary>
        public void TryRestoreSession(Action<bool, string> onComplete)
        {
            if (!ValidateConfig() || !HasSavedSession)
            {
                onComplete?.Invoke(false, null);
                return;
            }

            if (!IsAccessTokenExpired())
            {
                onComplete?.Invoke(true, null);
                return;
            }

            if (string.IsNullOrEmpty(CurrentRefreshToken))
            {
                Logout();
                onComplete?.Invoke(false, "Session expired. Please log in again.");
                return;
            }

            RefreshSession(onComplete);
        }

        /// <summary>
        /// Gọi <c>POST /api/v1/auth/refresh</c> để đổi lấy <c>accessToken</c> và <c>refreshToken</c> mới.
        /// Gom nhiều lời gọi đồng thời vào chung 1 request để tránh lỗi token rotation của Supabase.
        /// </summary>
        public void RefreshSession(Action<bool, string> onComplete = null)
        {
            if (!ValidateConfig())
            {
                onComplete?.Invoke(false, "AuthConfig is not assigned.");
                return;
            }

            if (string.IsNullOrEmpty(CurrentRefreshToken))
            {
                Logout();
                onComplete?.Invoke(false, "Session expired. Please log in again.");
                return;
            }

            if (onComplete != null)
                _pendingRefreshCallbacks.Add(onComplete);

            if (_isRefreshing)
                return;

            _isRefreshing = true;
            StartCoroutine(AuthApiClient.RefreshToken(_config, CurrentRefreshToken, OnRefreshTokenComplete));
        }

        /// <summary>
        /// Đăng xuất: xoá token khỏi bộ nhớ và PlayerPrefs.
        /// </summary>
        public void Logout()
        {
            CurrentToken = null;
            CurrentRefreshToken = null;
            TokenExpiresAtUnix = 0;
            CurrentEmail = null;
            CurrentUserId = null;
            CurrentRecoveryToken = null;
            ClearSavedSession();
        }

        // ── Callbacks ─────────────────────────────────────────────────────────

        private void OnSignInComplete(ApiResult<AuthResponse> result)
        {
            if (result.IsSuccess && result.Data != null)
            {
                string email = !string.IsNullOrEmpty(result.Data.email) ? result.Data.email : CurrentEmail;
                StoreSession(result.Data.accessToken, result.Data.refreshToken, email, result.Data.userId, result.Data.expiresIn);
                OnLoginSuccess?.Invoke(result.Data.accessToken);
            }
            else
            {
                OnLoginFailed?.Invoke(result.ErrorMessage ?? "Login failed.");
            }
        }

        private void OnSignUpComplete(ApiResult<AuthResponse> result)
        {
            if (result.IsSuccess)
            {
                // Một số backend không trả token ngay sau đăng ký (cần verify email)
                if (result.Data != null && !string.IsNullOrEmpty(result.Data.accessToken))
                {
                    string email = !string.IsNullOrEmpty(result.Data.email) ? result.Data.email : CurrentEmail;
                    StoreSession(result.Data.accessToken, result.Data.refreshToken, email, result.Data.userId, result.Data.expiresIn);
                }

                OnRegisterSuccess?.Invoke(
                    result.Data?.accessToken ?? "Registration successful. Please check your email.");
            }
            else
            {
                OnRegisterFailed?.Invoke(result.ErrorMessage ?? "Registration failed.");
            }
        }

        private void OnForgotPasswordComplete(ApiResult<string> result)
        {
            if (result.IsSuccess)
                OnResetPasswordSuccess?.Invoke();
            else
                OnResetPasswordFailed?.Invoke(result.ErrorMessage ?? "Failed to send OTP email.");
        }

        private void OnVerifyOtpComplete(ApiResult<VerifyRecoveryOtpResponse> result)
        {
            if (result.IsSuccess && result.Data != null && !string.IsNullOrEmpty(result.Data.recoveryToken))
            {
                CurrentRecoveryToken = result.Data.recoveryToken;
                if (!string.IsNullOrEmpty(result.Data.email))
                    CurrentEmail = result.Data.email;

                OnVerifyOtpSuccess?.Invoke(CurrentRecoveryToken);
            }
            else
            {
                OnVerifyOtpFailed?.Invoke(result.ErrorMessage ?? "Invalid or expired OTP code.");
            }
        }

        private void OnUpdatePasswordComplete(ApiResult<AuthResponse> result)
        {
            if (result.IsSuccess)
            {
                CurrentRecoveryToken = null;
                OnUpdatePasswordSuccess?.Invoke();
            }
            else
            {
                OnUpdatePasswordFailed?.Invoke(result.ErrorMessage ?? "Failed to update password.");
            }
        }

        private void OnOAuthLoginComplete(ApiResult<AuthResponse> result)
        {
            if (result.IsSuccess && result.Data != null)
            {
                string email = !string.IsNullOrEmpty(result.Data.email) ? result.Data.email : (CurrentEmail ?? "");
                StoreSession(result.Data.accessToken, result.Data.refreshToken,
                             email, result.Data.userId, result.Data.expiresIn);
                OnGoogleLoginSuccess?.Invoke(result.Data.accessToken);
            }
            else
            {
                OnGoogleLoginFailed?.Invoke(result.ErrorMessage ?? "Google Sign-In failed.");
            }
        }

        private void OnRefreshTokenComplete(ApiResult<AuthResponse> result)
        {
            _isRefreshing = false;

            bool restored = false;
            string errorMessage = null;

            if (result.IsSuccess && result.Data != null && !string.IsNullOrEmpty(result.Data.accessToken))
            {
                string newRefresh = !string.IsNullOrEmpty(result.Data.refreshToken)
                    ? result.Data.refreshToken
                    : CurrentRefreshToken;
                string email = !string.IsNullOrEmpty(result.Data.email)
                    ? result.Data.email
                    : (CurrentEmail ?? "");
                string userId = !string.IsNullOrEmpty(result.Data.userId)
                    ? result.Data.userId
                    : CurrentUserId;

                StoreSession(result.Data.accessToken, newRefresh, email, userId, result.Data.expiresIn);
                restored = true;
            }
            else if (result.HttpStatusCode == 401 || result.HttpStatusCode == 403 || result.HttpStatusCode == 400)
            {
                Logout();
                errorMessage = "Session expired. Please log in again.";
            }
            else if (result.HttpStatusCode == 0 && !string.IsNullOrEmpty(CurrentUserId) && !string.IsNullOrEmpty(CurrentToken))
            {
                // Mất kết nối tới server: giữ nguyên session local và cho phép chơi offline
                Debug.LogWarning($"[AuthService] Không thể kết nối để refresh token ({result.ErrorMessage}). Tiếp tục ở chế độ offline.");
                restored = true;
            }
            else
            {
                errorMessage = result.ErrorMessage ?? "Failed to restore session.";
            }

            if (_pendingRefreshCallbacks.Count > 0)
            {
                var callbacks = new List<Action<bool, string>>(_pendingRefreshCallbacks);
                _pendingRefreshCallbacks.Clear();
                foreach (var cb in callbacks)
                {
                    cb?.Invoke(restored, errorMessage);
                }
            }
        }

        // ── Google OAuth Coroutine Bridge ─────────────────────────────────────

        /// <summary>
        /// Chờ cờ <see cref="_oauthResultReady"/> được set từ ThreadPool thread
        /// rồi mới gọi coroutine mạng trên Unity main thread.
        /// </summary>
        private IEnumerator WaitForOAuthResult()
        {
            float elapsed = 0f;
            float maxWait = _config.OAuthTimeoutSeconds + 5f;

            while (!_oauthResultReady && elapsed < maxWait)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!_oauthResultReady)
            {
                _googleListener?.Cancel();
                OnGoogleLoginFailed?.Invoke("Đăng nhập Google quá thời gian chờ (timeout).");
                yield break;
            }

            if (!string.IsNullOrEmpty(_pendingOAuthError))
            {
                OnGoogleLoginFailed?.Invoke(_pendingOAuthError);
                yield break;
            }

            if (string.IsNullOrEmpty(_pendingOAuthIdToken))
            {
                OnGoogleLoginFailed?.Invoke("Không nhận được id_token từ Google.");
                yield break;
            }

            string idToken = _pendingOAuthIdToken;
            string nonce   = _pendingOAuthNonce;

            _pendingOAuthIdToken = null;
            _pendingOAuthNonce   = null;

            // Gửi id_token lên backend POST /api/v1/auth/oauth
            yield return AuthApiClient.OAuthLogin(
                _config, idToken, nonce, OnOAuthLoginComplete);
        }

        // ── Session Persistence ───────────────────────────────────────────────

        private void StoreSession(string jwt, string refresh, string email, string userId = null, int expiresInSeconds = 0)
        {
            CurrentToken = jwt;
            CurrentRefreshToken = refresh;
            CurrentEmail = email;
            CurrentUserId = userId;
            TokenExpiresAtUnix = ComputeExpiresAtUnix(jwt, expiresInSeconds);

            if (_config == null) return;

            PlayerPrefs.SetString(_config.PrefKeyJwt,     jwt     ?? "");
            PlayerPrefs.SetString(_config.PrefKeyEmail,   email   ?? "");
            PlayerPrefs.SetString(_config.PrefKeyRefresh, refresh ?? "");
            PlayerPrefs.SetString(GetExpiresAtPrefKey(),  TokenExpiresAtUnix > 0 ? TokenExpiresAtUnix.ToString() : "");
            if (!string.IsNullOrEmpty(userId))
            {
                PlayerPrefs.SetString(_config.PrefKeyUserId, userId);
            }
            PlayerPrefs.Save();
        }

        private void LoadSavedSession()
        {
            if (_config == null) return;

            string jwt = PlayerPrefs.GetString(_config.PrefKeyJwt, "");
            string refresh = PlayerPrefs.GetString(_config.PrefKeyRefresh, "");
            if (!string.IsNullOrEmpty(jwt) || !string.IsNullOrEmpty(refresh))
            {
                CurrentToken = jwt;
                CurrentRefreshToken = refresh;
                CurrentEmail = PlayerPrefs.GetString(_config.PrefKeyEmail, "");
                CurrentUserId = PlayerPrefs.GetString(_config.PrefKeyUserId, "");

                string expiresRaw = PlayerPrefs.GetString(GetExpiresAtPrefKey(), "");
                if (long.TryParse(expiresRaw, out long parsedExp) && parsedExp > 0)
                {
                    TokenExpiresAtUnix = parsedExp;
                }
                else
                {
                    TokenExpiresAtUnix = TryExtractJwtExpUnix(jwt);
                }
            }
        }

        private void ClearSavedSession()
        {
            if (_config == null) return;

            PlayerPrefs.DeleteKey(_config.PrefKeyJwt);
            PlayerPrefs.DeleteKey(_config.PrefKeyEmail);
            PlayerPrefs.DeleteKey(_config.PrefKeyUserId);
            PlayerPrefs.DeleteKey(_config.PrefKeyRefresh);
            PlayerPrefs.DeleteKey(GetExpiresAtPrefKey());
            PlayerPrefs.Save();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string GetExpiresAtPrefKey()
        {
            if (_config != null && !string.IsNullOrEmpty(_config.PrefKeyExpiresAt))
                return _config.PrefKeyExpiresAt;
            return "auth_expires_at";
        }

        private static long ComputeExpiresAtUnix(string jwt, int expiresInSeconds)
        {
            if (expiresInSeconds > 0)
                return DateTimeOffset.UtcNow.ToUnixTimeSeconds() + expiresInSeconds;

            return TryExtractJwtExpUnix(jwt);
        }

        [Serializable]
        private class JwtExpPayload
        {
            public long exp;
        }

        private static long TryExtractJwtExpUnix(string jwt)
        {
            if (string.IsNullOrEmpty(jwt)) return 0;

            string[] parts = jwt.Split('.');
            if (parts.Length < 2) return 0;

            try
            {
                string payload = parts[1].Replace('-', '+').Replace('_', '/');
                int mod = payload.Length % 4;
                if (mod == 2) payload += "==";
                else if (mod == 3) payload += "=";

                byte[] bytes = Convert.FromBase64String(payload);
                string json = Encoding.UTF8.GetString(bytes);
                var data = JsonUtility.FromJson<JwtExpPayload>(json);
                return data != null && data.exp > 0 ? data.exp : 0;
            }
            catch
            {
                return 0;
            }
        }

        private bool ValidateConfig()
        {
            if (_config != null) return true;
            Debug.LogError("[AuthService] AuthConfig chưa được gán trong Inspector!", this);
            return false;
        }
    }
}
