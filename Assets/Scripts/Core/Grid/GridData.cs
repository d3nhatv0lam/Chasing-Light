using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace ChasingLight
{
    public class GridData
    {
        // số lượng ô cột
        public int Height { get; }

        // số lượng ô hàng
        public int Width { get; }

        private readonly Dictionary<Vector2Int, CellData> _cells;

        public GridData(int height, int width, [CanBeNull] IEnumerable<CellData> cells)
        {
            Height = height;
            Width = width;

            _cells = new Dictionary<Vector2Int, CellData>();

            if (cells is not null)
            {
                foreach (var cell in cells)
                {
                    _cells.Add(cell.Position, cell);
                }
            }
        }

        public CellData GetCell(Vector2Int position)
        {
            if (_cells.TryGetValue(position, out var cell))
                return cell;

            return new CellData(position, CellType.Empty);
        }

        public bool InBounds(Vector2Int position)
        {
            return position.x >= 0 
                   && position.x < Width 
                   && position.y >= 0 
                   && position.y < Height;
        }
    }
}