using WizardGame.Engine;

namespace WizardGame.Engine.Abstractions;

public interface IRenderer
{
    void Render(GameState state, GameTime time);
}
