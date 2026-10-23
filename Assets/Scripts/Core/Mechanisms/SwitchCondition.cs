using System;
using System.Collections.Generic;

namespace ChasingLight
{
    public enum LinkLogic
    {
        All,
        Any
    }
    
    [Serializable]
    public sealed class SwitchCondition
    {
        public LinkLogic Logic { get; }
        public IReadOnlyList<int> SwitchIds { get; }

        public SwitchCondition(
            LinkLogic logic,
            IReadOnlyList<int> switchIds)
        {
            Logic = logic;
            SwitchIds = switchIds;
        }
    }
}