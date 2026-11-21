using System.Linq;
using System.Collections.Generic;
using ChasingLight.Gameplay.Views;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ChasingLight
{
    public class GridView : MonoBehaviour
    {
        [Header("Tilemap Layer (Nền tĩnh)")]
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private TileBase floorTile;
        [SerializeField] private TileBase ladderTile;
        [SerializeField] private TileBase startTile;
        [SerializeField] private TileBase poleTile;
        [SerializeField] private TileBase poleTileBlock;

        [Header("Dynamic Prefabs")]
        [SerializeField] private Transform mechanismContainer;
        [SerializeField] private GateView gatePrefab;
        [SerializeField] private SwitchView switchPrefab;
        [SerializeField] private TeleportView teleportPrefab;
        [SerializeField] private GoalView goalPrefab;

        [Header("Mechanism Palettes")]
        [Tooltip("Element 0 = Yellow (default), Element 1 = Blue. Add more palettes if needed.")]
        [SerializeField] private List<MechanismPalette> mechanismPalettes = new();

        [Header("Teleport Switch Sprites")]
        [Tooltip("Sprite used for switches controlling teleport gates.")]
        [SerializeField] private Sprite teleportSwitchUnpressed;
        [SerializeField] private Sprite teleportSwitchPressed;

        [Header("Grid Config")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 cellOffset = new Vector2(0.5f, 0f);
        
        private readonly Dictionary<int, GateView> _gateViews = new();
        private readonly Dictionary<int, SwitchView> _switchViews = new();
        private readonly Dictionary<int, List<TeleportView>> _teleportViews = new();
        private readonly List<GoalView> _goalViews = new();

        public Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(
                (gridPos.x + cellOffset.x) * cellSize,
                (gridPos.y + cellOffset.y) * cellSize,
                0f
            );
        }

        public void BuildMap(StageData stageData)
        {
            ClearAll();

            if (stageData == null)
            {
                Debug.LogWarning("[GridView] StageData bị null, không thể vẽ map.");
                return;
            }

            BuildStaticTiles(stageData.Grid);

            SpawnMechanisms(stageData);
        }

        private void BuildStaticTiles(GridData grid)
        {
            if (groundTilemap == null || grid == null) return;

            var effectivePoleTile = poleTile != null ? poleTile : floorTile;
            var effectivePoleBlockTile = poleTileBlock != null ? poleTileBlock : floorTile;
            var effectiveStartTile = startTile != null ? startTile : floorTile;

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var pos = new Vector2Int(x, y);
                    var type = grid.GetCell(pos).Type;
                    var tilePos = new Vector3Int(x, y, 0);

                    var terrainTile = GetDirectTerrainTile(type, effectivePoleTile, effectivePoleBlockTile);
                    if (terrainTile != null)
                    {
                        groundTilemap.SetTile(tilePos, terrainTile);
                        continue;
                    }

                    if (type == CellType.Start)
                    {
                        groundTilemap.SetTile(tilePos, effectiveStartTile);
                        if (NeedsPillarSupport(grid, pos, y))
                        {
                            groundTilemap.SetTile(new Vector3Int(x, y, 1), effectivePoleBlockTile);
                        }
                        continue;
                    }

                    if (type is CellType.Switch or CellType.Teleport or CellType.Goal)
                    {
                        var supportTile = GetMechanismSupportTile(grid, pos, y, effectivePoleBlockTile);
                        if (supportTile != null)
                        {
                            groundTilemap.SetTile(tilePos, supportTile);
                        }
                    }
                }
            }
        }

        private TileBase GetDirectTerrainTile(CellType type, TileBase resolvedPoleTile, TileBase resolvedPoleBlockTile) => type switch
        {
            CellType.Floor => floorTile,
            CellType.Ladder => ladderTile,
            CellType.Pole => resolvedPoleTile,
            CellType.PoleTile => resolvedPoleBlockTile,
            _ => null
        };

        private TileBase GetMechanismSupportTile(GridData grid, Vector2Int pos, int y, TileBase resolvedPoleBlockTile)
        {
            var below = pos + Vector2Int.down;
            if (grid.InBounds(below) && grid.GetCell(below).Type == CellType.Ladder)
                return ladderTile;

            if (NeedsPillarSupport(grid, pos, y))
                return resolvedPoleBlockTile;

            return null;
        }

        private static bool NeedsPillarSupport(GridData grid, Vector2Int pos, int y)
        {
            var below = pos + Vector2Int.down;
            return y == 0 || (grid.InBounds(below) && grid.GetCell(below).Type is CellType.Pole or CellType.PoleTile);
        }

        private void SpawnMechanisms(StageData stageData)
        {
            Transform container = (mechanismContainer != null && mechanismContainer.gameObject.scene.IsValid())
                ? mechanismContainer
                : transform;

            // Automatically calculate color groups based on Switch-Bridge links
            MechanismGroupingHelper.CalculateGroups(
                stageData.Gates, stageData.Switches,
                out var gateGroups, out var switchGroups);

            // Spawn Gates
            if (gatePrefab != null && stageData.Gates != null)
            {
                foreach (var gate in stageData.Gates)
                {
                    Vector3 worldPos = GridToWorld(gate.Position);
                    var gateView = Instantiate(gatePrefab, worldPos, Quaternion.identity, container);
                    gateView.Initialize(gate.Id, gate.InitialSolid);

                    // Apply sprites according to color group
                    int groupId = gateGroups.TryGetValue(gate.Id, out var gid) ? gid : 0;
                    ApplyGatePalette(gateView, groupId);

                    _gateViews[gate.Id] = gateView;
                }
            }

            // Collect switches linked to teleports
            var teleportSwitchIds = new HashSet<int>();
            if (stageData.Teleports != null)
            {
                foreach (var tp in stageData.Teleports)
                {
                    if (tp.Condition?.SwitchIds != null)
                    {
                        foreach (var sid in tp.Condition.SwitchIds)
                        {
                            teleportSwitchIds.Add(sid);
                        }
                    }
                }
            }

            // Spawn Switches
            if (switchPrefab != null && stageData.Switches != null)
            {
                foreach (var sw in stageData.Switches)
                {
                    Vector3 worldPos = GridToWorld(sw.Position);
                    var switchView = Instantiate(switchPrefab, worldPos, Quaternion.identity, container);
                    switchView.Initialize(sw.Id, sw.InitialActivated);

                    if (teleportSwitchIds.Contains(sw.Id) && (teleportSwitchUnpressed != null || teleportSwitchPressed != null))
                    {
                        switchView.SetSprites(teleportSwitchUnpressed, teleportSwitchPressed);
                    }
                    else
                    {
                        // Apply sprites according to color group
                        int groupId = switchGroups.TryGetValue(sw.Id, out var sid) ? sid : 0;
                        ApplySwitchPalette(switchView, groupId);
                    }

                    _switchViews[sw.Id] = switchView;
                }
            }

            // Spawn Teleports
            if (teleportPrefab != null && stageData.Teleports != null)
            {
                foreach (var tp in stageData.Teleports)
                {
                    var list = new List<TeleportView>();

                    Vector3 posA = GridToWorld(tp.PositionA);
                    var viewA = Instantiate(teleportPrefab, posA, Quaternion.identity, container);
                    viewA.Initialize(tp.Id, tp.InitialEnabled);
                    list.Add(viewA);

                    Vector3 posB = GridToWorld(tp.PositionB);
                    var viewB = Instantiate(teleportPrefab, posB, Quaternion.identity, container);
                    viewB.Initialize(tp.Id, tp.InitialEnabled);
                    list.Add(viewB);

                    _teleportViews[tp.Id] = list;
                }
            }

            // Spawn Goals
            if (goalPrefab != null && stageData.Grid != null)
            {
                var grid = stageData.Grid;
                for (int y = 0; y < grid.Height; y++)
                {
                    for (int x = 0; x < grid.Width; x++)
                    {
                        var pos = new Vector2Int(x, y);
                        if (grid.GetCell(pos).Type == CellType.Goal)
                        {
                            Vector3 worldPos = GridToWorld(pos);
                            var goalView = Instantiate(goalPrefab, worldPos, Quaternion.identity, container);
                            _goalViews.Add(goalView);
                        }
                    }
                }
            }
        }

        private void ApplyGatePalette(GateView gateView, int groupId)
        {
            if (mechanismPalettes == null || mechanismPalettes.Count == 0) return;
            int index = groupId % mechanismPalettes.Count;
            var palette = mechanismPalettes[index];

            if (palette.gateNonSolid != null || palette.gateSolid != null)
            {
                gateView.SetSprites(palette.gateNonSolid, palette.gateSolid);
            }
        }

        private void ApplySwitchPalette(SwitchView switchView, int groupId)
        {
            if (mechanismPalettes == null || mechanismPalettes.Count == 0) return;
            int index = groupId % mechanismPalettes.Count;
            var palette = mechanismPalettes[index];

            if (palette.switchUnpressed != null || palette.switchPressed != null)
            {
                switchView.SetSprites(palette.switchUnpressed, palette.switchPressed);
            }
        }

        public void PlayGoalWin()
        {
            foreach (var goal in _goalViews)
            {
                if (goal != null)
                    goal.PlayWinAnimation();
            }
        }

        public void UpdateGate(int gateId, bool isOpen)
        {
            if (_gateViews.TryGetValue(gateId, out var view))
                view.SetOpened(isOpen);
        }

        public void UpdateSwitch(int switchId, bool isActivated)
        {
            if (_switchViews.TryGetValue(switchId, out var view))
                view.SetActivated(isActivated);
        }

        public void UpdateTeleport(int teleportId, bool isEnabled)
        {
            if (_teleportViews.TryGetValue(teleportId, out var list))
            {
                foreach (var view in list)
                    view.SetEnabled(isEnabled);
            }
        }

        public void ClearAll()
        {
            if (groundTilemap != null)
                groundTilemap.ClearAllTiles();

            DestroyViews(_gateViews.Values);
            _gateViews.Clear();

            DestroyViews(_switchViews.Values);
            _switchViews.Clear();

            foreach (var list in _teleportViews.Values)
                DestroyViews(list);
            _teleportViews.Clear();

            DestroyViews(_goalViews);
            _goalViews.Clear();

            if (mechanismContainer != null)
            {
                for (int i = mechanismContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(mechanismContainer.GetChild(i).gameObject);
                }
            }
        }

        private static void DestroyViews<T>(IEnumerable<T> views) where T : Component
        {
            foreach (var view in views)
            {
                if (view != null)
                    Destroy(view.gameObject);
            }
        }
    }
}
