/// <summary>
/// One phase of a gameplay session. Subclass this and register it in
/// <see cref="GameStateManager"/> to add a new phase.
/// </summary>
public abstract class GameState
{
    protected GameStateManager Manager { get; private set; }

    /// <summary>Terminal states (victory, game over) ignore further automatic transitions.</summary>
    public virtual bool IsTerminal => false;

    internal void Bind(GameStateManager manager) => Manager = manager;

    public virtual void Enter() { }
    public virtual void Tick() { }
    public virtual void Exit() { }

    public override string ToString() => GetType().Name;
}
