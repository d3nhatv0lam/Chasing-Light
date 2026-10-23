using ChasingLight.Core.Interfaces;
using UnityEngine;

namespace ChasingLight
{
    public sealed class GateData : IIdentifiable
    {
        public int Id { get; }
        public Vector2Int Position { get; }
        public SwitchCondition Condition { get; }
        public bool InitialOpened { get; }
        

        public GateData(int id, Vector2Int position, SwitchCondition condition,  bool initialOpened = false)
        {
            Id = id;
            Position = position;
            Condition = condition;
            InitialOpened = initialOpened;
        }
    }
}