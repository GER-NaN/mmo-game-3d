namespace MmoGame3d.Rules.Maps;

/// <summary>
/// Where a player has been in one zone, for the map: world.md says the map shows only
/// that. The zone is cut into square cells; a cell is discovered once the player has
/// been within SightRadius of it. Stored and sent as a small bit set (a town of 100 by
/// 100 m is 100 cells, 13 bytes). Positions are zone-local; the zone's box is centred
/// on its origin.
/// </summary>
public class Discovery
{
    public const float CellSize = 10f;
    public const float SightRadius = 15f;

    private readonly bool[] _cells;

    public Discovery(float width, float depth)
    {
        Columns = Math.Max(1, (int)Math.Ceiling(width / CellSize));
        Rows = Math.Max(1, (int)Math.Ceiling(depth / CellSize));
        Width = width;
        Depth = depth;
        _cells = new bool[Columns * Rows];
    }

    public int Columns { get; }
    public int Rows { get; }
    public float Width { get; }
    public float Depth { get; }

    public bool IsDiscovered(int column, int row)
    {
        return column >= 0 && row >= 0 && column < Columns && row < Rows && _cells[(row * Columns) + column];
    }

    // Marks every cell whose centre is within sight of the spot. True when anything was
    // new, which is when the owner needs telling.
    public bool See(float x, float z)
    {
        bool changed = false;

        for (int row = 0; row < Rows; row++)
        {
            for (int column = 0; column < Columns; column++)
            {
                float centreX = -Width / 2f + ((column + 0.5f) * CellSize);
                float centreZ = -Depth / 2f + ((row + 0.5f) * CellSize);
                float dx = centreX - x;
                float dz = centreZ - z;
                int index = (row * Columns) + column;

                if (!_cells[index] && (dx * dx) + (dz * dz) <= SightRadius * SightRadius)
                {
                    _cells[index] = true;
                    changed = true;
                }
            }
        }

        return changed;
    }

    public int DiscoveredCount()
    {
        int count = 0;

        foreach (bool cell in _cells)
        {
            if (cell)
            {
                count++;
            }
        }

        return count;
    }

    public byte[] ToBytes()
    {
        byte[] bytes = new byte[(_cells.Length + 7) / 8];

        for (int i = 0; i < _cells.Length; i++)
        {
            if (_cells[i])
            {
                bytes[i / 8] |= (byte)(1 << (i % 8));
            }
        }

        return bytes;
    }

    // Bits past the grid, from a zone that has since shrunk, are ignored.
    public void Load(byte[] bytes)
    {
        for (int i = 0; i < _cells.Length && (i / 8) < bytes.Length; i++)
        {
            _cells[i] = (bytes[i / 8] & (1 << (i % 8))) != 0;
        }
    }
}
