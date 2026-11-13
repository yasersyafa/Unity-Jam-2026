using UnityEngine;

/// <summary>One flight and its captured damage. Presentation observes this state; hiding a visual cannot remove damage.</summary>
public sealed class ProjectileShot
{
    private ShotVisualContext visual;
    private readonly ShotContext shot;
    private readonly DamageType damageType;
    private Collider2D hitCollider;
    private Vector2 impactWorldPosition;
    private bool tracking;
    private bool resolved;

    public Vector2 DestinationScreen { get; private set; }
    public float Duration { get; }
    public float Elapsed { get; private set; }
    public float Progress => Mathf.Clamp01(Elapsed / Duration);
    public bool HasImpact { get; }
    public bool IsComplete { get; private set; }
    public bool IsCanceled { get; private set; }

    // Used by visual-only previews, which deliberately have no damage payload.
    public ProjectileShot(in ShotVisualContext visual, float duration)
        : this(visual, duration, default, null) { }

    internal ProjectileShot(in ShotVisualContext visual, float duration, in ShotContext shot, DamageType damageType)
    {
        this.visual = visual;
        this.shot = shot;
        this.damageType = damageType;
        Duration = Mathf.Max(0.01f, duration);
        DestinationScreen = visual.DestinationScreen;
        HasImpact = visual.HasImpact;
        hitCollider = visual.HitCollider;
        impactWorldPosition = visual.ImpactWorldPosition;
        tracking = visual.TracksTarget;
    }

    internal void Advance(float deltaTime)
    {
        if (IsComplete) return;
        if (tracking)
        {
            if (visual.TryGetTargetScreenPosition(out Vector2 position))
            {
                DestinationScreen = position;
                impactWorldPosition = visual.HitCollider.bounds.center;
            }
            else
            {
                tracking = false;
                hitCollider = null;
                visual = default;
            }
        }
        Elapsed += Mathf.Max(0f, deltaTime);
        IsComplete = Elapsed >= Duration;
    }

    internal bool ResolveDamage()
    {
        if (resolved || !IsComplete || IsCanceled) return false;
        resolved = true;
        if (!HasImpact || damageType == null) return false;
        Collider2D target = visual.IsTargetCurrent && hitCollider != null && hitCollider.isActiveAndEnabled ? hitCollider : null;
        return damageType.ApplyImpact(shot, target, impactWorldPosition);
    }

    internal void Cancel()
    {
        IsCanceled = IsComplete = true;
        hitCollider = null;
        visual = default;
    }
}
