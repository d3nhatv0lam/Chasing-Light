namespace ChasingLight
{
    public sealed class StateStar
    {
        public bool hasTimeLimit { get; }
        public int maxTimeSeconds { get; }

        public StateStar(
            bool hasTimeLimit,
            int maxTimeSeconds)
        {
            this.hasTimeLimit = hasTimeLimit;
            this.maxTimeSeconds = maxTimeSeconds;
        }
    }
}