namespace CaseFit
{
    public class ItemInstance
    {
        public ItemDefinition Definition;
        public int Column = -1;
        public int Row = -1;
        public bool Rotated;

        public bool IsPlaced => Column >= 0 && Row >= 0;
        public int Width => Rotated ? Definition.height : Definition.width;
        public int Height => Rotated ? Definition.width : Definition.height;
        public int Area => Definition.width * Definition.height;
    }

    public class InventoryGrid
    {
        public int Size { get; }

        readonly ItemInstance[,] cells;

        public InventoryGrid(int size)
        {
            Size = size;
            cells = new ItemInstance[size, size];
        }

        public bool InBounds(int column, int row)
        {
            return column >= 0 && row >= 0 && column < Size && row < Size;
        }

        public ItemInstance GetAt(int column, int row)
        {
            return InBounds(column, row) ? cells[column, row] : null;
        }

        public bool CanPlace(ItemInstance item, int column, int row)
        {
            if (item == null || item.Definition == null) return false;
            int width = item.Width;
            int height = item.Height;
            if (column < 0 || row < 0 || column + width > Size || row + height > Size) return false;

            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    ItemInstance occupant = cells[column + c, row + r];
                    if (occupant != null && occupant != item) return false;
                }
            }
            return true;
        }

        public bool Place(ItemInstance item, int column, int row)
        {
            if (!CanPlace(item, column, row)) return false;
            Remove(item);
            for (int r = 0; r < item.Height; r++)
                for (int c = 0; c < item.Width; c++)
                    cells[column + c, row + r] = item;
            item.Column = column;
            item.Row = row;
            return true;
        }

        public void Remove(ItemInstance item)
        {
            if (item == null || !item.IsPlaced) return;
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    if (cells[c, r] == item) cells[c, r] = null;
            item.Column = -1;
            item.Row = -1;
        }

        public bool HasAnyValidPlacement(ItemInstance item)
        {
            if (item == null) return false;
            bool originalRotation = item.Rotated;
            int attempts = item.Definition.canRotate ? 2 : 1;

            for (int attempt = 0; attempt < attempts; attempt++)
            {
                for (int row = 0; row < Size; row++)
                {
                    for (int column = 0; column < Size; column++)
                    {
                        if (!CanPlace(item, column, row)) continue;
                        item.Rotated = originalRotation;
                        return true;
                    }
                }
                item.Rotated = !item.Rotated;
            }

            item.Rotated = originalRotation;
            return false;
        }

        public int FreeCellCount()
        {
            int free = 0;
            foreach (ItemInstance cell in cells)
                if (cell == null) free++;
            return free;
        }

        public bool IsFull => FreeCellCount() == 0;

        public void Clear()
        {
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                    cells[c, r] = null;
        }
    }
}
