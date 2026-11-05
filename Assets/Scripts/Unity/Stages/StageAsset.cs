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

        [Header("Grid")]
        [SerializeField] private int height;
        [SerializeField] private int width;
        [SerializeField] private List<CellDefinition> cells = new();

        [Header("Entities")]
        [SerializeField] private List<EntityDefinition> entities = new();
        [SerializeField] private int shadowSpawnDelaySteps = 5;

        [Header("Switches")]
        [SerializeField] private List<SwitchDefinition> switches = new();

        [Header("Gates")]
        [SerializeField] private List<GateDefinition> gates = new();

        [Header("Teleports")]
        [SerializeField] private List<TeleportDefinition> teleports = new();

        [Header("Stars")]
        [SerializeField] private List<StarCondition> starConditions = new();

        public string Id { get => id; set => id = value; }
        public int StageNumber { get => stageNumber; set => stageNumber = value; }
        public Difficulty Difficulty { get => difficulty; set => difficulty = value; }
        public int Height { get => height; set => height = value; }
        public int Width { get => width; set => width = value; }
        public List<CellDefinition> Cells { get => cells ??= new(); set => cells = value; }
        public List<EntityDefinition> Entities { get => entities ??= new(); set => entities = value; }
        public int ShadowSpawnDelaySteps { get => shadowSpawnDelaySteps; set => shadowSpawnDelaySteps = value; }
        public List<SwitchDefinition> Switches { get => switches ??= new(); set => switches = value; }
        public List<GateDefinition> Gates { get => gates ??= new(); set => gates = value; }
        public List<TeleportDefinition> Teleports { get => teleports ??= new(); set => teleports = value; }
        public List<StarCondition> StarConditions { get => starConditions ??= new(); set => starConditions = value; }

        public StageData ToData()
        {
            var gridCells = (this.cells ?? new List<CellDefinition>())
                .Select(cell => new CellData(cell.position, cell.type))
                .ToList();
            var grid = new GridData(height, width, gridCells);

            var entitiesData = (entities ?? new List<EntityDefinition>())
                .Select(e => new EntityData(e.id, e.type, e.position))
                .ToList();
            var switchesData = (switches ?? new List<SwitchDefinition>())
                .Select(s => new SwitchData(s.id, s.position, s.mode, s.initialActivated))
                .ToList();
            var gatesData = (gates ?? new List<GateDefinition>())
                .Select(g => new GateData(g.id, g.position, g.switchCondition != null ? g.switchCondition.ToData() : new SwitchCondition(LinkLogic.All, Array.Empty<int>()), g.initialOpened))
                .ToList();
            var teleportsData = (teleports ?? new List<TeleportDefinition>())
                .Select(t => new TeleportData(t.id, t.positionA, t.positionB, t.switchCondition != null ? t.switchCondition.ToData() : new SwitchCondition(LinkLogic.All, Array.Empty<int>()), t.initialEnabled))
                .ToList();

            var stageStar = (starConditions ?? new List<StarCondition>())
                .Select(star => new StateStar(star.hasTimeLimit, star.maxTimeSeconds))
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

        public void EnsureAllIds()
        {
            EnsureIds();
        }

        public void AssignNewIdIfEmpty()
        {
            CheckAndAssignID();
        }

        public void SetupDefaultStars()
        {
            starConditions = new List<StarCondition>
            {
                new StarCondition(false, 0),   // Star 1: Complete stage
                new StarCondition(true, 420),  // Star 2: Under 7 mins (420s)
                new StarCondition(true, 180)   // Star 3: Under 3 mins (180s)
            };
        }

        public bool ValidateStage(out List<string> errors, out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();

            if (width <= 0 || height <= 0)
                errors.Add($"Invalid grid dimensions ({width}x{height}).");

            int startCount = cells != null ? cells.Count(c => c.type == CellType.Start) : 0;
            if (startCount == 0)
                errors.Add("Missing Start point (IN).");
            else if (startCount > 1)
                errors.Add($"Found {startCount} Start points! Exactly 1 Start point is allowed.");

            int goalCount = cells != null ? cells.Count(c => c.type == CellType.Goal) : 0;
            if (goalCount == 0)
                errors.Add("Missing Goal point (OUT).");
            else if (goalCount > 1)
                errors.Add($"Found {goalCount} Goal points! Exactly 1 Goal point is allowed.");

            var switchIds = new HashSet<int>(switches != null ? switches.Select(s => s.id) : Array.Empty<int>());

            if (gates != null)
            {
                foreach (var gate in gates)
                {
                    if (gate.switchCondition != null)
                    {
                        foreach (var sid in gate.switchCondition.switchIds)
                        {
                            if (!switchIds.Contains(sid))
                                errors.Add($"Gate #{gate.id} links to non-existent Switch #{sid}.");
                        }
                    }
                }
            }

            if (teleports != null)
            {
                foreach (var tp in teleports)
                {
                    if (tp.switchCondition != null)
                    {
                        foreach (var sid in tp.switchCondition.switchIds)
                        {
                            if (!switchIds.Contains(sid))
                                errors.Add($"Teleport #{tp.id} links to non-existent Switch #{sid}.");
                        }
                    }
                }
            }

            // Warnings
            if (entities == null || !entities.Any(e => e.type == EntityType.Player))
                warnings.Add("Missing Player entity definition.");
            if (entities == null || !entities.Any(e => e.type == EntityType.Shadow))
                warnings.Add("Missing Shadow entity definition.");
            if (shadowSpawnDelaySteps <= 0)
                warnings.Add($"Shadow delay steps ({shadowSpawnDelaySteps}) should be greater than 0.");

            return errors.Count == 0;
        }

        private void EnsureIds()
        {
            if (entities != null) EnsureIds(entities, e => e.id, (e, newId) => e.id = newId);
            if (switches != null) EnsureIds(switches, s => s.id, (s, newId) => s.id = newId);
            if (gates != null) EnsureIds(gates, g => g.id, (g, newId) => g.id = newId);
            if (teleports != null) EnsureIds(teleports, t => t.id, (t, newId) => t.id = newId);
        }

        private void Awake()
        {
            CheckAndAssignID();
        }

        private void OnValidate()
        {
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
            if (switches == null) return;
            var switchIds = new HashSet<int>(switches.Select(s => s.id));

            if (teleports != null)
            {
                foreach (var teleport in teleports)
                {
                    if (teleport.switchCondition?.switchIds == null) continue;
                    foreach (var switchId in teleport.switchCondition.switchIds)
                    {
                        if (!switchIds.Contains(switchId))
                        {
                            Debug.LogError(
                                $"Stage '{name}' - Teleport '{teleport.id}': Switch ID {switchId} does not exist.",
                                this);
                        }
                    }
                }
            }

            if (gates != null)
            {
                foreach (var gate in gates)
                {
                    if (gate.switchCondition?.switchIds == null) continue;
                    foreach (var switchId in gate.switchCondition.switchIds)
                    {
                        if (!switchIds.Contains(switchId))
                        {
                            Debug.LogError(
                                $"Stage '{name}' - Gate '{gate.id}': Switch ID {switchId} does not exist.",
                                this);
                        }
                    }
                }
            }
        }

        private static void EnsureIds<T>(
            List<T> items,
            Func<T, int> getId,
            Action<T, int> setId)
        {
            if (items == null) return;
            var usedIds = new HashSet<int>();
            int maxId = 0;

            foreach (var item in items)
            {
                var currentId = getId(item);
                if (currentId <= 0) continue;
                maxId = Math.Max(maxId, currentId);
            }

            foreach (var item in items)
            {
                var currentId = getId(item);
                if (currentId > 0 && usedIds.Add(currentId)) continue;

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

        public CellDefinition() { }

        public CellDefinition(Vector2Int position, CellType type)
        {
            this.position = position;
            this.type = type;
        }
    }

    [Serializable]
    public sealed class EntityDefinition
    {
        public int id;
        public EntityType type;
        public Vector2Int position;

        public EntityDefinition() { }

        public EntityDefinition(int id, EntityType type, Vector2Int position)
        {
            this.id = id;
            this.type = type;
            this.position = position;
        }
    }

    [Serializable]
    public sealed class SwitchDefinition
    {
        public int id;
        public Vector2Int position;
        public SwitchMode mode;
        public bool initialActivated;

        public SwitchDefinition() { }

        public SwitchDefinition(int id, Vector2Int position, SwitchMode mode = SwitchMode.Hold, bool initialActivated = false)
        {
            this.id = id;
            this.position = position;
            this.mode = mode;
            this.initialActivated = initialActivated;
        }
    }

    [Serializable]
    public sealed class GateDefinition
    {
        public int id;
        public Vector2Int position;
        public bool initialOpened;
        public SwitchConditionDefinition switchCondition = new();

        public GateDefinition() { }

        public GateDefinition(int id, Vector2Int position, bool initialOpened = false, SwitchConditionDefinition condition = null)
        {
            this.id = id;
            this.position = position;
            this.initialOpened = initialOpened;
            this.switchCondition = condition ?? new SwitchConditionDefinition();
        }
    }

    [Serializable]
    public sealed class TeleportDefinition
    {
        public int id;
        public Vector2Int positionA;
        public Vector2Int positionB;
        public SwitchConditionDefinition switchCondition = new();
        public bool initialEnabled = true;

        public TeleportDefinition() { }

        public TeleportDefinition(int id, Vector2Int positionA, Vector2Int positionB, bool initialEnabled = true, SwitchConditionDefinition condition = null)
        {
            this.id = id;
            this.positionA = positionA;
            this.positionB = positionB;
            this.initialEnabled = initialEnabled;
            this.switchCondition = condition ?? new SwitchConditionDefinition();
        }
    }

    [Serializable]
    public sealed class SwitchConditionDefinition
    {
        public LinkLogic logic;
        public List<int> switchIds = new();

        public SwitchConditionDefinition() { }

        public SwitchConditionDefinition(LinkLogic logic, IEnumerable<int> ids = null)
        {
            this.logic = logic;
            if (ids != null) switchIds.AddRange(ids);
        }

        public SwitchCondition ToData()
        {
            return new SwitchCondition(logic, switchIds ?? new List<int>());
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

        public StarCondition() { }

        public StarCondition(bool hasTimeLimit, int maxTimeSeconds)
        {
            this.hasTimeLimit = hasTimeLimit;
            this.maxTimeSeconds = maxTimeSeconds;
        }
    }
}
