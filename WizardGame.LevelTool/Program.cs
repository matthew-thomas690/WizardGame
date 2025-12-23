using WizardGame.Engine;

internal static class Program
{
    private const int ExitSuccess = 0;
    private const int ExitFailure = 1;

    private static int Main(string[] args)
    {
        if (args.Length == 0 || HasHelpFlag(args))
        {
            PrintUsage();
            return ExitSuccess;
        }

        if (args.Length < 2)
        {
            Console.Error.WriteLine("Input and output paths are required.");
            PrintUsage();
            return ExitFailure;
        }

        var inputPath = args[0];
        var outputPath = args[1];

        int? totalLemmings = null;
        int? requiredToSave = null;
        int? spawnIntervalTicks = null;
        int? builderUses = null;
        int? diggerUses = null;
        int? basherUses = null;
        int? minerUses = null;

        for (var index = 2; index < args.Length; index++)
        {
            var arg = args[index];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine($"Unknown argument '{arg}'.");
                return ExitFailure;
            }

            if (!TryReadOptionValue(arg, args, ref index, out var value))
            {
                Console.Error.WriteLine($"Missing value for '{arg}'.");
                return ExitFailure;
            }

            switch (GetOptionName(arg))
            {
                case "--green":
                    Console.Error.WriteLine("The --green option is no longer supported; transparency is used for background detection.");
                    break;
                case "--lemmings":
                    if (!TryParsePositiveInt(value, out totalLemmings))
                    {
                        Console.Error.WriteLine("Lemmings must be a positive integer.");
                        return ExitFailure;
                    }

                    break;
                case "--save":
                    if (!int.TryParse(value, out var required) || required < 0)
                    {
                        Console.Error.WriteLine("Save count must be a non-negative integer.");
                        return ExitFailure;
                    }

                    requiredToSave = required;
                    break;
                case "--spawn-interval":
                    if (!TryParsePositiveInt(value, out spawnIntervalTicks))
                    {
                        Console.Error.WriteLine("Spawn interval must be a positive integer (ticks).");
                        return ExitFailure;
                    }

                    break;
                case "--builders":
                    if (!int.TryParse(value, out var builders) || builders < 0)
                    {
                        Console.Error.WriteLine("Builder uses must be a non-negative integer.");
                        return ExitFailure;
                    }

                    builderUses = builders;
                    break;
                case "--diggers":
                    if (!int.TryParse(value, out var diggers) || diggers < 0)
                    {
                        Console.Error.WriteLine("Digger uses must be a non-negative integer.");
                        return ExitFailure;
                    }

                    diggerUses = diggers;
                    break;
                case "--bashers":
                    if (!int.TryParse(value, out var bashers) || bashers < 0)
                    {
                        Console.Error.WriteLine("Basher uses must be a non-negative integer.");
                        return ExitFailure;
                    }

                    basherUses = bashers;
                    break;
                case "--miners":
                    if (!int.TryParse(value, out var miners) || miners < 0)
                    {
                        Console.Error.WriteLine("Miner uses must be a non-negative integer.");
                        return ExitFailure;
                    }

                    minerUses = miners;
                    break;
                default:
                    Console.Error.WriteLine($"Unknown option '{arg}'.");
                    return ExitFailure;
            }
        }

        if (totalLemmings.HasValue && requiredToSave.HasValue && requiredToSave > totalLemmings)
        {
            Console.Error.WriteLine("Save count cannot exceed total lemmings.");
            return ExitFailure;
        }

        LevelImageMetadata? metadata = null;
        if (totalLemmings.HasValue || requiredToSave.HasValue || spawnIntervalTicks.HasValue || builderUses.HasValue || diggerUses.HasValue || basherUses.HasValue || minerUses.HasValue)
        {
            metadata = new LevelImageMetadata
            {
                TotalLemmings = totalLemmings,
                RequiredToSave = requiredToSave,
                SpawnIntervalTicks = spawnIntervalTicks,
                BuilderUses = builderUses,
                DiggerUses = diggerUses,
                BasherUses = basherUses,
                MinerUses = minerUses
            };
        }

        var options = new LevelImageConversionOptions();

        try
        {
            var levelData = LevelImageConverter.ConvertFromFile(inputPath, options, metadata);
            var json = LevelImageJsonSerializer.Serialize(levelData);
            File.WriteAllText(outputPath, json);
            Console.WriteLine($"Wrote {outputPath} ({levelData.Width}x{levelData.Height} tiles).");
            return ExitSuccess;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return ExitFailure;
        }
    }

    private static bool HasHelpFlag(string[] args)
    {
        foreach (var arg in args)
        {
            if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("-h", StringComparison.OrdinalIgnoreCase)
                || arg.Equals("/?", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  WizardGame.LevelTool <input.png> <output.level> [options]");
        Console.WriteLine();
        Console.WriteLine("Options:");
        Console.WriteLine("  --lemmings N          Total lemmings in the level");
        Console.WriteLine("  --save N              Lemmings required to save");
        Console.WriteLine("  --spawn-interval N    Spawn interval in ticks");
        Console.WriteLine("  --builders N          Builder ability uses available");
        Console.WriteLine("  --diggers N           Digger ability uses available");
        Console.WriteLine("  --bashers N           Basher ability uses available");
        Console.WriteLine("  --miners N            Miner ability uses available");
    }

    private static bool TryReadOptionValue(
        string arg,
        string[] args,
        ref int index,
        out string value)
    {
        var parts = arg.Split('=', 2, StringSplitOptions.TrimEntries);
        if (parts.Length == 2)
        {
            value = parts[1];
            return true;
        }

        if (index + 1 >= args.Length)
        {
            value = string.Empty;
            return false;
        }

        index++;
        value = args[index];
        return true;
    }

    private static string GetOptionName(string arg)
    {
        var parts = arg.Split('=', 2, StringSplitOptions.TrimEntries);
        return parts[0];
    }

    private static bool TryParsePositiveInt(string value, out int? result)
    {
        if (!int.TryParse(value, out var parsed) || parsed <= 0)
        {
            result = null;
            return false;
        }

        result = parsed;
        return true;
    }
}
