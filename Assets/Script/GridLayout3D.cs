using UnityEngine;

namespace CaseFit
{
    [ExecuteAlways]
    public class GridLayout3D : MonoBehaviour
    {
        [Header("Grid")]
        [Min(1)] public int columns = 3;
        [Min(1)] public int rows = 3;

        [Header("Cell")]
        public Vector2 cellSize = new(1f, 1f);
        public Vector2 spacing = new(0.05f, 0.05f);

        [Header("Alignment")]
        public bool centerOnPivot = true;
        public bool arrangeOwnChildren;

        [Header("Gizmos")]
        public bool drawGizmos = true;
        public Color gizmoColor = new(0.79f, 0.64f, 0.15f, 0.8f);

        public float StepX => cellSize.x + spacing.x;
        public float StepY => cellSize.y + spacing.y;

        public Vector2 TotalSize => new(
            columns * cellSize.x + Mathf.Max(0, columns - 1) * spacing.x,
            rows * cellSize.y + Mathf.Max(0, rows - 1) * spacing.y);

        public Vector3 Origin
        {
            get
            {
                if (!centerOnPivot) return Vector3.zero;
                Vector2 total = TotalSize;
                return new Vector3(-total.x * 0.5f, total.y * 0.5f, 0f);
            }
        }

        public Plane SurfacePlane => new(transform.forward, transform.position);

        public Vector3 CellToLocal(int column, int row, int widthInCells = 1, int heightInCells = 1)
        {
            float x = column * StepX + ((widthInCells - 1) * StepX + cellSize.x) * 0.5f;
            float y = row * StepY + ((heightInCells - 1) * StepY + cellSize.y) * 0.5f;
            return Origin + new Vector3(x, -y, 0f);
        }

        public Vector3 CellToWorld(int column, int row, int widthInCells = 1, int heightInCells = 1)
        {
            return transform.TransformPoint(CellToLocal(column, row, widthInCells, heightInCells));
        }

        public bool LocalToCell(Vector3 localPoint, out int column, out int row)
        {
            Vector3 p = localPoint - Origin;
            column = Mathf.FloorToInt((p.x + spacing.x * 0.5f) / StepX);
            row = Mathf.FloorToInt((-p.y + spacing.y * 0.5f) / StepY);
            return column >= 0 && row >= 0 && column < columns && row < rows;
        }

        public bool WorldToCell(Vector3 worldPoint, out int column, out int row)
        {
            return LocalToCell(transform.InverseTransformPoint(worldPoint), out column, out row);
        }

        public void LocalToCellForFootprint(Vector3 centerLocal, int widthInCells, int heightInCells,
                                            out int column, out int row)
        {
            Vector3 p = centerLocal - Origin;
            column = Mathf.RoundToInt((p.x - ((widthInCells - 1) * StepX + cellSize.x) * 0.5f) / StepX);
            row = Mathf.RoundToInt((-p.y - ((heightInCells - 1) * StepY + cellSize.y) * 0.5f) / StepY);
        }

        public bool RaycastSurface(Ray ray, out Vector3 worldPoint)
        {
            worldPoint = default;
            if (!SurfacePlane.Raycast(ray, out float enter)) return false;
            worldPoint = ray.GetPoint(enter);
            return true;
        }

        public Vector3 SizeOfFootprint(int widthInCells, int heightInCells)
        {
            return new Vector3(
                widthInCells * StepX - spacing.x,
                heightInCells * StepY - spacing.y,
                1f);
        }

        public void ArrangeChildrenInGrid()
        {
            if (columns < 1) return;
            int slot = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (!child.gameObject.activeSelf) continue;
                child.localPosition = CellToLocal(slot % columns, slot / columns);
                child.localRotation = Quaternion.identity;
                slot++;
            }
        }

        public void ArrangeChildren(Transform parent)
        {
            if (columns < 1 || parent == null) return;
            int slot = 0;
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (!child.gameObject.activeSelf) continue;
                child.SetPositionAndRotation(
                    CellToWorld(slot % columns, slot / columns),
                    transform.rotation);
                slot++;
            }
        }

        public void NormalizeChildRoot(Transform child)
        {
            if (child == null || child.parent != transform) return;
            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }

        void OnTransformChildrenChanged()
        {
            if (arrangeOwnChildren) ArrangeChildrenInGrid();
        }

        void OnValidate()
        {
            if (columns < 1) columns = 1;
            if (rows < 1) rows = 1;
            if (arrangeOwnChildren) ArrangeChildrenInGrid();
        }

        void OnDrawGizmos()
        {
            if (!drawGizmos) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = gizmoColor;
            for (int row = 0; row < rows; row++)
                for (int column = 0; column < columns; column++)
                    Gizmos.DrawWireCube(CellToLocal(column, row),
                                        new Vector3(cellSize.x, cellSize.y, 0.01f));
        }
    }
}
