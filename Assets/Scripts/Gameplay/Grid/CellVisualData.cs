using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChasingLight
{
    [Serializable]
    public struct CellVisualMapping
    {
        public CellType type;
        public GameObject cellPrefab; // Prefab tương ứng với loại ô
    }

    [CreateAssetMenu(fileName = "CellVisualData", menuName = "Chasing Light/Cell Visual Data")]
    public class CellVisualData : ScriptableObject
    {
        [SerializeField] private List<CellVisualMapping> _mappings = new List<CellVisualMapping>();

        private Dictionary<CellType, GameObject> _mappingDict;

        public void Initialize()
        {
            _mappingDict = new Dictionary<CellType, GameObject>();
            foreach (var item in _mappings)
            {
                if (!_mappingDict.ContainsKey(item.type) && item.cellPrefab != null)
                {
                    _mappingDict.Add(item.type, item.cellPrefab);
                }
            }
        }

        public GameObject GetPrefab(CellType type)
        {
            if (_mappingDict == null) Initialize();

            if (_mappingDict.TryGetValue(type, out var prefab))
            {
                return prefab;
            }

            return null;
        }
    }
}