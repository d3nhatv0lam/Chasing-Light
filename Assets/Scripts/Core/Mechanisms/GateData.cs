using ChasingLight.Core.Interfaces;
using UnityEngine;

namespace ChasingLight
{
    public sealed class GateData : IIdentifiable
    {
        public int Id { get; }
        public Vector2Int Position { get; }
        public bool InitialOpened { get; }

        public GateData(int id, Vector2Int position,  bool initialOpened = false)
        {
            Id = id;
            Position = position;
            InitialOpened = initialOpened;
        }
    }
}