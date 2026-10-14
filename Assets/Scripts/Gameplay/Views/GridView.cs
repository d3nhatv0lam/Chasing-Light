using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace ChasingLight.Gameplay.Views
{
    public class GridView : MonoBehaviour
    {
        [Header("Tilemap Layer (Nền tĩnh)")]
        [SerializeField] private Tilemap groundTilemap;
        [SerializeField] private TileBase floorTile;
        [SerializeField] private TileBase ladderTile;
        [SerializeField] private TileBase goalTile;
        [SerializeField] private TileBase startTile;

        [Header("Dynamic Prefabs (Cơ quan động)")]
        [SerializeField] private Transform mechanismContainer;
        [SerializeField] private GateView gatePrefab;
        [SerializeField] private SwitchView switchPrefab;
        [SerializeField] private TeleportView teleportPrefab;

        [Header("Grid Config")]
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 cellOffset = new Vector2(0.5f, 0.5f);

        private readonly Dictionary<int, GateView> _gateViews = new();
        private readonly Dictionary<int, SwitchView> _switchViews = new();
        private readonly Dictionary<int, List<TeleportView>> _teleportViews = new();

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

            // 1. Vẽ các ô tĩnh lên Tilemap
            BuildStaticTiles(stageData.Grid);

            // 2. Sinh các Prefab cơ quan đặc biệt
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

                    switch (cell.Type)
                    {
                        case CellType.Floor:
                            if (floorTile != null) groundTilemap.SetTile(tilePos, floorTile);
                            break;
                        case CellType.Ladder:
                            if (ladderTile != null) groundTilemap.SetTile(tilePos, ladderTile);
                            break;
                        case CellType.Goal:
                            if (goalTile != null) groundTilemap.SetTile(tilePos, goalTile);
                            break;
                        case CellType.Start:
                            var sTile = startTile != null ? startTile : floorTile;
                            if (sTile != null) groundTilemap.SetTile(tilePos, sTile);
                            break;
                    }
                }
            }
        }

        private void SpawnMechanisms(StageData stageData)
        {
            Transform container = mechanismContainer != null ? mechanismContainer : transform;

            // Spawn Gates
            if (gatePrefab != null && stageData.Gates != null)
            {
                foreach (var gate in stageData.Gates)
                {
                    Vector3 worldPos = GridToWorld(gate.Position);
                    var gateView = Instantiate(gatePrefab, worldPos, Quaternion.identity, container);
                    gateView.Initialize(gate.Id, gate.InitialOpened);
                    _gateViews[gate.Id] = gateView;
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

            _gateViews.Clear();
            _switchViews.Clear();
            _teleportViews.Clear();

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
