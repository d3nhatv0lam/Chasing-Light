using System;
using System.Collections.Generic;

namespace ChasingLight.Network.DTOs
{
    #region Requests

    [Serializable]
    public class CompleteStageRequest
    {
        public string stageId;
        public int timeDoneSeconds;
        public int stepCount;
        public int starsEarned;
    }

    [Serializable]
    public class SyncRecordItem
    {
        public string stageId;
        public int timeDoneSeconds;
        public int stepCount;
        public int starsEarned;
    }

    [Serializable]
    public class SyncRecordsRequest
    {
        public List<SyncRecordItem> records = new();
    }

    #endregion

    #region Responses

    [Serializable]
    public class CompleteStageResponse
    {
        public bool success;
        public bool isNewBest;
        public int totalStars;
        public int bestTimeSeconds;
        public int bestStepCount;
        public int starsEarned;
        public string message;
    }

    [Serializable]
    public class UserRecordDto
    {
        public string stageId;
        public int stageNumber;
        public int difficulty;
        public int starsEarned;
        public int bestTimeSeconds;
        public int bestStepCount;
        public string updatedAt;
    }

    [Serializable]
    public class SyncRecordsResponse
    {
        public bool success;
        public int totalStars;
        public int updatedCount;
        public List<UserRecordDto> records = new();
        public string message;
    }

    [Serializable]
    public class UserRecordsSummaryResponse
    {
        public int totalStars;
        public int completedStagesCount;
        public List<UserRecordDto> records = new();
    }

    #endregion
}
