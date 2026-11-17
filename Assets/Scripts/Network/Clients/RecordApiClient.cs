using System;
using System.Collections;
using ChasingLight.Network.DTOs;
using UnityEngine;

namespace ChasingLight.Network.Clients
{
    /// <summary>
    /// REST API Client kết nối tới các endpoint /api/v1/records của ChasingLight Backend.
    /// Sử dụng <see cref="BaseApiClient"/> để thực thi HTTP requests.
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
            yield return BaseApiClient.Post(config, config.RecordsCompleteUrl, jsonBody, onComplete, jwt);
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
            yield return BaseApiClient.Post(config, config.RecordsSyncUrl, jsonBody, onComplete, jwt);
        }

        // ── Get My Records (GET /api/v1/records/me) ───────────────────────────
        public static IEnumerator GetMyRecords(
            AuthConfig config,
            string jwt,
            Action<ApiResult<UserRecordsSummaryResponse>> onComplete)
        {
            yield return BaseApiClient.Get(config, config.RecordsMeUrl, onComplete, jwt);
        }
    }
}
