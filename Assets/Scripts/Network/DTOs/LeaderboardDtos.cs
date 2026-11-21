using System;
using System.Collections.Generic;

namespace ChasingLight.Network.DTOs
{
    /// <summary>
    /// Bản ghi xếp hạng toàn cầu (Top sao) trả về từ GET /api/v1/leaderboards/global.
    /// </summary>
    [Serializable]
    public class GlobalLeaderboardEntryDto
    {
        public string userId;
        public string displayName;
        public string avatarUrl;
        public int totalStars;
        public int rank;
    }

    /// <summary>
    /// Bản ghi xếp hạng theo màn chơi (Top thời gian / bước) trả về từ GET /api/v1/leaderboards/stage/{stageId}.
    /// </summary>
    [Serializable]
    public class StageLeaderboardEntryDto
    {
        public string userId;
        public string displayName;
        public string avatarUrl;
        public int bestTimeSeconds;
        public int bestStepCount;
        public int starsEarned;
        public int rank;
    }

    /// <summary>
    /// Wrapper phục vụ deserialize root JSON arrays ("[...] -> { \"items\": [...] }") với JsonUtility.
    /// </summary>
    [Serializable]
    public class JsonArrayWrapper<T>
    {
        public List<T> items = new();
    }
}
