namespace WizardGame.Engine;

public readonly struct GameTime
{
    public GameTime(long tick, TimeSpan delta)
    {
        Tick = tick;
        Delta = delta;
    }

    public long Tick { get; }
    public TimeSpan Delta { get; }
    public float DeltaSeconds => (float)Delta.TotalSeconds;
}
