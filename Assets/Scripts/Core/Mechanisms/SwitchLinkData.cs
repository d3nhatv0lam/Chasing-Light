namespace ChasingLight
{
    public sealed class SwitchLinkData
    {
        public int SwitchId { get; }
        public MechanismType Type { get; }
        public int TargetId { get; }

        public SwitchLinkData(int switchId, MechanismType type, int targetId)
        {
            SwitchId = switchId;
            Type = type;
            TargetId = targetId;
        }
    }
}