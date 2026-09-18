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

        [Tooltip("Rotation applied to the model so it lies flat and faces the camera. Usually (90,0,0) or (-90,0,0).")]
        public Vector3 prefabEuler;

        [Tooltip("Size multiplier. With 'Fit To Footprint' on, this multiplies the auto-fitted size, so leave it at 1 unless you want the model a little bigger or smaller.")]
        public float prefabScale = 1f;

        [Tooltip("Move the model so its own centre sits on the grid cells. Fixes models whose pivot is at a corner, at the base, or off in empty space.")]
        public bool autoCenter = true;

        [Tooltip("Scale the model so it fills its footprint. Turn this off if you want to set the size by hand with 'Prefab Scale'.")]
        public bool fitToFootprint = true;

        [Tooltip("How much of the footprint the model fills. 0.9 leaves a small gap around the edge.")]
        [Range(0.2f, 1f)] public float fitMargin = 0.9f;

        [Tooltip("Extra nudge applied after centring, in world units. X = right, Y = up, Z = towards the camera.")]
        public Vector3 prefabOffset;

        public int Area => width * height;

        void OnValidate()
        {
            if (width < 1) width = 1;
            if (height < 1) height = 1;
            if (prefabScale <= 0f) prefabScale = 1f;
            if (fitMargin <= 0f) fitMargin = 0.9f;
            if (string.IsNullOrEmpty(displayName)) displayName = name;
        }
    }
}
