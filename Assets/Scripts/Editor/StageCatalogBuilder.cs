using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ChasingLight.Editor
{
    /// <summary>
    /// Giữ StageCatalog đồng bộ với mọi StageAsset trong project (kể cả sau khi merge nhánh chứa
    /// Assets/Data/Levels). Chạy tự động khi StageAsset được import và trước khi build.
    /// </summary>
    public sealed class StageCatalogBuilder : AssetPostprocessor, IPreprocessBuildWithReport
    {
        private const string CatalogPath = "Assets/Data/StageCatalog.asset";

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report) => Rebuild();

        [MenuItem("Chasing Light/Rebuild Stage Catalog")]
        public static void Rebuild()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<StageCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            var stages = AssetDatabase.FindAssets("t:StageAsset")
                .Select(g => AssetDatabase.LoadAssetAtPath<StageAsset>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(s => s != null)
                .OrderBy(s => s.Difficulty)
                .ThenBy(s => s.StageNumber)
                .ToList();

            if (SameContent(catalog.Stages, stages)) return;

            catalog.SetStages(stages);
            AssetDatabase.SaveAssetIfDirty(catalog);
            Debug.Log($"[StageCatalogBuilder] Cập nhật StageCatalog: {stages.Count} màn.");
        }

        private static bool SameContent(IReadOnlyList<StageAsset> a, List<StageAsset> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
                if (a[i] != b[i]) return false;
            return true;
        }

        private static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool touched = imported.Concat(deleted).Concat(moved).Concat(movedFrom)
                .Any(p => p.EndsWith(".asset") && p.StartsWith("Assets/Data/Levels"));

            if (touched)
                EditorApplication.delayCall += Rebuild;
        }
    }
}

