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
