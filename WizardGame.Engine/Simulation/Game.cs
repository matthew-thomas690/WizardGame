using System.Numerics;

namespace WizardGame.Engine;

public sealed class Game
{
    public Game(GameState state)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public GameState State { get; }

    public void Update(GameTime time, InputState input)
    {
        State.Tick = time.Tick;

        if (State.Spawned < State.TotalToSpawn && time.Tick >= State.NextSpawnTick)
        {
            var spawnPoint = ResolveSpawnPoint(State.Level, State.NextSpawnIndex);
            var facingRight = State.NextSpawnIndex % 2 == 0;
            State.AddLemming(spawnPoint, facingRight);
            State.NextSpawnIndex++;
            State.NextSpawnTick = time.Tick + State.SpawnIntervalTicks;
        }

        foreach (var lemming in State.Lemmings)
        {
            lemming.Update(State.World, time.DeltaSeconds);
        }

        var escaped = State.RemoveEscaped();
        if (escaped > 0)
        {
            State.Escaped += escaped;
        }

        var removed = State.RemoveDead();
        if (removed > 0)
        {
            State.Dead += removed;
        }
    }

    private static Vector2 ResolveSpawnPoint(Level level, int spawnIndex)
    {
        if (level.SpawnPoints.Count == 0)
        {
            return new System.Numerics.Vector2(1f, 1f);
        }

        return level.SpawnPoints[spawnIndex % level.SpawnPoints.Count];
    }
}
