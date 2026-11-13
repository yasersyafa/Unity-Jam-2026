using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Scene-owned prefab pool. Call Prepare while inactive, then activate the lease.</summary>
public sealed class GameplayPrefabPool : IDisposable
{
    private readonly Transform root;
    private readonly Dictionary<GameObject, ObjectPool<GameplayLease>> pools = new();
    private readonly HashSet<GameplayLease> active = new();
    private bool disposed;
    private static readonly Unity.Profiling.ProfilerMarker missMarker = new("Game.Pool.Miss");
    public int MissCount { get; private set; }

    public GameplayPrefabPool(Transform owner)
    {
        root = new GameObject("Gameplay Pool").transform;
        root.SetParent(owner, false);
        root.gameObject.SetActive(false);
    }

    private ObjectPool<GameplayLease> GetPool(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out var pool)) return pool;
        pool = new ObjectPool<GameplayLease>(() =>
        {
            using var sample = missMarker.Auto();
            MissCount++;
            var instance = UnityEngine.Object.Instantiate(prefab, root);
            instance.SetActive(false);
            var lease = instance.GetComponent<GameplayLease>() ?? instance.AddComponent<GameplayLease>();
            lease.Capture();
            return lease;
        }, null, lease => lease.gameObject.SetActive(false),
        lease => { if (lease != null) UnityEngine.Object.Destroy(lease.gameObject); }, false, 8, 256);
        pools.Add(prefab, pool);
        return pool;
    }

    public T Acquire<T>(T prefab, Transform parent, Vector3 position, Quaternion rotation) where T : Component
    {
        if (disposed) throw new ObjectDisposedException(nameof(GameplayPrefabPool));
        var pool = GetPool(prefab.gameObject);
        var lease = pool.Get();
        lease.Begin(this, pool);
        active.Add(lease);
        lease.transform.SetParent(parent, false);
        lease.transform.SetPositionAndRotation(position, rotation);
        return lease.GetComponent<T>();
    }

    internal void Release(GameplayLease lease, IObjectPool<GameplayLease> pool)
    {
        if (!active.Remove(lease)) return;
        lease.transform.SetParent(root, false);
        if (!disposed) pool.Release(lease);
        else UnityEngine.Object.Destroy(lease.gameObject);
    }

    public void Prewarm<T>(T prefab, int count) where T : Component
    {
        var pool = GetPool(prefab.gameObject);
        var held = new List<GameplayLease>(count);
        for (int i = 0; i < count; i++) held.Add(pool.Get());
        foreach (var lease in held) pool.Release(lease);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (var lease in active) if (lease != null) UnityEngine.Object.Destroy(lease.gameObject);
        active.Clear();
        foreach (var pool in pools.Values) pool.Clear();
        pools.Clear();
        if (root != null) UnityEngine.Object.Destroy(root.gameObject);
    }
}
