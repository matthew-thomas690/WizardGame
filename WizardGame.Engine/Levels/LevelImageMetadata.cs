namespace WizardGame.Engine;

public sealed class LevelImageMetadata
{
    public int? TotalLemmings { get; init; }
    public int? RequiredToSave { get; init; }
    public int? SpawnIntervalTicks { get; init; }
    public int? BuilderUses { get; init; }
    public int? DiggerUses { get; init; }
    public int? BasherUses { get; init; }
    public int? MinerUses { get; init; }
}
