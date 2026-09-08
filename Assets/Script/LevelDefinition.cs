using System;
using System.Collections.Generic;
using UnityEngine;

namespace CaseFit
{
    [CreateAssetMenu(fileName = "Level", menuName = "Case Fit/Level Definition")]
    public class LevelDefinition : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public ItemDefinition item;
            [Min(1)] public int count = 1;
        }

        [Header("Case")]
        [Min(2)] public int gridSize = 3;

        [Header("Pressure")]
        public float timeLimit = 60f;
        [Min(1)] public int parMoves = 6;

        [Header("Briefing")]
        public string title = "Level";
        [TextArea(2, 4)] public string brief;

        [Header("Items")]
        public List<Entry> items = new();

        public int CellCount => gridSize * gridSize;

        public int TotalItemArea
        {
            get
            {
                int total = 0;
                foreach (Entry entry in items)
                    if (entry != null && entry.item != null)
                        total += entry.item.Area * Mathf.Max(1, entry.count);
                return total;
            }
        }

        public int ItemCount
        {
            get
            {
                int total = 0;
                foreach (Entry entry in items)
                    if (entry != null && entry.item != null)
                        total += Mathf.Max(1, entry.count);
                return total;
            }
        }

        public bool IsExactFit => TotalItemArea == CellCount;

        void OnValidate()
        {
            if (gridSize < 2) gridSize = 2;
            if (items.Count == 0) return;
            if (!IsExactFit)
                Debug.LogWarning($"[Case Fit] {name}: item area {TotalItemArea} does not match {CellCount} cells.", this);
        }
    }
}
