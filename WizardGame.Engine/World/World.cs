namespace WizardGame.Engine;

public sealed class World
{
    private readonly TileType[] _tiles;

    public World(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width));
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height));
        }

        Width = width;
        Height = height;
        _tiles = new TileType[width * height];
    }

    public int Width { get; }
    public int Height { get; }

    public bool InBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public TileType GetTile(int x, int y)
    {
        if (!InBounds(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Tile {x},{y} is out of bounds.");
        }

        return _tiles[(y * Width) + x];
    }

    public void SetTile(int x, int y, TileType tile)
    {
        if (!InBounds(x, y))
        {
            throw new ArgumentOutOfRangeException(nameof(x), $"Tile {x},{y} is out of bounds.");
        }

        _tiles[(y * Width) + x] = tile;
    }

    public bool IsSolid(int x, int y)
    {
        if (!InBounds(x, y))
        {
            return true;
        }

        return _tiles[(y * Width) + x] == TileType.Solid;
    }
}
