using System;
using System.Collections;
using System.Collections.Generic;
using ChasingLight.Network.DTOs;
using UnityEngine;

namespace ChasingLight.Network.Clients
{
    /// <summary>
    /// REST API Client kết nối tới các endpoint /api/v1/leaderboards của ChasingLight Backend:
    /// - GET /api/v1/leaderboards/global?limit=50
    /// - GET /api/v1/leaderboards/stage/{stageId}?limit=50
    /// </summary>
    public static class LeaderboardApiClient
    {
        // ── Get Global Leaderboard ────────────────────────────────────────────

        /// <summary>
        /// Lấy bảng xếp hạng toàn cầu sắp xếp theo tổng số sao giảm dần.
        /// </summary>
        public static IEnumerator GetGlobalLeaderboard(
            AuthConfig config,
            int limit,
            Action<ApiResult<List<GlobalLeaderboardEntryDto>>> onComplete,
            string jwt = null)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<List<GlobalLeaderboardEntryDto>>.Failure("AuthConfig is not assigned."));
                yield break;
            }

            string url = config.GetLeaderboardGlobalUrl(limit);

            yield return BaseApiClient.Get<string>(config, url, result =>
            {
                HandleArrayResponse(result, onComplete);
            }, jwt);
        }

        // ── Get Stage Leaderboard ─────────────────────────────────────────────

        /// <summary>
        /// Lấy bảng xếp hạng theo màn chơi sắp xếp theo số sao, thời gian và số bước đi.
        /// </summary>
        public static IEnumerator GetStageLeaderboard(
            AuthConfig config,
            string stageId,
            int limit,
            Action<ApiResult<List<StageLeaderboardEntryDto>>> onComplete,
            string jwt = null)
        {
            if (config == null)
            {
                onComplete?.Invoke(ApiResult<List<StageLeaderboardEntryDto>>.Failure("AuthConfig is not assigned."));
                yield break;
            }

            if (string.IsNullOrEmpty(stageId))
            {
                onComplete?.Invoke(ApiResult<List<StageLeaderboardEntryDto>>.Failure("Stage ID cannot be null or empty."));
                yield break;
            }

            string url = config.GetLeaderboardStageUrl(stageId, limit);

            yield return BaseApiClient.Get<string>(config, url, result =>
            {
                HandleArrayResponse(result, onComplete);
            }, jwt);
        }

        // ── Helper Parsing JSON Array ─────────────────────────────────────────

        private static void HandleArrayResponse<T>(
            ApiResult<string> rawResult,
            Action<ApiResult<List<T>>> onComplete)
        {
            if (!rawResult.IsSuccess)
            {
                onComplete?.Invoke(ApiResult<List<T>>.Failure(rawResult.ErrorMessage, rawResult.HttpStatusCode));
                return;
            }

            string raw = rawResult.Data?.Trim();
            if (string.IsNullOrEmpty(raw) || raw == "[]")
            {
                onComplete?.Invoke(ApiResult<List<T>>.Success(new List<T>(), rawResult.HttpStatusCode));
                return;
            }

            try
            {
                string wrapped = "{\"items\":" + raw + "}";
                var wrapper = JsonUtility.FromJson<JsonArrayWrapper<T>>(wrapped);
                var list = wrapper?.items ?? new List<T>();
                onComplete?.Invoke(ApiResult<List<T>>.Success(list, rawResult.HttpStatusCode));
            }
            catch (Exception ex)
            {
                onComplete?.Invoke(ApiResult<List<T>>.Failure(
                    $"JSON parse error: {ex.Message} (Payload: {raw})",
                    rawResult.HttpStatusCode));
            }
        }
    }
}
