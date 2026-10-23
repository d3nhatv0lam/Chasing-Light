using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Chuyển đổi tọa độ lưới Core và tọa độ World của Unity.
    /// Không tự quyết định luật di chuyển; GameRules chịu trách nhiệm phần đó.
    /// </summary>
    public sealed class GridManager : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _cellSize = 1f;
        [SerializeField] private CellVisualData _cellVisualData; // Asset chứa mapping Prefab
        [SerializeField] private Transform _cellsParent;

        private GridData _gridData;

        public float CellSize => _cellSize;
        public GridData Data => _gridData;

        /// <summary>Liên kết dữ liệu lưới runtime của Core.</summary>
        public void Initialize(GridData gridData)
        {
            if (gridData == null)
            {
                Debug.LogError("GridManager: GridData chưa được gán.", this);
                _gridData = null;
                return;
            }

            _gridData = gridData;
            _cellSize = Mathf.Max(0.01f, _cellSize);

            GenerateVisualCells();
        }

        /// <summary>Sinh các Prefab ô hiển thị từ dữ liệu GridData</summary>
        public void GenerateVisualCells()
        {
            if (_gridData == null || _cellVisualData == null) return;

            _cellVisualData.Initialize();

            // Xóa các Cell cũ nếu có
            if (_cellsParent != null)
            {
                for (int i = _cellsParent.childCount - 1; i >= 0; i--)
                {
                    Destroy(_cellsParent.GetChild(i).gameObject);
                }
            }

            Transform parent = _cellsParent != null ? _cellsParent : transform;

            // Duyệt qua toàn bộ tọa độ trên lưới bản đồ
            for (int x = 0; x < _gridData.Width; x++)
            {
                for (int y = 0; y < _gridData.Height; y++)
                {
                    Vector2Int gridPos = new Vector2Int(x, y);
                    CellData cell = _gridData.GetCell(gridPos);

                    if (cell.Type == CellType.Empty) continue; // Không sinh cell nếu là ô trống

                    GameObject prefab = _cellVisualData.GetPrefab(cell.Type);
                    if (prefab != null)
                    {
                        Vector3 worldPos = GridToWorld(gridPos);
                        GameObject instance = Instantiate(prefab, worldPos, Quaternion.identity, parent);
                        instance.name = $"Cell_{x}_{y}_{cell.Type}";
                    }
                }
            }
        }

        /// <summary>Chuyển tọa độ grid thành world, lấy Transform của Grid làm gốc.</summary>
        public Vector3 GridToWorld(Vector2Int position)
        {
            return transform.position + new Vector3(
                position.x * _cellSize,
                position.y * _cellSize,
                0f);
        }

        /// <summary>Chuyển tọa độ world về ô grid gần nhất.</summary>
        public Vector2Int WorldToGrid(Vector3 position)
        {
            Vector3 localPosition = position - transform.position;
            return new Vector2Int(
                Mathf.RoundToInt(localPosition.x / _cellSize),
                Mathf.RoundToInt(localPosition.y / _cellSize));
        }

        public bool InBounds(Vector2Int position)
        {
            return _gridData != null && _gridData.InBounds(position);
        }

        public CellData GetCell(Vector2Int position)
        {
            if (_gridData == null)
            {
                return new CellData(position, CellType.Empty);
            }

            return _gridData.GetCell(position);
        }
    }
}
