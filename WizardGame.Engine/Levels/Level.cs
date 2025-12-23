using System.Numerics;

namespace WizardGame.Engine;

public sealed class Level
{
    private readonly List<Vector2> _spawnPoints;
    private readonly List<GridPoint> _exitTiles;
    private readonly HashSet<GridPoint> _exitSet;

    public Level(
        World world,
        IEnumerable<Vector2> spawnPoints,
        IEnumerable<GridPoint> exitTiles,
        int totalLemmings,
        int requiredToSave,
        int spawnIntervalTicks,
        int builderUses,
        int diggerUses,
        int basherUses,
        int minerUses,
        int bomberUses)
    {
        if (totalLemmings <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalLemmings));
        }

        if (requiredToSave < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredToSave));
        }

        if (requiredToSave > totalLemmings)
        {
            throw new ArgumentOutOfRangeException(nameof(requiredToSave));
        }

        if (spawnIntervalTicks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(spawnIntervalTicks));
        }

        if (builderUses < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(builderUses));
        }

        if (diggerUses < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(diggerUses));
        }

        if (basherUses < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(basherUses));
        }

        if (minerUses < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minerUses));
        }

        if (bomberUses < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(bomberUses));
        }

        World = world ?? throw new ArgumentNullException(nameof(world));
        _spawnPoints = new List<Vector2>(spawnPoints ?? throw new ArgumentNullException(nameof(spawnPoints)));
        _exitTiles = new List<GridPoint>(exitTiles ?? throw new ArgumentNullException(nameof(exitTiles)));
        _exitSet = new HashSet<GridPoint>(_exitTiles);
        TotalLemmings = totalLemmings;
        RequiredToSave = requiredToSave;
        SpawnIntervalTicks = spawnIntervalTicks;
        BuilderUses = builderUses;
        DiggerUses = diggerUses;
        BasherUses = basherUses;
        MinerUses = minerUses;
        BomberUses = bomberUses;
    }

    public World World { get; }
    public IReadOnlyList<Vector2> SpawnPoints => _spawnPoints;
    public IReadOnlyList<GridPoint> ExitTiles => _exitTiles;
    public int TotalLemmings { get; }
    public int RequiredToSave { get; }
    public int SpawnIntervalTicks { get; }
    public int BuilderUses { get; }
    public int DiggerUses { get; }
    public int BasherUses { get; }
    public int MinerUses { get; }
    public int BomberUses { get; }
    public int Width => World.Width;
    public int Height => World.Height;

    public bool IsExitTile(int x, int y)
    {
        return _exitSet.Contains(new GridPoint(x, y));
    }
}
