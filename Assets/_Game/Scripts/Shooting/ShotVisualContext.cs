using UnityEngine;

/// <summary>Pre-damage destination and optional visual target lock; never applies damage.</summary>
public readonly struct ShotVisualContext
{
    private readonly Collider2D target;
    private readonly Camera camera;
    private readonly GameplayLease lease;
    private readonly uint generation;
    public bool IsTargetCurrent => lease == null || (lease.IsLeased && lease.Generation == generation);
    public Collider2D HitCollider { get; }
    public Vector2 ImpactWorldPosition { get; }
    public bool TracksTarget => target != null;
    public Vector2 DestinationScreen { get; }
    public bool HasImpact { get; }

    public ShotVisualContext(Vector2 destinationScreen, bool hasImpact)
        : this(destinationScreen, hasImpact, null, null) { }

    private ShotVisualContext(Vector2 destinationScreen, bool hasImpact, Collider2D target, Camera camera)
        : this(destinationScreen, hasImpact, target, camera, target, target != null ? (Vector2)target.bounds.center : default) { }

    private ShotVisualContext(Vector2 destinationScreen, bool hasImpact, Collider2D target, Camera camera,
        Collider2D hitCollider, Vector2 impactWorldPosition)
    {
        DestinationScreen = destinationScreen;
        HasImpact = hasImpact;
        this.target = target;
        this.camera = camera;
        HitCollider = hitCollider;
        lease = hitCollider != null ? hitCollider.GetComponentInParent<GameplayLease>() : null;
        generation = lease != null ? lease.Generation : 0;
        ImpactWorldPosition = impactWorldPosition;
    }

    /// <summary>False once the target disappears or its hitbox is disabled; callers retain the last destination.</summary>
    public bool TryGetTargetScreenPosition(out Vector2 position)
    {
        position = default;
        if (!IsTargetCurrent || target == null || camera == null || !target.isActiveAndEnabled) return false;
        position = camera.WorldToScreenPoint(target.bounds.center);
        return true;
    }

    public static ShotVisualContext Capture(in ShotContext shot, bool trackTarget = false)
    {
        if (!DamageQuery.First(shot, out RaycastHit2D hit)) return new ShotVisualContext(shot.AimScreen, false);
        if (trackTarget && hit.collider.GetComponentInParent<IDamageable>() != null)
        {
            Vector2 center = shot.Camera.WorldToScreenPoint(hit.collider.bounds.center);
            return new ShotVisualContext(center, true, hit.collider, shot.Camera);
        }
        return new ShotVisualContext(shot.Camera.WorldToScreenPoint(hit.point), true, null, shot.Camera, hit.collider, hit.point);
    }
}
