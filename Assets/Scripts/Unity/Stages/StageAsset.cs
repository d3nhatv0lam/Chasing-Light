using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChasingLight
{
    [CreateAssetMenu(
        fileName = "NewStage",
        menuName = "Chasing Light/Stage")]
    public sealed class StageAsset : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private int stageNumber;
        [SerializeField] private Difficulty difficulty;

        [Header("Grid")] [SerializeField] private int height;
        [SerializeField] private int width;
        [SerializeField] private List<CellDefinition> cells;

        [Header("Entities")] [SerializeField] private List<EntityDefinition> entities;
        [SerializeField] private int shadowSpawnDelaySteps;

        [Header("Switches")] [SerializeField] private List<SwitchDefinition> switches;

        [Header("Gates")] [SerializeField] private List<GateDefinition> gates;

        [Header("Teleports")] [SerializeField] private List<TeleportDefinition> teleports;

        [Header("Stars")] [SerializeField] private List<StarCondition> starConditions;

        public StageData ToData()
        {
            var gridCells = this.cells.Select(cell => new CellData(cell.position, cell.type)).ToList();
            var grid = new GridData(height, width, gridCells);

            var entitiesData = entities.Select(e => new EntityData(e.id, e.type, e.position)).ToList();
            var switchesData = switches.Select(s => new SwitchData(s.id, s.position, s.mode, s.initialActivated))
                .ToList();
            var gatesData = gates
                .Select(g => new GateData(g.id, g.position, g.switchCondition.ToData(), g.initialOpened))
                .ToList();
            var teleportsData = teleports
                .Select(t =>
                    new TeleportData(t.id, t.positionA, t.positionB, t.switchCondition.ToData(), t.initialEnabled))
                .ToList();

            var stageStar = starConditions.Select(star => new StateStar(star.hasTimeLimit, star.maxTimeSeconds))
                .ToList();

            return new StageData(
                id,
                stageNumber,
                difficulty,
                grid,
                entitiesData,
                shadowSpawnDelaySteps,
                switchesData,
                gatesData,
                teleportsData,
                stageStar);
        }

        private void EnsureIds()
        {
            EnsureIds(entities, e => e.id, (e, newId) => e.id = newId);
            EnsureIds(switches, s => s.id, (s, newId) => s.id = newId);
            EnsureIds(gates, g => g.id, (g, newId) => g.id = newId);
            EnsureIds(teleports, t => t.id, (t, newId) => t.id = newId);
        }


        private void Awake()
        {
            CheckAndAssignID();
        }

        private void OnValidate()
        {
            // id của màn chơi
            CheckAndAssignID();

            EnsureIds();
            ValidateSwitchLinks();

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
        }

        private void CheckAndAssignID()
        {
            if (string.IsNullOrEmpty(id))
            {
                id = System.Guid.NewGuid().ToString("D");
            }
        }

        private void ValidateSwitchLinks()
        {
            var switchIds = new HashSet<int>(
                switches.Select(s => s.id));

            foreach (var teleport in teleports)
            {
                foreach (var id in teleport.switchCondition.switchIds)
                {
                    if (!switchIds.Contains(id))
                    {
                        Debug.LogError(
                            $"Stage '{name}' - Teleport '{teleport.id}': Switch ID {id} does not exist.",
                            this);
                    }
                }
            }

            foreach (var gate in gates)
            {
                foreach (var id in gate.switchCondition.switchIds)
                {
                    if (!switchIds.Contains(id))
                    {
                        Debug.LogError(
                            $"Stage '{name}' - Gate '{gate.id}': Switch ID {id} does not exist.",
                            this);
                    }
                }
            }
        }

        private static void EnsureIds<T>(
            List<T> items,
            Func<T, int> getId,
            Action<T, int> setId)
        {
            var usedIds = new HashSet<int>();
            int maxId = 0;

            foreach (var item in items)
            {
                var currentId = getId(item);

                if (currentId <= 0)
                    continue;

                maxId = Math.Max(maxId, currentId);
            }

            foreach (var item in items)
            {
                var currentId = getId(item);

                if (currentId > 0 && usedIds.Add(currentId))
                    continue;

                maxId++;
                setId(item, maxId);
                usedIds.Add(maxId);
            }
        }
    }


    [Serializable]
    public sealed class CellDefinition
    {
        public Vector2Int position;
        public CellType type;
    }

    [Serializable]
    public sealed class EntityDefinition
    {
        public int id;
        public EntityType type;
        public Vector2Int position;
    }

    [Serializable]
    public sealed class SwitchDefinition
    {
        public int id;
        public Vector2Int position;
        public SwitchMode mode;
        public bool initialActivated;
    }

    [Serializable]
    public sealed class GateDefinition
    {
        public int id;
        public Vector2Int position;
        public bool initialOpened;
        public SwitchConditionDefinition switchCondition;
    }

    [Serializable]
    public sealed class TeleportDefinition
    {
        public int id;
        public Vector2Int positionA;
        public Vector2Int positionB;
        public SwitchConditionDefinition switchCondition;
        public bool initialEnabled;
    }

    [Serializable]
    public sealed class SwitchConditionDefinition
    {
        public LinkLogic logic;
        public List<int> switchIds = new();

        public SwitchCondition ToData()
        {
            return new SwitchCondition(logic, switchIds);
        }
    }

    [Serializable]
    public sealed class SwitchLinkDefinition
    {
        public int switchId;
        public MechanismType targetType;
        public int targetId;
    }

    [Serializable]
    public class StarCondition
    {
        public bool hasTimeLimit;
        [Min(0)] public int maxTimeSeconds;
    }
}