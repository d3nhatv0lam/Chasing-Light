using System;
using System.Collections;
using ChasingLight.Network.Clients;
using ChasingLight.Network.DTOs;
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
            var body = JsonUtility.ToJson(new SignUpRequest { email = email, password = password });
            yield return BaseApiClient.Post(config, config.SignUpUrl, body, onComplete);
        }

        // ── Reset Password ────────────────────────────────────────────────────

        public static IEnumerator ResetPassword(
            AuthConfig config,
            string email,
            Action<ApiResult<string>> onComplete)
        {
            var body = JsonUtility.ToJson(new ResetPasswordRequest { email = email });
            yield return BaseApiClient.Post<string>(config, config.ResetPassUrl, body, onComplete);
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
            var body = JsonUtility.ToJson(new OAuthLoginRequest
            {
                provider = "google",
                idToken  = idToken,
                nonce    = nonce
            });
            yield return BaseApiClient.Post(config, config.GoogleOAuthUrl, body, onComplete);
        }
    }
}
