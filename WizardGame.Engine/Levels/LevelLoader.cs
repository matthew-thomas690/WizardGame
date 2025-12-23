using System.Numerics;

namespace WizardGame.Engine;

public static class LevelLoader
{
    public static Level LoadLevel(string path)
    {
        return Load(path).Level;
    }

    public static LevelLoadResult Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Level path is required.", nameof(path));
        }

        var content = File.ReadAllText(path);
        var extension = Path.GetExtension(path);
        return LoadFromContent(content, extension);
    }

    public static LevelLoadResult LoadFromContent(string content, string? extension = null)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (IsJsonExtension(extension) || LooksLikeJson(content))
        {
            return LoadFromJson(content);
        }

        return LoadFromText(content);
    }

    public static LevelLoadResult LoadFromText(string text)
    {
        if (text is null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        var lines = text.Replace("\r", string.Empty).Split('\n');
        var level = LevelParser.Parse(lines);
        return new LevelLoadResult(level, null);
    }

    public static LevelLoadResult LoadFromLines(IReadOnlyList<string> lines)
    {
        var level = LevelParser.Parse(lines);
        return new LevelLoadResult(level, null);
    }

    public static LevelLoadResult LoadFromJson(string json)
    {
        var data = LevelImageJsonSerializer.Deserialize(json);
        var level = BuildLevelFromImageData(data);
        return new LevelLoadResult(level, data);
    }

    private static Level BuildLevelFromImageData(LevelImageData data)
    {
        if (data is null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (data.Tiles is null || data.Tiles.Length == 0)
        {
            throw new FormatException("Level JSON must include a non-empty tile grid.");
        }

        var height = data.Height > 0 ? data.Height : data.Tiles.Length;
        var width = data.Width > 0 ? data.Width : data.Tiles[0]?.Length ?? 0;

        if (width <= 0 || height <= 0)
        {
            throw new FormatException("Level JSON must define positive width and height.");
        }

        if (data.Tiles.Length != height)
        {
            throw new FormatException($"Tile row count {data.Tiles.Length} does not match height {height}.");
        }

        for (var y = 0; y < height; y++)
        {
            var row = data.Tiles[y];
            if (row is null || row.Length != width)
            {
                throw new FormatException($"Tile row {y} length does not match width {width}.");
            }
        }

        var world = new World(width, height);
        for (var y = 0; y < height; y++)
        {
            var row = data.Tiles[y];
            for (var x = 0; x < width; x++)
            {
                if (row[x])
                {
                    world.SetTile(x, y, TileType.Solid);
                }
            }
        }

        var spawnPoints = new List<Vector2>();
        var exitTiles = new List<GridPoint>();

        if (data.SpawnPoints is not null)
        {
            foreach (var point in data.SpawnPoints)
            {
                EnsureInBounds(point, width, height, "Spawn");
                spawnPoints.Add(new Vector2(point.X, point.Y));
            }
        }

        if (data.ExitTiles is not null)
        {
            foreach (var point in data.ExitTiles)
            {
                EnsureInBounds(point, width, height, "Exit");
                exitTiles.Add(point);
            }
        }

        var totalLemmings = data.Metadata?.TotalLemmings ?? Math.Max(1, spawnPoints.Count);
        var requiredToSave = data.Metadata?.RequiredToSave ?? totalLemmings;
        var spawnIntervalTicks = data.Metadata?.SpawnIntervalTicks ?? 60;
        var builderUses = data.Metadata?.BuilderUses ?? 0;
        var diggerUses = data.Metadata?.DiggerUses ?? 0;
        var basherUses = data.Metadata?.BasherUses ?? 0;
        var minerUses = data.Metadata?.MinerUses ?? 0;

        return new Level(world, spawnPoints, exitTiles, totalLemmings, requiredToSave, spawnIntervalTicks, builderUses, diggerUses, basherUses, minerUses);
    }

    private static void EnsureInBounds(GridPoint point, int width, int height, string label)
    {
        if (point.X < 0 || point.X >= width || point.Y < 0 || point.Y >= height)
        {
            throw new FormatException($"{label} point {point.X},{point.Y} is outside {width}x{height} level bounds.");
        }
    }

    private static bool IsJsonExtension(string? extension)
    {
        return string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".level", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".lvl", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeJson(string content)
    {
        for (var i = 0; i < content.Length; i++)
        {
            var ch = content[i];
            if (char.IsWhiteSpace(ch))
            {
                continue;
            }

            return ch == '{';
        }

        return false;
    }
}
