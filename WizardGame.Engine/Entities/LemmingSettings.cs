namespace WizardGame.Engine;

public readonly record struct LemmingSettings(
    float WalkSpeed,
    float FallSpeed,
    float MaxSafeFall,
    int MaxStepHeightTiles,
    float Width,
    float Height,
    int BuilderSteps,
    float BuilderStepIntervalSeconds,
    float DiggerStepIntervalSeconds,
    float BasherStepIntervalSeconds,
    float MinerStepIntervalSeconds)
{
    public static LemmingSettings Default => new(8f, 48f, 6f, 1, 3f, 3f, 12, 0.6f, 0.4f, 0.4f, 0.4f);

    public void Validate()
    {
        if (WalkSpeed <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(WalkSpeed));
        }

        if (FallSpeed <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(FallSpeed));
        }

        if (MaxSafeFall < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxSafeFall));
        }

        if (MaxStepHeightTiles < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxStepHeightTiles));
        }

        if (Width <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(Width));
        }

        if (Height <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(Height));
        }

        if (BuilderSteps <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(BuilderSteps));
        }

        if (BuilderStepIntervalSeconds <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(BuilderStepIntervalSeconds));
        }

        if (DiggerStepIntervalSeconds <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(DiggerStepIntervalSeconds));
        }

        if (BasherStepIntervalSeconds <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(BasherStepIntervalSeconds));
        }

        if (MinerStepIntervalSeconds <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(MinerStepIntervalSeconds));
        }
    }
}
