using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace ChasingLight
{
    // ──────────────────────────────────────────────────────────────────────────
    //  Data Transfer Objects
    // ──────────────────────────────────────────────────────────────────────────

    [Serializable]
    public class SignInRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class SignUpRequest
    {
        public string email;
        public string password;
    }

    [Serializable]
    public class ResetPasswordRequest
    {
        public string email;
    }

    [Serializable]
    public class AuthResponse
    {
        /// <summary>JWT access token trả về khi đăng nhập / đăng ký thành công.</summary>
        public string access_token;
        /// <summary>Refresh token (nếu backend cung cấp).</summary>
        public string refresh_token;
        public string token_type;
        public int    expires_in;
    }

    [Serializable]
    public class ApiErrorResponse
    {
        public string message;
        public string error;
        public int    statusCode;
    }

    /// <summary>
    /// Kết quả trả về từ mọi lời gọi API. Dùng <see cref="IsSuccess"/> để kiểm tra.
    /// </summary>
    public class ApiResult<T>
    {
        public bool   IsSuccess;
        public T      Data;
        public string ErrorMessage;
        public int    HttpStatusCode;

        public static ApiResult<T> Success(T data, int code = 200) =>
            new ApiResult<T> { IsSuccess = true, Data = data, HttpStatusCode = code };

        public static ApiResult<T> Failure(string message, int code = 0) =>
            new ApiResult<T> { IsSuccess = false, ErrorMessage = message, HttpStatusCode = code };
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  HTTP Client
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Gọi Supabase-compatible REST API bằng <see cref="UnityWebRequest"/>.
    /// Không phụ thuộc MonoBehaviour – cần một coroutine runner bên ngoài
    /// (xem <see cref="AuthService"/>).
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
            yield return Post<AuthResponse>(config, config.SignInUrl, body, onComplete);
        }

        // ── Sign Up ──────────────────────────────────────────────────────────

        public static IEnumerator SignUp(
            AuthConfig config,
            string email,
            string password,
            Action<ApiResult<AuthResponse>> onComplete)
        {
            var body = JsonUtility.ToJson(new SignUpRequest { email = email, password = password });
            yield return Post<AuthResponse>(config, config.SignUpUrl, body, onComplete);
        }

        // ── Reset Password ────────────────────────────────────────────────────

        public static IEnumerator ResetPassword(
            AuthConfig config,
            string email,
            Action<ApiResult<string>> onComplete)
        {
            var body = JsonUtility.ToJson(new ResetPasswordRequest { email = email });
            yield return Post<string>(config, config.ResetPassUrl, body, onComplete);
        }

        // ── Generic POST ──────────────────────────────────────────────────────

        private static IEnumerator Post<T>(
            AuthConfig config,
            string url,
            string jsonBody,
            Action<ApiResult<T>> onComplete)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<T>.Failure("AuthConfig chưa được gán."));
                yield break;
            }

            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler   = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout         = config.TimeoutSeconds
            };

            req.SetRequestHeader("Content-Type",  "application/json");
            req.SetRequestHeader("Accept",         "application/json");

            if (!string.IsNullOrEmpty(config.ApiKey))
                req.SetRequestHeader("apikey", config.ApiKey);

            yield return req.SendWebRequest();

            int statusCode = (int)req.responseCode;
            string raw     = req.downloadHandler?.text ?? "";

            if (req.result == UnityWebRequest.Result.ConnectionError ||
                req.result == UnityWebRequest.Result.DataProcessingError)
            {
                onComplete?.Invoke(ApiResult<T>.Failure($"Lỗi kết nối: {req.error}", statusCode));
                yield break;
            }

            bool httpSuccess = statusCode >= 200 && statusCode < 300;

            if (!httpSuccess)
            {
                string errMsg = TryParseError(raw, statusCode);
                onComplete?.Invoke(ApiResult<T>.Failure(errMsg, statusCode));
                yield break;
            }

            // Nếu T là string (ví dụ reset-password chỉ cần biết thành công)
            if (typeof(T) == typeof(string))
            {
                onComplete?.Invoke(ApiResult<T>.Success((T)(object)raw, statusCode));
                yield break;
            }

            try
            {
                T data = JsonUtility.FromJson<T>(raw);
                onComplete?.Invoke(ApiResult<T>.Success(data, statusCode));
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(ApiResult<T>.Failure($"Lỗi parse JSON: {ex.Message}", statusCode));
            }
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static string TryParseError(string raw, int code)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return $"Lỗi HTTP {code}";

            try
            {
                var err = JsonUtility.FromJson<ApiErrorResponse>(raw);
                if (!string.IsNullOrEmpty(err?.message)) return err.message;
                if (!string.IsNullOrEmpty(err?.error))   return err.error;
            }
            catch { /* ignore */ }

            return $"Lỗi HTTP {code}: {raw}";
        }
    }
}

