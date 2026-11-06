using UnityEngine;


namespace ChasingLight
{
    public sealed record CellData 
    {
        public Vector2Int Position { get; private set; }
        public CellType Type { get; private set; }

        public CellData(Vector2Int position, CellType type)
        {
            Position = position;
            Type = type;
        }
    }
}
