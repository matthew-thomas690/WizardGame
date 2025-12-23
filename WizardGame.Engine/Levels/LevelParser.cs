using System.Numerics;

namespace WizardGame.Engine;

public static class LevelParser
{
    public static Level Parse(IReadOnlyList<string> lines)
    {
        if (lines is null)
        {
            throw new ArgumentNullException(nameof(lines));
        }

        if (lines.Count == 0)
        {
            throw new ArgumentException("Level data must contain at least one row.", nameof(lines));
        }

        var totalLemmings = -1;
        var requiredToSave = -1;
        var spawnIntervalTicks = 60;
        var builderUses = 0;
        var diggerUses = 0;
        var basherUses = 0;
        var minerUses = 0;
        var gridLines = new List<string>();

        var index = 0;
        while (index < lines.Count)
        {
            var line = lines[index] ?? string.Empty;
            if (line.Length == 0)
            {
                index++;
                continue;
            }

            if (!line.StartsWith("@", StringComparison.Ordinal))
            {
                break;
            }

            ParseHeaderLine(line[1..], ref totalLemmings, ref requiredToSave, ref spawnIntervalTicks, ref builderUses, ref diggerUses, ref basherUses, ref minerUses);
            index++;
        }

        for (; index < lines.Count; index++)
        {
            var line = lines[index] ?? string.Empty;
            if (line.Length == 0)
            {
                continue;
            }

            gridLines.Add(line);
        }

        if (gridLines.Count == 0)
        {
            throw new ArgumentException("Level data must contain at least one row.", nameof(lines));
        }

        var width = gridLines[0].Length;
        if (width == 0)
        {
            throw new ArgumentException("Level rows must be non-empty.", nameof(lines));
        }

        var world = new World(width, gridLines.Count);
        var spawns = new List<Vector2>();
        var exits = new List<GridPoint>();

        for (var y = 0; y < gridLines.Count; y++)
        {
            var line = gridLines[y] ?? string.Empty;
            if (line.Length != width)
            {
                throw new FormatException($"Line {y} has length {line.Length} but expected {width}.");
            }

            for (var x = 0; x < width; x++)
            {
                var token = line[x];
                switch (token)
                {
                    case '#':
                        world.SetTile(x, y, TileType.Solid);
                        break;
                    case 'S':
                        spawns.Add(new Vector2(x, y));
                        break;
                    case 'E':
                        exits.Add(new GridPoint(x, y));
                        break;
                    case '.':
                    case ' ':
                        break;
                    default:
                        throw new FormatException($"Unknown level token '{token}' at {x},{y}.");
                }
            }
        }

        if (totalLemmings < 0)
        {
            totalLemmings = Math.Max(1, spawns.Count);
        }

        if (requiredToSave < 0)
        {
            requiredToSave = totalLemmings;
        }

        if (requiredToSave > totalLemmings)
        {
            throw new FormatException("Required lemmings cannot exceed total lemmings.");
        }

        if (spawnIntervalTicks <= 0)
        {
            throw new FormatException("Spawn interval must be greater than zero.");
        }

        return new Level(world, spawns, exits, totalLemmings, requiredToSave, spawnIntervalTicks, builderUses, diggerUses, basherUses, minerUses);
    }

    private static void ParseHeaderLine(
        string line,
        ref int totalLemmings,
        ref int requiredToSave,
        ref int spawnIntervalTicks,
        ref int builderUses,
        ref int diggerUses,
        ref int basherUses,
        ref int minerUses)
    {
        var parts = line.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || parts[0].Length == 0)
        {
            throw new FormatException($"Invalid header line '{line}'.");
        }

        if (!int.TryParse(parts[1], out var value))
        {
            throw new FormatException($"Invalid header value '{parts[1]}'.");
        }

        var key = parts[0].ToLowerInvariant();
        switch (key)
        {
            case "lemmings":
            case "total":
                if (value <= 0)
                {
                    throw new FormatException("Total lemmings must be greater than zero.");
                }

                totalLemmings = value;
                break;
            case "save":
            case "required":
                if (value < 0)
                {
                    throw new FormatException("Required lemmings cannot be negative.");
                }

                requiredToSave = value;
                break;
            case "spawn_interval":
                if (value <= 0)
                {
                    throw new FormatException("Spawn interval must be greater than zero.");
                }

                spawnIntervalTicks = value;
                break;
            case "builders":
            case "builder_uses":
                if (value < 0)
                {
                    throw new FormatException("Builder uses cannot be negative.");
                }

                builderUses = value;
                break;
            case "diggers":
            case "digger_uses":
                if (value < 0)
                {
                    throw new FormatException("Digger uses cannot be negative.");
                }

                diggerUses = value;
                break;
            case "bashers":
            case "basher_uses":
                if (value < 0)
                {
                    throw new FormatException("Basher uses cannot be negative.");
                }

                basherUses = value;
                break;
            case "miners":
            case "miner_uses":
                if (value < 0)
                {
                    throw new FormatException("Miner uses cannot be negative.");
                }

                minerUses = value;
                break;
            default:
                throw new FormatException($"Unknown header key '{parts[0]}'.");
        }
    }
}
