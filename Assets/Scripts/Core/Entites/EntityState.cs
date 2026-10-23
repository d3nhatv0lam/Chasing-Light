using UnityEngine;

namespace ChasingLight
{
    public sealed class EntityState
    {
        public int Id { get; }
        public Vector2Int Position { get; set; }

        // false = đã rơi khỏi màn chơi.
        public bool Alive { get; set; } = true;

        // false = shadow chưa spawn.
        public bool Active { get; set; } = true;
        
        public EntityState(int id, Vector2Int position)
        {
            Id = id;
            Position = position;
        }
    }
}