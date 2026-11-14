using System;
using System.Collections;
using System.Text;
using ChasingLight.Network.DTOs;
using UnityEngine;
using UnityEngine.Networking;

namespace ChasingLight.Network.Clients
{
    /// <summary>
    /// REST API Client kết nối tới các endpoint /api/v1/records của ChasingLight Backend.
    /// Dùng coroutine và UnityWebRequest.
    /// </summary>
    public static class RecordApiClient
    {
        // ── Complete Stage (POST /api/v1/records/complete) ────────────────────
        public static IEnumerator CompleteStage(
            AuthConfig config,
            string jwt,
            CompleteStageRequest request,
            Action<ApiResult<CompleteStageResponse>> onComplete)
        {
            if (request == null)
            {
                onComplete?.Invoke(ApiResult<CompleteStageResponse>.Failure("CompleteStageRequest cannot be null."));
                yield break;
            }

            string jsonBody = JsonUtility.ToJson(request);
            yield return Post(config, config.RecordsCompleteUrl, jwt, jsonBody, onComplete);
        }

        // ── Sync Records (POST /api/v1/records/sync) ──────────────────────────
        public static IEnumerator SyncRecords(
            AuthConfig config,
            string jwt,
            SyncRecordsRequest request,
            Action<ApiResult<SyncRecordsResponse>> onComplete)
        {
            if (request == null)
            {
                onComplete?.Invoke(ApiResult<SyncRecordsResponse>.Failure("SyncRecordsRequest cannot be null."));
                yield break;
            }

            string jsonBody = JsonUtility.ToJson(request);
            yield return Post(config, config.RecordsSyncUrl, jwt, jsonBody, onComplete);
        }

        // ── Get My Records (GET /api/v1/records/me) ───────────────────────────
        public static IEnumerator GetMyRecords(
            AuthConfig config,
            string jwt,
            Action<ApiResult<UserRecordsSummaryResponse>> onComplete)
        {
            yield return Get(config, config.RecordsMeUrl, jwt, onComplete);
        }

        // ── Generic HTTP Methods ──────────────────────────────────────────────

        private static IEnumerator Post<T>(
            AuthConfig config,
            string url,
            string jwt,
            string jsonBody,
            Action<ApiResult<T>> onComplete)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<T>.Failure("AuthConfig chưa được gán."));
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

        private static IEnumerator Get<T>(
            AuthConfig config,
            string url,
            string jwt,
            Action<ApiResult<T>> onComplete)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<T>.Failure("AuthConfig chưa được gán."));
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

            if (!string.IsNullOrEmpty(config.ApiKey))
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
