using ChasingLight.Core.Interfaces;
using UnityEngine;

namespace ChasingLight
{
    public sealed class GateData : IIdentifiable
    {
        public int Id { get; }
        public Vector2Int Position { get; }
        public SwitchCondition Condition { get; }
        public bool InitialSolid { get; }
        

        public GateData(int id, Vector2Int position, SwitchCondition condition,  bool initialSolid = false)
        {
            Id = id;
            Position = position;
            Condition = condition;
            InitialSolid = initialSolid;
        }
    }
}