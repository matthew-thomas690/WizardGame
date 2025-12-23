namespace WizardGame.Engine;

public readonly record struct InputState(bool Quit, bool TogglePause, bool Step)
{
    public static InputState None => new(false, false, false);
}
