using UnityEngine;

/// <summary>
/// Explodes where the shot hits a target, damaging everything in the radius with linear falloff.
/// A shot that hits nothing does not explode.
/// </summary>
[CreateAssetMenu(menuName = "Game/Damage/Explosion")]
public sealed class ExplosionDamage : DamageType
{
    [Tooltip("Blast radius in world units on the gameplay plane.")]
    [SerializeField, Min(0.01f)] private float radius = 2f;
    [Tooltip("Damage multiplier at the edge of the blast; the centre always deals full damage.")]
    [SerializeField, Range(0f, 1f)] private float edgeDamage = 0.5f;
    [Tooltip("Optional VFX spawned at the impact point.")]
    [SerializeField] private GameObject impactVfx;
    [SerializeField, Min(0.01f)] private float vfxLifetime = 2f;

    /// <summary>Damage multiplier at <paramref name="distance"/> from the blast centre; 0 outside the radius.</summary>
    public static float Falloff(float distance, float radius, float edgeDamage)
    {
        if (radius <= 0f || distance > radius) return 0f;
        return Mathf.Lerp(1f, edgeDamage, Mathf.Clamp01(distance / radius));
    }

    protected override bool Resolve(in ShotContext shot)
    {
        if (!DamageQuery.First(shot, out RaycastHit2D direct)) return false;
        Vector2 center = direct.point;
        bool damaged = Hit(direct.collider, shot.Damage);
        foreach (var collider in DamageQuery.Circle(center, radius, shot.HitMask))
        {
            float distance = Vector2.Distance(center, collider.ClosestPoint(center));
            damaged |= Hit(collider, shot.Damage * Falloff(distance, radius, edgeDamage));
        }
        if (impactVfx != null) VfxPool.Spawn(impactVfx, center, Quaternion.identity, vfxLifetime);
        return damaged;
    }
}
