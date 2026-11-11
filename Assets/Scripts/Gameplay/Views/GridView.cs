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
        [SerializeField] private TileBase goalTile;
        [SerializeField] private TileBase startTile;
        [SerializeField] private TileBase poleTile;
        [SerializeField] private TileBase poleTileBlock;

        [Header("Dynamic Prefabs (Cơ quan động)")]
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

        private void Awake()
        {
#if UNITY_EDITOR
            EnsureDefaultSprites();
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureDefaultSprites();
        }

        private void Reset()
        {
            EnsureDefaultSprites();
        }

        private static Sprite LoadSpriteAsset(string path)
        {
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            var assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
            return assets != null ? System.Linq.Enumerable.OfType<Sprite>(assets).FirstOrDefault() : null;
        }

        private void EnsureDefaultSprites()
        {
            if (teleportSwitchUnpressed == null)
                teleportSwitchUnpressed = LoadSpriteAsset("Assets/Sprites/Mechanisms/Teleport gate button.png");
            if (teleportSwitchPressed == null)
                teleportSwitchPressed = LoadSpriteAsset("Assets/Sprites/Mechanisms/Teleport gate button pressed.png");

            if (poleTile == null)
                poleTile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Prefabs/PoleTile.asset");
            if (poleTileBlock == null)
                poleTileBlock = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Prefabs/PoleTileBlock.asset");

            while (mechanismPalettes.Count < 2)
            {
                mechanismPalettes.Add(new MechanismPalette());
            }

            // Palette 0: Yellow
            var p0 = mechanismPalettes[0];
            if (p0.switchUnpressed == null)
                p0.switchUnpressed = LoadSpriteAsset("Assets/Sprites/Mechanisms/Yellow Button Tile.png");
            if (p0.switchPressed == null)
                p0.switchPressed = LoadSpriteAsset("Assets/Sprites/Mechanisms/Yellow Button Tile - Pressed.png");
            if (p0.gateNonSolid == null)
                p0.gateNonSolid = LoadSpriteAsset("Assets/Sprites/Mechanisms/Yellow non solid tile.png");
            if (p0.gateSolid == null)
                p0.gateSolid = LoadSpriteAsset("Assets/Sprites/Mechanisms/Yellow solid tile.png");
            mechanismPalettes[0] = p0;

            // Palette 1: Blue
            var p1 = mechanismPalettes[1];
            if (p1.switchUnpressed == null)
                p1.switchUnpressed = LoadSpriteAsset("Assets/Sprites/Mechanisms/Blue Button Tile.png");
            if (p1.switchPressed == null)
                p1.switchPressed = LoadSpriteAsset("Assets/Sprites/Mechanisms/Blue Button Tile - Pressed.png");
            if (p1.gateNonSolid == null)
                p1.gateNonSolid = LoadSpriteAsset("Assets/Sprites/Mechanisms/Blue solid tile.png");
            if (p1.gateSolid == null)
                p1.gateSolid = LoadSpriteAsset("Assets/Sprites/Mechanisms/Blue non solid tile.png");
            mechanismPalettes[1] = p1;
        }
#endif

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

            for (int y = 0; y < grid.Height; y++)
            {
                for (int x = 0; x < grid.Width; x++)
                {
                    var pos = new Vector2Int(x, y);
                    var cell = grid.GetCell(pos);
                    var tilePos = new Vector3Int(x, y, 0);
                    
                    var belowPos = pos + Vector2Int.down;
                    bool hasLadderBelow = grid.InBounds(belowPos) && grid.GetCell(belowPos).Type == CellType.Ladder;

                    switch (cell.Type)
                    {
                        case CellType.Floor:
                            if (floorTile != null) groundTilemap.SetTile(tilePos, floorTile);
                            break;
                        case CellType.Ladder:
                            if (ladderTile != null) groundTilemap.SetTile(tilePos, ladderTile);
                            break;
                        case CellType.Start:
                            var sTile = startTile != null ? startTile : floorTile;
                            if (sTile != null) groundTilemap.SetTile(tilePos, sTile);
                            break;
                        case CellType.Pole:
                            var pTile = poleTile;
#if UNITY_EDITOR
                            if (pTile == null)
                                pTile = poleTile = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Prefabs/PoleTile.asset");
                            if (pTile == null)
                            {
                                var s = LoadSpriteAsset("Assets/Sprites/Environment/Pole.png");
                                if (s != null)
                                {
                                    var t = ScriptableObject.CreateInstance<Tile>();
                                    t.sprite = s;
                                    pTile = poleTile = t;
                                }
                            }
#endif
                            if (pTile == null) pTile = floorTile;
                            if (pTile != null) groundTilemap.SetTile(tilePos, pTile);
                            break;
                        case CellType.PoleTile:
                            var ptTile = poleTileBlock;
#if UNITY_EDITOR
                            if (ptTile == null)
                                ptTile = poleTileBlock = UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Prefabs/PoleTileBlock.asset");
                            if (ptTile == null)
                            {
                                var s = LoadSpriteAsset("Assets/Sprites/Environment/Pole tile.png");
                                if (s != null)
                                {
                                    var t = ScriptableObject.CreateInstance<Tile>();
                                    t.sprite = s;
                                    ptTile = poleTileBlock = t;
                                }
                            }
#endif
                            if (ptTile == null) ptTile = floorTile;
                            if (ptTile != null) groundTilemap.SetTile(tilePos, ptTile);
                            break;
                        case CellType.Switch:
                        case CellType.Gate:
                        case CellType.Teleport:
                        case CellType.Goal:
                            if (hasLadderBelow && ladderTile != null)
                            {
                                // Vẽ 1 đốt thang đâm thẳng lên chân cơ quan/Goal
                                groundTilemap.SetTile(tilePos, ladderTile);
                            }
                            else if (cell.Type == CellType.Goal && goalPrefab == null && goalTile != null)
                            {
                                // Fallback nếu chưa gán goalPrefab
                                groundTilemap.SetTile(tilePos, goalTile);
                            }
                            break;
                    }
                }
            }
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
            
            foreach (var gate in _gateViews.Values)
                if (gate != null) Destroy(gate.gameObject);
            _gateViews.Clear();
            
            foreach (var sw in _switchViews.Values)
                if (sw != null) Destroy(sw.gameObject);
            _switchViews.Clear();
            
            foreach (var list in _teleportViews.Values)
                foreach (var tp in list)
                    if (tp != null) Destroy(tp.gameObject);
            _teleportViews.Clear();

            foreach (var goal in _goalViews)
                if (goal != null) Destroy(goal.gameObject);
            _goalViews.Clear();
            
            if (mechanismContainer != null)
            {
                for (int i = mechanismContainer.childCount - 1; i >= 0; i--)
                {
                    Destroy(mechanismContainer.GetChild(i).gameObject);
                }
            }
        }
    }
}
