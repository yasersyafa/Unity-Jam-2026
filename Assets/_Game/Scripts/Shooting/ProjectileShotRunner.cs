using System.Collections.Generic;
using UnityEngine;

/// <summary>Gameplay lifetime for projectiles, independent of VFX pooling, weapon swaps and input suspension.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(90)] // After camera shake, before WeaponShotPresentation renders the same flight state.
public sealed class ProjectileShotRunner : MonoBehaviour
{
    private readonly List<ProjectileShot> active = new List<ProjectileShot>();
    private WeaponFeedback feedback;
    public int ActiveShotCount => active.Count;

    public void Initialize(WeaponFeedback presentation) { feedback = presentation; }

    public ProjectileShot Launch(in ShotContext shot, in ShotVisualContext visual, DamageType damageType, float duration)
    {
        var projectile = new ProjectileShot(visual, duration, shot, damageType);
        active.Add(projectile);
        return projectile;
    }

    private void LateUpdate()
    {
        if (Time.timeScale <= 0f) return;
        for (int i = active.Count - 1; i >= 0 && i < active.Count; i--)
        {
            ProjectileShot projectile = active[i];
            projectile.Advance(Time.deltaTime);
            if (!projectile.IsComplete) continue;
            active.RemoveAt(i);
            bool hit = projectile.ResolveDamage();
            if (hit && feedback != null && feedback.isActiveAndEnabled) feedback.PlayHitmarker();
        }
    }

    private void OnDisable()
    {
        foreach (ProjectileShot projectile in active) projectile.Cancel();
        active.Clear();
    }
}
