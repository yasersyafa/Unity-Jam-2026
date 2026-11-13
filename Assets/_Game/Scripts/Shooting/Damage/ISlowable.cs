/// <summary>A target whose movement and actions can be slowed temporarily.</summary>
public interface ISlowable
{
    /// <param name="speedMultiplier">0..1 speed while slowed; the strongest active slow wins.</param>
    /// <param name="duration">Seconds; reapplying refreshes the remaining time.</param>
    void ApplySlow(float speedMultiplier, float duration);
}
