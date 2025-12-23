using System.Numerics;

namespace WizardGame.Engine;

public sealed class GameState
{
    private readonly List<Lemming> _lemmings;

    public GameState(Level level)
        : this(level, LemmingSettings.Default)
    {
    }

    public GameState(Level level, LemmingSettings lemmingSettings)
    {
        Level = level ?? throw new ArgumentNullException(nameof(level));
        lemmingSettings.Validate();
        LemmingSettings = lemmingSettings;
        _lemmings = new List<Lemming>();
        TotalToSpawn = level.TotalLemmings;
        RequiredToSave = level.RequiredToSave;
        SpawnIntervalTicks = level.SpawnIntervalTicks;
        BuildersRemaining = level.BuilderUses;
        DiggersRemaining = level.DiggerUses;
        BashersRemaining = level.BasherUses;
        MinersRemaining = level.MinerUses;
        BombersRemaining = level.BomberUses;
        NextSpawnTick = 0;
    }

    public Level Level { get; }
    public World World => Level.World;
    public IReadOnlyList<Lemming> Lemmings => _lemmings;
    public long Tick { get; internal set; }
    public int Spawned { get; internal set; }
    public int Escaped { get; internal set; }
    public int Dead { get; internal set; }
    public int TotalToSpawn { get; }
    public int RequiredToSave { get; }
    public int SpawnIntervalTicks { get; private set; }
    public int BuildersRemaining { get; private set; }
    public int DiggersRemaining { get; private set; }
    public int BashersRemaining { get; private set; }
    public int MinersRemaining { get; private set; }
    public int BombersRemaining { get; private set; }
    public LemmingSettings LemmingSettings { get; }
    internal long NextSpawnTick { get; set; }
    internal int NextSpawnIndex { get; set; }
    public bool IsObjectiveMet => Escaped >= RequiredToSave;
    public bool AreAllLemmingsResolved => Spawned >= TotalToSpawn && _lemmings.Count == 0;
    public bool CanAdvanceToNextLevel => IsObjectiveMet && AreAllLemmingsResolved;

    internal void AddLemming(Vector2 position, bool facingRight)
    {
        _lemmings.Add(new Lemming(position, facingRight, LemmingSettings));
        Spawned++;
    }

    public void AdjustSpawnIntervalTicks(int delta, long currentTick)
    {
        var updated = Math.Max(1, SpawnIntervalTicks + delta);
        SetSpawnIntervalTicks(updated, currentTick);
    }

    public void SetSpawnIntervalTicks(int ticks, long currentTick)
    {
        if (ticks <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticks));
        }

        SpawnIntervalTicks = ticks;
        var earliestNext = currentTick + SpawnIntervalTicks;
        if (NextSpawnTick > earliestNext)
        {
            NextSpawnTick = earliestNext;
        }
    }

    public bool TryAssignBuilderAt(float worldX, float worldY)
    {
        if (BuildersRemaining <= 0)
        {
            return false;
        }

        for (var i = _lemmings.Count - 1; i >= 0; i--)
        {
            var lemming = _lemmings[i];
            if (!lemming.IsAlive || lemming.Ability == LemmingAbility.Builder)
            {
                continue;
            }

            if (!ContainsPoint(lemming, worldX, worldY))
            {
                continue;
            }

            if (!lemming.HasBuilderSupport(World))
            {
                continue;
            }

            if (lemming.TryStartBuilder())
            {
                BuildersRemaining--;
                return true;
            }
        }

        return false;
    }

    public bool TryAssignDiggerAt(float worldX, float worldY)
    {
        if (DiggersRemaining <= 0)
        {
            return false;
        }

        for (var i = _lemmings.Count - 1; i >= 0; i--)
        {
            var lemming = _lemmings[i];
            if (!lemming.IsAlive || lemming.Ability == LemmingAbility.Digger)
            {
                continue;
            }

            if (!ContainsPoint(lemming, worldX, worldY))
            {
                continue;
            }

            if (!lemming.IsGrounded)
            {
                continue;
            }

            if (!lemming.HasDiggableGround(World))
            {
                continue;
            }

            if (lemming.TryStartDigger())
            {
                DiggersRemaining--;
                return true;
            }
        }

        return false;
    }

    public bool TryAssignBasherAt(float worldX, float worldY)
    {
        if (BashersRemaining <= 0)
        {
            return false;
        }

        for (var i = _lemmings.Count - 1; i >= 0; i--)
        {
            var lemming = _lemmings[i];
            if (!lemming.IsAlive || lemming.Ability == LemmingAbility.Basher)
            {
                continue;
            }

            if (!ContainsPoint(lemming, worldX, worldY))
            {
                continue;
            }

            if (!lemming.IsGrounded)
            {
                continue;
            }

            if (!lemming.HasBashableTerrain(World))
            {
                continue;
            }

            if (lemming.TryStartBasher())
            {
                BashersRemaining--;
                return true;
            }
        }

        return false;
    }

    public bool TryAssignMinerAt(float worldX, float worldY)
    {
        if (MinersRemaining <= 0)
        {
            return false;
        }

        for (var i = _lemmings.Count - 1; i >= 0; i--)
        {
            var lemming = _lemmings[i];
            if (!lemming.IsAlive || lemming.Ability == LemmingAbility.Miner)
            {
                continue;
            }

            if (!ContainsPoint(lemming, worldX, worldY))
            {
                continue;
            }

            if (!lemming.IsGrounded)
            {
                continue;
            }

            if (!lemming.HasMineableTerrain(World))
            {
                continue;
            }

            if (lemming.TryStartMiner())
            {
                MinersRemaining--;
                return true;
            }
        }

        return false;
    }

    public bool TryAssignBomberAt(float worldX, float worldY)
    {
        if (BombersRemaining <= 0)
        {
            return false;
        }

        for (var i = _lemmings.Count - 1; i >= 0; i--)
        {
            var lemming = _lemmings[i];
            if (!lemming.IsAlive || lemming.IsBombing)
            {
                continue;
            }

            if (!ContainsPoint(lemming, worldX, worldY))
            {
                continue;
            }

            if (lemming.TryStartBomber())
            {
                BombersRemaining--;
                return true;
            }
        }

        return false;
    }

    internal int RemoveDead()
    {
        return _lemmings.RemoveAll(static lemming => !lemming.IsAlive);
    }

    internal int RemoveEscaped()
    {
        var removed = 0;
        for (var i = _lemmings.Count - 1; i >= 0; i--)
        {
            var lemming = _lemmings[i];
            if (!lemming.IsAlive)
            {
                continue;
            }

            const float epsilon = 0.001f;
            var left = (int)MathF.Floor(lemming.Position.X);
            var right = (int)MathF.Floor(lemming.Position.X + lemming.Width - epsilon);
            var top = (int)MathF.Floor(lemming.Position.Y);
            var bottom = (int)MathF.Floor(lemming.Position.Y + lemming.Height - epsilon);

            var escaped = false;
            for (var y = top; y <= bottom && !escaped; y++)
            {
                for (var x = left; x <= right; x++)
                {
                    if (Level.IsExitTile(x, y))
                    {
                        escaped = true;
                        break;
                    }
                }
            }

            if (escaped)
            {
                _lemmings.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }

    private static bool ContainsPoint(Lemming lemming, float worldX, float worldY)
    {
        return worldX >= lemming.Position.X
            && worldX <= lemming.Position.X + lemming.Width
            && worldY >= lemming.Position.Y
            && worldY <= lemming.Position.Y + lemming.Height;
    }
}
