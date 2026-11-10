using UnityEngine;

/// <summary>Sprays a cone from the weapon (bottom centre of the screen) toward the aim; no direct hit needed.</summary>
[CreateAssetMenu(menuName = "Game/Damage/Cone")]
public sealed class ConeDamage : DamageType
{
    [Tooltip("Cone length in world units from the weapon.")]
    [SerializeField, Min(0.01f)] private float range = 9f;
    [Tooltip("Half of the cone's opening angle in degrees.")]
    [SerializeField, Range(1f, 89f)] private float halfAngle = 14f;
    [Tooltip("Damage multiplier at the far end of the cone.")]
    [SerializeField, Range(0f, 1f)] private float edgeDamage = 1f;

    /// <summary>True when <paramref name="point"/> lies within the cone from <paramref name="origin"/>.</summary>
    public static bool IsInsideCone(Vector2 origin, Vector2 direction, Vector2 point, float range, float halfAngle)
    {
        Vector2 offset = point - origin;
        if (direction.sqrMagnitude < 0.0001f || offset.sqrMagnitude > range * range) return false;
        return offset.sqrMagnitude < 0.0001f || Vector2.Angle(direction, offset) <= halfAngle;
    }

    protected override bool Resolve(in ShotContext shot)
    {
        Vector2 origin = shot.ToWorld(shot.MuzzleScreen);
        Vector2 direction = shot.ToWorld(shot.AimScreen) - origin;
        bool damaged = false;
        foreach (var collider in DamageQuery.Circle(origin, range, shot.HitMask))
        {
            Vector2 point = collider.bounds.center;
            if (!IsInsideCone(origin, direction, point, range, halfAngle)) continue;
            float falloff = Mathf.Lerp(1f, edgeDamage, Vector2.Distance(origin, point) / range);
            damaged |= Hit(collider, shot.Damage * falloff);
        }
        return damaged;
    }
}
