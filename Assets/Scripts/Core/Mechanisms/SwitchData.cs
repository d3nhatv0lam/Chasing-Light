using UnityEngine;

namespace ChasingLight
{
    public sealed class SwitchData
    {
        public int Id { get; }
        public Vector2Int Position { get; }
        public bool InitialActivated  { get; }

        public SwitchData(int id, Vector2Int position)
        {
            Id = id;
            Position = position;
        }
    }
}