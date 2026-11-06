using UnityEngine;

namespace ChasingLight
{
    public sealed class SwitchData
    {
        public int Id { get; }
        public Vector2Int Position { get; }
        public SwitchMode Mode { get; }
        public bool InitialActivated { get; }

        public SwitchData(int id, Vector2Int position, SwitchMode mode, bool initialActivated = false)
        {
            Id = id;
            Position = position;
            Mode = mode;
            InitialActivated = initialActivated;
        }
    }
}