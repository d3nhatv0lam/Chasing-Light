using System.Collections.Generic;

namespace ChasingLight
{
    public sealed class StageData
    {
        public string Id { get; }
        public int StageNumber { get; }
        public Difficulty Difficulty { get; }
        public GridData Grid { get; }
        public IReadOnlyList<EntityData> Entities { get; }
        public int ShadowSpawnDelaySteps { get; }
        public IReadOnlyList<SwitchData> Switches { get; }
        public IReadOnlyList<GateData> Gates { get; }
        public IReadOnlyList<TeleportData> Teleports { get; }
        public IReadOnlyList<SwitchLinkData> SwitchLinks { get; }
        public IReadOnlyList<StateStar> StateStars { get; }
        
        public StageData(
            string id,
            int stageNumber,
            Difficulty difficulty,
            GridData grid,
            IReadOnlyList<EntityData> entities,
            int shadowSpawnDelaySteps,
            IReadOnlyList<SwitchData> switches,
            IReadOnlyList<GateData> gates,
            IReadOnlyList<TeleportData> teleports,
            IReadOnlyList<SwitchLinkData> switchLinks,
            IReadOnlyList<StateStar> stateStars)
        {
            Id = id;
            StageNumber = stageNumber;
            Difficulty = difficulty;
            Grid = grid;
            Entities = entities;
            ShadowSpawnDelaySteps = shadowSpawnDelaySteps;
            Switches = switches;
            Gates = gates;
            Teleports = teleports;
            SwitchLinks = switchLinks;
            StateStars = stateStars;
        }
    }
}