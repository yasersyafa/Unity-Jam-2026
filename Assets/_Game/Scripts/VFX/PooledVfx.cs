using UnityEngine;
using UnityEngine.Pool;

/// <summary>Returns its instance to the owning pool after the lifetime, in scaled time so it pauses with the game.</summary>
[DisallowMultipleComponent]
public sealed class PooledVfx : MonoBehaviour
{
    private ParticleSystem[] systems;
    private IObjectPool<PooledVfx> owner;
    private float remaining;

    public void Play(IObjectPool<PooledVfx> pool, float lifetime)
    {
        owner = pool;
        remaining = lifetime;
        systems ??= GetComponentsInChildren<ParticleSystem>(true);
        // Restart every top-level system; Play(true) also restarts its children (sub-emitters are driven by their parent).
        foreach (var system in systems)
        {
            Transform parent = system.transform.parent;
            if (parent != transform.parent && parent.GetComponentInParent<ParticleSystem>() != null) continue;
            system.Clear(true);
            system.Play(true);
        }
    }

    private void Update()
    {
        remaining -= Time.deltaTime;
        if (remaining > 0f || owner == null) return;
        var pool = owner;
        owner = null;
        pool.Release(this);
    }
}
