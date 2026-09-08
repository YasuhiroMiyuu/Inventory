using UnityEngine;

namespace CaseFit
{
    [RequireComponent(typeof(GridLayout3D))]
    public class InventoryCase : MonoBehaviour
    {
        [Header("Roots")]
        public Transform cellRoot;
        public Transform itemRoot;

        [Header("Cell Visual")]
        public GameObject cellPrefab;
        public bool scaleCellToCellSize = true;

        public GridLayout3D Layout { get; private set; }
        public InventoryGrid Grid { get; private set; }
        public Transform ItemRoot => itemRoot != null ? itemRoot : transform;

        void Awake() => EnsureLayout();

        void EnsureLayout()
        {
            if (Layout == null) Layout = GetComponent<GridLayout3D>();
        }

        public void Build(int size)
        {
            EnsureLayout();
            Layout.columns = size;
            Layout.rows = size;
            Layout.NormalizeChildRoot(cellRoot);
            Layout.NormalizeChildRoot(itemRoot);
            Grid = new InventoryGrid(size);
            BuildCellVisuals();
        }

        void BuildCellVisuals()
        {
            if (cellRoot == null || cellPrefab == null) return;

            for (int i = cellRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = cellRoot.GetChild(i).gameObject;
                child.transform.SetParent(null, false);
                Destroy(child);
            }

            int total = Layout.columns * Layout.rows;
            for (int i = 0; i < total; i++)
            {
                GameObject cell = Instantiate(cellPrefab, cellRoot);
                cell.name = $"Cell_{i % Layout.columns}_{i / Layout.columns}";
                if (scaleCellToCellSize)
                    cell.transform.localScale = new Vector3(Layout.cellSize.x, Layout.cellSize.y, 1f);
            }

            Layout.ArrangeChildren(cellRoot);
        }

        public void Take(ItemInstance item)
        {
            Grid?.Remove(item);
        }

        public bool TryPlace(ItemView view, int column, int row)
        {
            if (Grid == null || view == null) return false;
            if (!Grid.Place(view.Instance, column, row)) return false;
            Snap(view);
            return true;
        }

        public void Snap(ItemView view)
        {
            ItemInstance item = view.Instance;
            view.transform.SetParent(ItemRoot, false);
            view.transform.localPosition = Layout.CellToLocal(item.Column, item.Row, item.Width, item.Height);
            view.ApplyOrientation();
        }
    }
}
