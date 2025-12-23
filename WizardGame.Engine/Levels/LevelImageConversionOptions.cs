namespace WizardGame.Engine;

public sealed class LevelImageConversionOptions
{
    public int TileSize { get; init; } = 4;
    public int AspectWidth { get; init; } = 16;
    public int AspectHeight { get; init; } = 9;
    public bool EnforceAspectRatio { get; init; } = true;
}
