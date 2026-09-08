using System.Collections.Generic;
using UnityEngine;

namespace CaseFit
{
    public class TrayArea : MonoBehaviour
    {
        [Header("Layout")]
        [Min(1)] public int columns = 6;
        public Vector2 cellSize = new(1f, 1f);
        public Vector2 spacing = new(0.15f, 0.15f);
        public bool centerHorizontally = true;

        public float StepX => cellSize.x + spacing.x;
        public float StepY => cellSize.y + spacing.y;

        readonly List<ItemView> members = new();

        public IReadOnlyList<ItemView> Members => members;

        public void Clear()
        {
            members.Clear();
        }

        public void Add(ItemView view)
        {
            if (view == null || members.Contains(view)) return;
            members.Add(view);
            view.transform.SetParent(transform, false);
            Rebuild();
        }

        public void Remove(ItemView view)
        {
            if (view == null) return;
            if (members.Remove(view)) Rebuild();
        }

        public void Rebuild()
        {
            members.RemoveAll(view => view == null);

            int column = 0;
            int rowTop = 0;
            int rowHeight = 0;
            float offsetX = centerHorizontally ? -(columns * StepX - spacing.x) * 0.5f : 0f;

            foreach (ItemView view in members)
            {
                ItemInstance item = view.Instance;
                int width = item.Width;
                int height = item.Height;

                if (column + width > columns && column > 0)
                {
                    rowTop += Mathf.Max(1, rowHeight);
                    column = 0;
                    rowHeight = 0;
                }

                float x = column * StepX + ((width - 1) * StepX + cellSize.x) * 0.5f;
                float y = rowTop * StepY + ((height - 1) * StepY + cellSize.y) * 0.5f;

                view.transform.localPosition = new Vector3(offsetX + x, -y, 0f);
                view.ApplyOrientation();

                column += width;
                rowHeight = Mathf.Max(rowHeight, height);
            }
        }
    }
}
