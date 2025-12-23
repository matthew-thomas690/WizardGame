using System.Diagnostics;
using WizardGame.Engine.Abstractions;

namespace WizardGame.Engine;

public sealed class GameRunner
{
    private readonly Game _game;
    private readonly IRenderer _renderer;
    private readonly IInputSource _input;
    private readonly Func<GameState, bool>? _stopCondition;
    private readonly TimeSpan _step;
    private readonly TimeSpan _maxFrameTime;
    private readonly int _maxUpdatesPerFrame;
    private bool _paused;
    private long _tick;

    public GameRunner(
        Game game,
        IRenderer renderer,
        IInputSource input,
        int ticksPerSecond = 60,
        int maxUpdatesPerFrame = 5,
        Func<GameState, bool>? stopCondition = null)
    {
        if (ticksPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ticksPerSecond));
        }

        if (maxUpdatesPerFrame <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxUpdatesPerFrame));
        }

        _game = game ?? throw new ArgumentNullException(nameof(game));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _input = input ?? throw new ArgumentNullException(nameof(input));
        _stopCondition = stopCondition;
        _step = TimeSpan.FromSeconds(1.0 / ticksPerSecond);
        _maxFrameTime = TimeSpan.FromSeconds(0.25);
        _maxUpdatesPerFrame = maxUpdatesPerFrame;
    }

    public int TicksPerSecond => (int)Math.Round(1.0 / _step.TotalSeconds);
    public bool QuitRequested { get; private set; }

    public void Run(CancellationToken token = default)
    {
        QuitRequested = false;
        var stopwatch = Stopwatch.StartNew();
        var previous = stopwatch.Elapsed;
        var accumulator = TimeSpan.Zero;

        while (!token.IsCancellationRequested)
        {
            var now = stopwatch.Elapsed;
            var frameTime = now - previous;
            previous = now;

            if (frameTime > _maxFrameTime)
            {
                frameTime = _maxFrameTime;
            }

            accumulator += frameTime;

            var inputState = _input.Poll();
            if (inputState.Quit)
            {
                QuitRequested = true;
                break;
            }

            if (inputState.TogglePause)
            {
                _paused = !_paused;
            }

            if (_paused && inputState.Step)
            {
                Step(inputState);
                accumulator = TimeSpan.Zero;
            }

            var updates = 0;
            // Fixed-step update with accumulator for deterministic simulation.
            while (!_paused && accumulator >= _step && updates < _maxUpdatesPerFrame)
            {
                Step(inputState);
                accumulator -= _step;
                updates++;
            }

            if (updates >= _maxUpdatesPerFrame)
            {
                accumulator = TimeSpan.Zero;
            }

            _renderer.Render(_game.State, new GameTime(_tick, _step));
            if (_stopCondition?.Invoke(_game.State) == true)
            {
                break;
            }
            Thread.Sleep(1);
        }
    }

    private void Step(InputState input)
    {
        _game.Update(new GameTime(_tick, _step), input);
        _tick++;
    }
}
