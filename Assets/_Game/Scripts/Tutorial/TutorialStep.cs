/// <summary>
/// One lesson of the tutorial. Steps complete through what the player does, never through a timer on text.
/// Add a step by subclassing this and listing it in <see cref="TutorialDirector"/>'s CreateSteps.
/// </summary>
public abstract class TutorialStep
{
    protected TutorialDirector Director { get; private set; }

    internal void Bind(TutorialDirector director) => Director = director;

    public abstract string Title { get; }
    public abstract string Instruction { get; }
    /// <summary>Shown when the player makes no progress for a while.</summary>
    public virtual string Hint => Instruction;
    public virtual bool AllowShooting => true;
    public virtual bool AllowSlot => false;
    /// <summary>Whether a streak of missed shots makes the coach tease the player.</summary>
    public virtual bool TeasesMisses => true;

    public virtual void Enter() { }
    /// <summary>Returns true once the lesson is done.</summary>
    public abstract bool Tick();
    public virtual void Exit() { }
}
