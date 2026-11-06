using ChasingLight.Core.Interfaces;
using UnityEngine;

namespace ChasingLight
{
    public class EntityData: IIdentifiable
    {
        public int Id { get; }
        public EntityType Type { get; }
        public Vector2Int Position { get; }

        public EntityData(int id, EntityType type, Vector2Int position)
        {
            Id = id;
            Type = type;
            Position = position;
        }
    }
}