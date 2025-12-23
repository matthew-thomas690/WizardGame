namespace WizardGame.Engine;

public sealed class LevelImageData
{
    public int TileSize { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool[][] Tiles { get; init; } = Array.Empty<bool[]>();
    public string OverlayImageBase64 { get; init; } = string.Empty;
    public string OverlayImageMimeType { get; init; } = "application/octet-stream";
    public int SourceImageWidth { get; init; }
    public int SourceImageHeight { get; init; }
    public GridPoint[]? SpawnPoints { get; init; }
    public GridPoint[]? ExitTiles { get; init; }
    public LevelImageMetadata? Metadata { get; init; }
}
