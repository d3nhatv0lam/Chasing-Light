namespace ChasingLight
{
    /// <summary>
    /// Truyền StageAsset được chọn từ scene chọn màn sang scene Game.
    /// Chỉ là dữ liệu điều hướng, không chứa luật chơi.
    /// </summary>
    public static class StageContext
    {
        public static StageAsset SelectedStage { get; set; }

        public static Difficulty LastSelectedDifficulty { get; set; } = Difficulty.Easy;
    }
}

