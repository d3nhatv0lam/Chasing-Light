using System;
using System.Collections;
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
    /// </list>
    /// </para>
    /// </summary>
    public class AuthService : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private AuthConfig _config;

        // ── Public state ──────────────────────────────────────────────────────

        /// <summary>Token JWT hiện tại (null nếu chưa đăng nhập).</summary>
        public string CurrentToken  { get; private set; }

        /// <summary>Email của user đang đăng nhập.</summary>
        public string CurrentEmail  { get; private set; }

        public bool IsLoggedIn => !string.IsNullOrEmpty(CurrentToken);

        // ── Events ────────────────────────────────────────────────────────────

        public event Action<string> OnLoginSuccess;
        public event Action<string> OnLoginFailed;

        public event Action<string> OnRegisterSuccess;
        public event Action<string> OnRegisterFailed;

        public event Action         OnResetPasswordSuccess;
        public event Action<string> OnResetPasswordFailed;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            LoadSavedSession();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Đăng nhập bằng email + password.
        /// Kết quả được thông báo qua <see cref="OnLoginSuccess"/> / <see cref="OnLoginFailed"/>.
        /// </summary>
        public void Login(string email, string password)
        {
            if (!ValidateConfig()) return;
            StartCoroutine(AuthApiClient.SignIn(_config, email, password, OnSignInComplete));
        }

        /// <summary>
        /// Đăng ký tài khoản mới.
        /// </summary>
        public void Register(string email, string password)
        {
            if (!ValidateConfig()) return;
            StartCoroutine(AuthApiClient.SignUp(_config, email, password, OnSignUpComplete));
        }

        /// <summary>
        /// Gửi email đặt lại mật khẩu.
        /// </summary>
        public void SendResetPassword(string email)
        {
            if (!ValidateConfig()) return;
            StartCoroutine(AuthApiClient.ResetPassword(_config, email, OnResetComplete));
        }

        /// <summary>
        /// Đăng xuất: xoá token khỏi bộ nhớ và PlayerPrefs.
        /// </summary>
        public void Logout()
        {
            CurrentToken = null;
            CurrentEmail = null;
            ClearSavedSession();
        }

        // ── Callbacks ─────────────────────────────────────────────────────────

        private void OnSignInComplete(ApiResult<AuthResponse> result)
        {
            if (result.IsSuccess && result.Data != null)
            {
                StoreSession(result.Data.access_token, result.Data.refresh_token, CurrentEmail);
                OnLoginSuccess?.Invoke(result.Data.access_token);
            }
            else
            {
                OnLoginFailed?.Invoke(result.ErrorMessage ?? "Đăng nhập thất bại.");
            }
        }

        private void OnSignUpComplete(ApiResult<AuthResponse> result)
        {
            if (result.IsSuccess)
            {
                // Một số backend không trả token ngay sau đăng ký (cần verify email)
                if (result.Data != null && !string.IsNullOrEmpty(result.Data.access_token))
                    StoreSession(result.Data.access_token, result.Data.refresh_token, CurrentEmail);

                OnRegisterSuccess?.Invoke(
                    result.Data?.access_token ?? "Đăng ký thành công. Vui lòng kiểm tra email.");
            }
            else
            {
                OnRegisterFailed?.Invoke(result.ErrorMessage ?? "Đăng ký thất bại.");
            }
        }

        private void OnResetComplete(ApiResult<string> result)
        {
            if (result.IsSuccess)
                OnResetPasswordSuccess?.Invoke();
            else
                OnResetPasswordFailed?.Invoke(result.ErrorMessage ?? "Gửi email thất bại.");
        }

        // ── Session Persistence ───────────────────────────────────────────────

        private void StoreSession(string jwt, string refresh, string email)
        {
            CurrentToken = jwt;
            CurrentEmail = email;

            if (_config == null) return;

            PlayerPrefs.SetString(_config.PrefKeyJwt,     jwt     ?? "");
            PlayerPrefs.SetString(_config.PrefKeyEmail,   email   ?? "");
            PlayerPrefs.SetString(_config.PrefKeyRefresh, refresh ?? "");
            PlayerPrefs.Save();
        }

        private void LoadSavedSession()
        {
            if (_config == null) return;

            string jwt = PlayerPrefs.GetString(_config.PrefKeyJwt, "");
            if (!string.IsNullOrEmpty(jwt))
            {
                CurrentToken = jwt;
                CurrentEmail = PlayerPrefs.GetString(_config.PrefKeyEmail, "");
            }
        }

        private void ClearSavedSession()
        {
            if (_config == null) return;

            PlayerPrefs.DeleteKey(_config.PrefKeyJwt);
            PlayerPrefs.DeleteKey(_config.PrefKeyEmail);
            PlayerPrefs.DeleteKey(_config.PrefKeyRefresh);
            PlayerPrefs.Save();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private bool ValidateConfig()
        {
            if (_config != null) return true;
            Debug.LogError("[AuthService] AuthConfig chưa được gán trong Inspector!", this);
            return false;
        }
    }
}

