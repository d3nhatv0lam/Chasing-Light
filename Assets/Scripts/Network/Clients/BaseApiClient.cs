using System;
using System.Collections;
using System.Text;
using ChasingLight.Network.DTOs;
using UnityEngine;
using UnityEngine.Networking;

namespace ChasingLight.Network.Clients
{
    /// <summary>
    /// Base HTTP client xử lý các tác vụ mạng chung bằng UnityWebRequest:
    /// - Quản lý HTTP Header, Bearer token, Supabase ApiKey
    /// - Timeout và phân tích lỗi kết nối / lỗi HTTP
    /// - Serialize/Deserialize JSON với JsonUtility
    /// </summary>
    public static class BaseApiClient
    {
        public static IEnumerator Post<T>(
            AuthConfig config,
            string url,
            string jsonBody,
            Action<ApiResult<T>> onComplete,
            string jwt = null)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<T>.Failure("AuthConfig is not assigned."));
                yield break;
            }

            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody ?? "");

            using var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler   = new UploadHandlerRaw(bodyRaw),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout         = config.TimeoutSeconds
            };

            SetupHeaders(req, config, jwt);

            yield return req.SendWebRequest();

            HandleResponse(req, onComplete);
        }

        public static IEnumerator Get<T>(
            AuthConfig config,
            string url,
            Action<ApiResult<T>> onComplete,
            string jwt = null)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<T>.Failure("AuthConfig is not assigned."));
                yield break;
            }

            using var req = UnityWebRequest.Get(url);
            req.timeout = config.TimeoutSeconds;

            SetupHeaders(req, config, jwt);

            yield return req.SendWebRequest();

            HandleResponse(req, onComplete);
        }

        private static void SetupHeaders(UnityWebRequest req, AuthConfig config, string jwt)
        {
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("Accept",       "application/json");

            if (!string.IsNullOrEmpty(jwt))
                req.SetRequestHeader("Authorization", $"Bearer {jwt}");

            if (config != null && !string.IsNullOrEmpty(config.ApiKey))
                req.SetRequestHeader("apikey", config.ApiKey);
        }

        private static void HandleResponse<T>(UnityWebRequest req, Action<ApiResult<T>> onComplete)
        {
            int statusCode = (int)req.responseCode;
            string raw = req.downloadHandler?.text ?? "";

            if (req.result == UnityWebRequest.Result.ConnectionError ||
                req.result == UnityWebRequest.Result.DataProcessingError)
            {
                onComplete?.Invoke(ApiResult<T>.Failure($"Connection error: {req.error}", statusCode));
                return;
            }

            bool httpSuccess = statusCode >= 200 && statusCode < 300;
            if (!httpSuccess)
            {
                string errMsg = TryParseError(raw, statusCode);
                onComplete?.Invoke(ApiResult<T>.Failure(errMsg, statusCode));
                return;
            }

            // Nếu T là string (ví dụ endpoint reset password chỉ cần text kết quả)
            if (typeof(T) == typeof(string))
            {
                onComplete?.Invoke(ApiResult<T>.Success((T)(object)raw, statusCode));
                return;
            }

            try
            {
                T data = JsonUtility.FromJson<T>(raw);
                onComplete?.Invoke(ApiResult<T>.Success(data, statusCode));
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(ApiResult<T>.Failure($"JSON parse error: {ex.Message} (Payload: {raw})", statusCode));
            }
        }

        private static string TryParseError(string raw, int code)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return $"HTTP Error {code}";

            try
            {
                var err = JsonUtility.FromJson<ApiErrorResponse>(raw);
                if (!string.IsNullOrEmpty(err?.message)) return err.message;
                if (!string.IsNullOrEmpty(err?.error))   return err.error;
            }
            catch { /* ignore */ }

            return $"HTTP Error {code}: {raw}";
        }
    }
}
