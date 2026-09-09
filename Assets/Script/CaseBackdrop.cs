using UnityEngine;

namespace CaseFit
{
    [ExecuteAlways]
    public class CaseBackdrop : MonoBehaviour
    {
        public enum FitMode
        {
            StretchToGrid,
            AnchorOnly
        }

        [Header("Grid")]
        [SerializeField] GridLayout3D layout;

        [Header("Fit")]
        [SerializeField] FitMode mode = FitMode.StretchToGrid;
        [SerializeField] Vector2 padding = new(0.4f, 0.4f);
        [SerializeField] float thickness = 0.25f;

        [Header("Placement")]
        [SerializeField] float gapBehindGrid = 0.05f;
        [SerializeField] Vector3 extraOffset;

        void OnEnable() => Fit();
        void LateUpdate() => Fit();

        public void Fit()
        {
            if (layout == null) layout = GetComponentInParent<GridLayout3D>();
            if (layout == null) return;

            Vector2 total = layout.TotalSize;
            Vector3 center = layout.Origin + new Vector3(total.x * 0.5f, -total.y * 0.5f, 0f);

            float depth = gapBehindGrid;
            if (mode == FitMode.StretchToGrid)
            {
                depth += thickness * 0.5f;
                transform.localScale = new Vector3(total.x + padding.x, total.y + padding.y, thickness);
            }

            transform.localPosition = center + new Vector3(0f, 0f, depth) + extraOffset;
        }
    }
}
