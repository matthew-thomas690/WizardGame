using System.Drawing;

namespace WizardGame.Engine;

public static class LevelImageConverter
{
    public static LevelImageData ConvertFromFile(
        string inputPath,
        LevelImageConversionOptions? options = null,
        LevelImageMetadata? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            throw new ArgumentException("Input path is required.", nameof(inputPath));
        }

        options ??= new LevelImageConversionOptions();
        ValidateOptions(options);
        ValidateMetadata(metadata);

        var imageBytes = File.ReadAllBytes(inputPath);
        using var stream = new MemoryStream(imageBytes);
        using var bitmap = new Bitmap(stream);

        var mimeType = GetMimeTypeFromExtension(Path.GetExtension(inputPath));
        return ConvertBitmap(bitmap, imageBytes, mimeType, options, metadata);
    }

    private static LevelImageData ConvertBitmap(
        Bitmap bitmap,
        byte[] imageBytes,
        string mimeType,
        LevelImageConversionOptions options,
        LevelImageMetadata? metadata)
    {
        if (bitmap.Width <= 0 || bitmap.Height <= 0)
        {
            throw new InvalidOperationException("Image dimensions must be greater than zero.");
        }

        if (options.EnforceAspectRatio)
        {
            var left = (long)bitmap.Width * options.AspectHeight;
            var right = (long)bitmap.Height * options.AspectWidth;
            if (left != right)
            {
                throw new InvalidOperationException(
                    $"Image must be {options.AspectWidth}:{options.AspectHeight} but is {bitmap.Width}:{bitmap.Height}.");
            }
        }

        if (bitmap.Width % options.TileSize != 0 || bitmap.Height % options.TileSize != 0)
        {
            throw new InvalidOperationException(
                $"Image dimensions must be divisible by tile size ({options.TileSize}).");
        }

        var tilesWide = bitmap.Width / options.TileSize;
        var tilesHigh = bitmap.Height / options.TileSize;
        var tiles = new bool[tilesHigh][];
        var markerMask = new bool[bitmap.Width * bitmap.Height];
        var spawnPoints = new List<GridPoint>();
        var exitTiles = new List<GridPoint>();

        DetectMarkers(bitmap, options, markerMask, spawnPoints, exitTiles);

        for (var tileY = 0; tileY < tilesHigh; tileY++)
        {
            var row = new bool[tilesWide];
            for (var tileX = 0; tileX < tilesWide; tileX++)
            {
                row[tileX] = IsSolidTile(bitmap, tileX, tileY, options, markerMask);
            }

            tiles[tileY] = row;
        }

        return new LevelImageData
        {
            TileSize = options.TileSize,
            Width = tilesWide,
            Height = tilesHigh,
            Tiles = tiles,
            OverlayImageBase64 = Convert.ToBase64String(imageBytes),
            OverlayImageMimeType = mimeType,
            SourceImageWidth = bitmap.Width,
            SourceImageHeight = bitmap.Height,
            SpawnPoints = spawnPoints.Count > 0 ? spawnPoints.ToArray() : null,
            ExitTiles = exitTiles.Count > 0 ? exitTiles.ToArray() : null,
            Metadata = metadata
        };
    }

    private static bool IsSolidTile(
        Bitmap bitmap,
        int tileX,
        int tileY,
        LevelImageConversionOptions options,
        bool[] markerMask)
    {
        var startX = tileX * options.TileSize;
        var startY = tileY * options.TileSize;
        var solidPixels = 0;
        var totalPixels = options.TileSize * options.TileSize;

        for (var y = 0; y < options.TileSize; y++)
        {
            for (var x = 0; x < options.TileSize; x++)
            {
                var pixelX = startX + x;
                var pixelY = startY + y;
                if (markerMask[(pixelY * bitmap.Width) + pixelX])
                {
                    continue;
                }

                var color = bitmap.GetPixel(pixelX, pixelY);
                if (!IsTransparent(color))
                {
                    solidPixels++;
                }
            }
        }

        return solidPixels * 2 >= totalPixels;
    }

    private static void DetectMarkers(
        Bitmap bitmap,
        LevelImageConversionOptions options,
        bool[] markerMask,
        List<GridPoint> spawnPoints,
        List<GridPoint> exitTiles)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var markerSize = options.TileSize;
        var tilesWide = width / markerSize;
        var tilesHigh = height / markerSize;
        var spawnColor = Color.FromArgb(0, 0, 0);
        var exitColor = Color.FromArgb(255, 0, 0);
        var spawnSet = new HashSet<GridPoint>();
        var exitSet = new HashSet<GridPoint>();

        for (var y = 0; y <= height - markerSize; y++)
        {
            for (var x = 0; x <= width - markerSize; x++)
            {
                if (IsMarkerBlock(bitmap, x, y, markerSize, spawnColor))
                {
                    MarkMarkerPixels(markerMask, width, x, y, markerSize);
                    var tile = SnapMarkerToTile(x, y, markerSize, tilesWide, tilesHigh);
                    if (spawnSet.Add(tile))
                    {
                        spawnPoints.Add(tile);
                    }

                    continue;
                }

                if (IsMarkerBlock(bitmap, x, y, markerSize, exitColor))
                {
                    MarkMarkerPixels(markerMask, width, x, y, markerSize);
                    var tile = SnapMarkerToTile(x, y, markerSize, tilesWide, tilesHigh);
                    if (exitSet.Add(tile))
                    {
                        exitTiles.Add(tile);
                    }
                }
            }
        }
    }

    private static bool IsMarkerBlock(
        Bitmap bitmap,
        int startX,
        int startY,
        int size,
        Color markerColor)
    {
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var color = bitmap.GetPixel(startX + x, startY + y);
                if (color.A != 255 || !IsExactColor(color, markerColor))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void MarkMarkerPixels(
        bool[] markerMask,
        int width,
        int startX,
        int startY,
        int size)
    {
        for (var y = 0; y < size; y++)
        {
            var row = (startY + y) * width;
            for (var x = 0; x < size; x++)
            {
                markerMask[row + startX + x] = true;
            }
        }
    }

    private static GridPoint SnapMarkerToTile(
        int pixelX,
        int pixelY,
        int tileSize,
        int tilesWide,
        int tilesHigh)
    {
        var tileX = SnapAxis(pixelX, tileSize, tilesWide);
        var tileY = SnapAxis(pixelY, tileSize, tilesHigh);
        return new GridPoint(tileX, tileY);
    }

    private static int SnapAxis(int pixel, int tileSize, int tilesCount)
    {
        var minTile = pixel / tileSize;
        var maxTile = (pixel + tileSize - 1) / tileSize;
        if (minTile == maxTile)
        {
            return minTile;
        }

        var shifted = minTile + 1;
        if (shifted < tilesCount)
        {
            return shifted;
        }

        return minTile;
    }

    private static bool IsExactColor(Color pixel, Color color)
    {
        return pixel.R == color.R && pixel.G == color.G && pixel.B == color.B;
    }

    private static bool IsTransparent(Color pixel)
    {
        return pixel.A == 0;
    }

    private static void ValidateOptions(LevelImageConversionOptions options)
    {
        if (options.TileSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Tile size must be greater than zero.");
        }

        if (options.AspectWidth <= 0 || options.AspectHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Aspect ratio must be greater than zero.");
        }
    }

    private static void ValidateMetadata(LevelImageMetadata? metadata)
    {
        if (metadata is null)
        {
            return;
        }

        if (metadata.TotalLemmings.HasValue && metadata.TotalLemmings <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(metadata), "Total lemmings must be greater than zero.");
        }

        if (metadata.RequiredToSave.HasValue && metadata.RequiredToSave < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(metadata), "Required lemmings cannot be negative.");
        }

        if (metadata.SpawnIntervalTicks.HasValue && metadata.SpawnIntervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(metadata), "Spawn interval must be greater than zero.");
        }

        if (metadata.BuilderUses.HasValue && metadata.BuilderUses < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(metadata), "Builder uses cannot be negative.");
        }

        if (metadata.DiggerUses.HasValue && metadata.DiggerUses < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(metadata), "Digger uses cannot be negative.");
        }

        if (metadata.BasherUses.HasValue && metadata.BasherUses < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(metadata), "Basher uses cannot be negative.");
        }

        if (metadata.MinerUses.HasValue && metadata.MinerUses < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(metadata), "Miner uses cannot be negative.");
        }

        if (metadata.TotalLemmings.HasValue
            && metadata.RequiredToSave.HasValue
            && metadata.RequiredToSave > metadata.TotalLemmings)
        {
            throw new ArgumentOutOfRangeException(
                nameof(metadata),
                "Required lemmings cannot exceed total lemmings.");
        }
    }

    private static string GetMimeTypeFromExtension(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".bmp" => "image/bmp",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}
