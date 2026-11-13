using UnityEngine;

/// <summary>
/// Extra effect applied to each target a <see cref="DamageType"/> damages. Subclass to add new effects
/// (burn, stun, knockback) and list the asset in a damage type's effects.
/// </summary>
public abstract class HitEffect : ScriptableObject
{
    /// <param name="target">The damaged component (the target's <see cref="IDamageable"/>).</param>
    public abstract void Apply(Component target);
}
