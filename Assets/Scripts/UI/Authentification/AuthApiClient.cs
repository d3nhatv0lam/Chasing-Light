using System;
using System.Collections;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// REST API Client cho tính năng Authentication.
    /// Kế thừa và sử dụng <see cref="BaseApiClient"/> để thực thi HTTP requests.
    /// </summary>
    public static class AuthApiClient
    {
        // ── Sign In ──────────────────────────────────────────────────────────

        public static IEnumerator SignIn(
            AuthConfig config,
            string email,
            string password,
            Action<ApiResult<AuthResponse>> onComplete)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<AuthResponse>.Failure("AuthConfig is not assigned."));
                yield break;
            }

            var body = JsonUtility.ToJson(new SignInRequest { email = email, password = password });
            yield return BaseApiClient.Post(config, config.SignInUrl, body, onComplete);
        }

        // ── Sign Up ──────────────────────────────────────────────────────────

        public static IEnumerator SignUp(
            AuthConfig config,
            string email,
            string password,
            Action<ApiResult<AuthResponse>> onComplete)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<AuthResponse>.Failure("AuthConfig is not assigned."));
                yield break;
            }

            var body = JsonUtility.ToJson(new SignUpRequest { email = email, password = password });
            yield return BaseApiClient.Post(config, config.SignUpUrl, body, onComplete);
        }

        // ── Forgot Password (Send OTP to Email) ───────────────────────────────

        public static IEnumerator ForgotPassword(
            AuthConfig config,
            string email,
            Action<ApiResult<string>> onComplete)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<string>.Failure("AuthConfig is not assigned."));
                yield break;
            }

            var body = JsonUtility.ToJson(new ForgotPasswordRequest { email = email });
            yield return BaseApiClient.Post<string>(config, config.ForgotPassUrl, body, onComplete);
        }

        // ── Verify Recovery OTP (Get recoveryToken) ──────────────────────────

        public static IEnumerator VerifyRecoveryOtp(
            AuthConfig config,
            string email,
            string otp,
            Action<ApiResult<VerifyRecoveryOtpResponse>> onComplete)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<VerifyRecoveryOtpResponse>.Failure("AuthConfig is not assigned."));
                yield break;
            }

            var body = JsonUtility.ToJson(new VerifyRecoveryOtpRequest { email = email, otp = otp });
            yield return BaseApiClient.Post<VerifyRecoveryOtpResponse>(config, config.VerifyOtpUrl, body, onComplete);
        }

        // ── Reset Password (Change Password with recoveryToken) ──────────────

        public static IEnumerator ResetPassword(
            AuthConfig config,
            string recoveryToken,
            string newPassword,
            Action<ApiResult<AuthResponse>> onComplete)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<AuthResponse>.Failure("AuthConfig is not assigned."));
                yield break;
            }

            var body = JsonUtility.ToJson(new ResetPasswordRequest
            {
                recoveryToken = recoveryToken,
                newPassword   = newPassword
            });
            yield return BaseApiClient.Post<AuthResponse>(config, config.ResetPassUrl, body, onComplete);
        }

        // ── OAuth Login (Google id_token → backend) ───────────────────────────

        /// <summary>
        /// Gửi <paramref name="idToken"/> nhận được từ Google lên backend
        /// <c>POST /api/v1/auth/oauth</c> và trả về <see cref="AuthResponse"/>.
        /// </summary>
        public static IEnumerator OAuthLogin(
            AuthConfig config,
            string idToken,
            string nonce,
            Action<ApiResult<AuthResponse>> onComplete)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<AuthResponse>.Failure("AuthConfig is not assigned."));
                yield break;
            }

            var body = JsonUtility.ToJson(new OAuthLoginRequest
            {
                provider = "google",
                idToken  = idToken,
                nonce    = nonce
            });
            yield return BaseApiClient.Post(config, config.GoogleOAuthUrl, body, onComplete);
        }

        // ── Refresh Token (POST /api/v1/auth/refresh) ────────────────────────

        /// <summary>
        /// Gửi <paramref name="refreshToken"/> lên backend <c>POST /api/v1/auth/refresh</c>
        /// để đổi lấy cặp <c>accessToken</c> và <c>refreshToken</c> mới.
        /// </summary>
        public static IEnumerator RefreshToken(
            AuthConfig config,
            string refreshToken,
            Action<ApiResult<AuthResponse>> onComplete)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<AuthResponse>.Failure("AuthConfig is not assigned."));
                yield break;
            }

            if (string.IsNullOrEmpty(refreshToken))
            {
                onComplete?.Invoke(ApiResult<AuthResponse>.Failure("Refresh token is missing.", 401));
                yield break;
            }

            var body = JsonUtility.ToJson(new RefreshTokenRequest { refreshToken = refreshToken });
            yield return BaseApiClient.Post(config, config.RefreshTokenUrl, body, onComplete);
        }
    }
}
