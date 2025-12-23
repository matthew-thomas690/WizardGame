using WizardGame.Engine;

namespace WizardGame.Engine.Abstractions;

public interface IInputSource
{
    InputState Poll();
}
