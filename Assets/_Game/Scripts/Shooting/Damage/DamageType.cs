using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Decides which targets one shot damages and how much. Subclass and override <see cref="Resolve"/> to add a
/// new type; reuse <see cref="DamageQuery"/> for physics and <see cref="Hit"/> to deal damage and effects.
/// </summary>
public abstract class DamageType : ScriptableObject
{
    [Tooltip("Extra effects applied to every target this shot damages (e.g. slow).")]
    [SerializeField] private HitEffect[] effects = System.Array.Empty<HitEffect>();

    private static readonly HashSet<IDamageable> struck = new();
    private static DamageType fallback;

    /// <summary>Plain single target damage, used by weapons without a damage type asset.</summary>
    public static DamageType Default
    {
        get
        {
            if (fallback == null)
            {
                fallback = CreateInstance<SingleTargetDamage>();
                fallback.name = "Default Single Target";
                fallback.hideFlags = HideFlags.DontSave;
            }
            return fallback;
        }
    }

    /// <summary>Resolves one shot. Returns true when at least one target was damaged.</summary>
    public bool Apply(in ShotContext shot)
    {
        struck.Clear();
        return Resolve(shot);
    }

    /// <summary>Resolves an arriving projectile against its original lock, without querying the cursor again.</summary>
    public bool ApplyImpact(in ShotContext shot, Collider2D target, Vector2 position)
    {
        struck.Clear();
        return ResolveImpact(shot, target, position);
    }

    protected virtual bool ResolveImpact(in ShotContext shot, Collider2D target, Vector2 position)
    {
        return Hit(target, shot.Damage);
    }

    protected abstract bool Resolve(in ShotContext shot);

    /// <summary>
    /// Damages the target owning <paramref name="collider"/> and applies the effects. Each target is hit at most
    /// once per shot, even when it has several colliders. Returns false when nothing new was damaged.
    /// </summary>
    protected bool Hit(Collider2D collider, float damage)
    {
        if (collider == null || !(damage > 0f)) return false;
        var target = collider.GetComponentInParent<IDamageable>();
        if (target == null || !struck.Add(target)) return false;
        target.TakeDamage(damage);
        if (target is Component component && component != null)
            foreach (var effect in effects)
                if (effect != null) effect.Apply(component);
        return true;
    }
}
