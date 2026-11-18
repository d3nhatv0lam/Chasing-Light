using System;
using System.Collections.Generic;

namespace ChasingLight
{
    [Serializable]
    public class StageRecordItem
    {
        public string stageId;
        public int stageNumber;
        public int difficulty; // 0 = Easy, 1 = Normal, 2 = Hard
        public int starsEarned;
        public int bestTimeSeconds;
        public int bestStepCount;
        public string updatedAt;

        public StageRecordItem() { }

        public StageRecordItem(string stageId, int stageNumber, int difficulty, int starsEarned, int bestTimeSeconds, int bestStepCount)
        {
            this.stageId = stageId;
            this.stageNumber = stageNumber;
            this.difficulty = difficulty;
            this.starsEarned = starsEarned;
            this.bestTimeSeconds = bestTimeSeconds;
            this.bestStepCount = bestStepCount;
            this.updatedAt = DateTime.UtcNow.ToString("o");
        }
    }

    [Serializable]
    public class GameSaveData
    {
        public string userId = "guest";
        public bool isGuest = true;
        public int totalStars = 0;
        public List<StageRecordItem> stageRecords = new();
        public bool hasUnsyncedChanges = false;
        public string lastSavedAt;
    }
}
