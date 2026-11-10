using System.Collections.Generic;
using UnityEngine;

/// <summary>Passes through every target under the aim, nearest first, up to a limit.</summary>
[CreateAssetMenu(menuName = "Game/Damage/Piercing")]
public sealed class PiercingDamage : DamageType
{
    [Tooltip("Maximum number of targets one shot can pass through.")]
    [SerializeField, Min(1)] private int maxTargets = 5;

    private static readonly List<RaycastHit2D> hits = new();

    protected override bool Resolve(in ShotContext shot)
    {
        DamageQuery.All(shot, hits);
        int damaged = 0;
        for (int i = 0; i < hits.Count && damaged < maxTargets; i++)
            if (Hit(hits[i].collider, shot.Damage)) damaged++;
        return damaged > 0;
    }
}
