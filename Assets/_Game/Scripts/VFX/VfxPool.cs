using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

/// <summary>Reuses one-shot VFX instances per prefab instead of Instantiate/Destroy on every hit or death.</summary>
public static class VfxPool
{
    private static readonly Dictionary<GameObject, ObjectPool<PooledVfx>> pools = new();
    private static Transform root;

    public static void Spawn(GameObject prefab, Vector3 position, Quaternion rotation, float lifetime)
    {
        if (prefab == null) return;
        EnsureRoot();
        if (!pools.TryGetValue(prefab, out var pool))
        {
            pool = new ObjectPool<PooledVfx>(
                () => Create(prefab),
                instance => instance.gameObject.SetActive(true),
                instance => instance.gameObject.SetActive(false),
                instance => { if (instance != null) Object.Destroy(instance.gameObject); },
                collectionCheck: false, defaultCapacity: 4, maxSize: 32);
            pools.Add(prefab, pool);
        }
        var vfx = pool.Get();
        vfx.transform.SetPositionAndRotation(position, rotation);
        vfx.Play(pool, Mathf.Max(0.01f, lifetime));
    }

    private static PooledVfx Create(GameObject prefab)
    {
        var instance = Object.Instantiate(prefab, root);
        instance.SetActive(false);
        return instance.AddComponent<PooledVfx>();
    }

    private static void EnsureRoot()
    {
        if (root != null) return;
        root = new GameObject("VFX Pool").transform;
        Object.DontDestroyOnLoad(root.gameObject);
        SceneManager.sceneUnloaded -= Clear;
        SceneManager.sceneUnloaded += Clear;
    }

    // Particles still in flight belong to the old scene; start the next scene with empty pools.
    private static void Clear(Scene scene)
    {
        foreach (var pool in pools.Values) pool.Clear();
        pools.Clear();
        if (root != null)
            for (int i = root.childCount - 1; i >= 0; i--) Object.Destroy(root.GetChild(i).gameObject);
    }
}
