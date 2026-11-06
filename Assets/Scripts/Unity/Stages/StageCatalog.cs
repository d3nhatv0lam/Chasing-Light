using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ChasingLight
{
    /// <summary>
    /// Danh mục các StageAsset để scene chọn màn liệt kê theo độ khó mà không phụ thuộc
    /// vào việc gán tay từng asset. Danh sách được cập nhật tự động bởi StageCatalogBuilder (Editor)
    /// mỗi khi StageAsset được import / merge, và trước khi build.
    /// </summary>
    [CreateAssetMenu(fileName = "StageCatalog", menuName = "Chasing Light/Stage Catalog")]
    public sealed class StageCatalog : ScriptableObject
    {
        [SerializeField] private List<StageAsset> stages = new();

        public IReadOnlyList<StageAsset> Stages => stages;

        public IEnumerable<StageAsset> GetStages(Difficulty difficulty)
        {
            return stages
                .Where(s => s != null && s.Difficulty == difficulty)
                .OrderBy(s => s.StageNumber)
                .ThenBy(s => s.name, System.StringComparer.OrdinalIgnoreCase);
        }

#if UNITY_EDITOR
        public void SetStages(List<StageAsset> newStages)
        {
            stages = newStages;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}

