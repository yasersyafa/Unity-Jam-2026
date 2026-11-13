using UnityEngine;

/// <summary>Damages the first target under the aim. The default for weapons without a damage type.</summary>
[CreateAssetMenu(menuName = "Game/Damage/Single Target")]
public sealed class SingleTargetDamage : DamageType
{
    protected override bool Resolve(in ShotContext shot)
    {
        return DamageQuery.First(shot, out RaycastHit2D hit) && Hit(hit.collider, shot.Damage);
    }
}
