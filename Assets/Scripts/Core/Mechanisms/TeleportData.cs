using UnityEngine;

namespace ChasingLight
{
    public sealed class TeleportData
    {
        public int Id { get; }
        public Vector2Int PositionA { get; }
        public Vector2Int PositionB { get; }
        public bool InitialEnabled { get; }

        public TeleportData(
            int id,
            Vector2Int positionA,
            Vector2Int positionB)
        {
            Id = id;
            PositionA = positionA;
            PositionB = positionB;
        }
    }
}