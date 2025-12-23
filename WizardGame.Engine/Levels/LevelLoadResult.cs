namespace WizardGame.Engine;

public sealed class LevelLoadResult
{
    public LevelLoadResult(Level level, LevelImageData? imageData)
    {
        Level = level ?? throw new ArgumentNullException(nameof(level));
        ImageData = imageData;
    }

    public Level Level { get; }
    public LevelImageData? ImageData { get; }
}
