using System.Collections.Generic;
using UnityEngine;

/// <summary>Allocation-free 2D physics queries shared by all damage types.</summary>
public static class DamageQuery
{
    private const int MaxHits = 32;
    private static readonly RaycastHit2D[] rayHits = new RaycastHit2D[MaxHits];
    private static readonly List<Collider2D> overlaps = new(MaxHits);

    /// <summary>First collider under the aim ray.</summary>
    public static bool First(in ShotContext shot, out RaycastHit2D hit)
    {
        hit = Physics2D.GetRayIntersection(shot.Ray, Mathf.Infinity, shot.HitMask);
        return hit.collider != null;
    }

    /// <summary>Fills <paramref name="results"/> with every collider under the aim ray, nearest first.</summary>
    public static int All(in ShotContext shot, List<RaycastHit2D> results)
    {
        results.Clear();
        int count = Physics2D.defaultPhysicsScene.GetRayIntersection(shot.Ray, Mathf.Infinity, rayHits, shot.HitMask);
        for (int i = 0; i < count; i++) results.Add(rayHits[i]);
        results.Sort((a, b) => a.distance.CompareTo(b.distance));
        return results.Count;
    }

    /// <summary>Colliders overlapping a circle on the gameplay plane. The returned list is reused; do not keep it.</summary>
    public static List<Collider2D> Circle(Vector2 center, float radius, LayerMask mask)
    {
        var filter = new ContactFilter2D { useTriggers = Physics2D.queriesHitTriggers };
        filter.SetLayerMask(mask);
        Physics2D.OverlapCircle(center, radius, filter, overlaps);
        return overlaps;
    }
}
