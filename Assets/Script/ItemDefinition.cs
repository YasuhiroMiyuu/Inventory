using UnityEngine;

namespace CaseFit
{
    [CreateAssetMenu(fileName = "Item", menuName = "Case Fit/Item Definition")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string displayName = "Item";

        [Header("Grid Footprint")]
        [Min(1)] public int width = 1;
        [Min(1)] public int height = 1;
        public bool canRotate = true;

        [Header("Visual")]
        public GameObject prefab;
        public Vector3 prefabEuler;
        public float prefabScale = 1f;

        public int Area => width * height;

        void OnValidate()
        {
            if (width < 1) width = 1;
            if (height < 1) height = 1;
            if (prefabScale <= 0f) prefabScale = 1f;
            if (string.IsNullOrEmpty(displayName)) displayName = name;
        }
    }
}
