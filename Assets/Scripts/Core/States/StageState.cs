using System.Collections.Generic;
using ChasingLight.Core.Actions;
using UnityEngine;

namespace ChasingLight
{
    public enum StageStatus
    {
        Playing,
        Won,
        Lost
    }

    public sealed class StageState
    {
        private readonly StageData stage;

        public StageData Data => stage;

        public int ElapsedTimeSeconds { get; set; }

        public int StepCount { get; set; }

        // Mọi lệnh của player (kể cả None / lệnh bị chặn), index 0 = bước 1.
        public List<EntityCommand> History { get; }

        public StageStatus Status { get; set; }

        public Dictionary<int, EntityState> Entities { get; }

        public Dictionary<int, bool> GateStates { get; }

        public Dictionary<int, bool> TeleportStates { get; }

        public Dictionary<int, bool> SwitchStates { get; }

        public StageState(StageData stageData)
        {
            this.stage = stageData;

            History = new List<EntityCommand>();
            Status = StageStatus.Playing;

            Entities = new Dictionary<int, EntityState>();
            GateStates = new Dictionary<int, bool>();
            TeleportStates = new Dictionary<int, bool>();
            SwitchStates = new Dictionary<int, bool>();

            InitializeEntities();
            InitializeGates();
            InitializeTeleports();
            InitializeSwitches();
        }

        private void InitializeEntities()
        {
            foreach (var entity in stage.Entities)
            {
                Entities.Add(entity.Id, new EntityState(entity.Id, entity.Position)
                {
                    // Shadow chỉ xuất hiện sau ShadowSpawnDelaySteps bước.
                    Active = entity.Type == EntityType.Player
                });
            }
        }

        private void InitializeGates()
        {
            foreach (var gate in stage.Gates)
            {
                GateStates.Add(gate.Id, gate.InitialOpened);
            }
        }

        private void InitializeTeleports()
        {
            foreach (var teleport in stage.Teleports)
            {
                TeleportStates.Add(teleport.Id, teleport.InitialEnabled);
            }
        }

        private void InitializeSwitches()
        {
            foreach (var sw in stage.Switches)
            {
                SwitchStates.Add(sw.Id, sw.InitialActivated);
            }
        }
    }
}