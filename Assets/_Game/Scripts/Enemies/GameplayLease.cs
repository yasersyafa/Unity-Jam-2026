using UnityEngine;
using UnityEngine.Pool;

/// <summary>A generation identifies one spawn, independently of Unity object identity.</summary>
public sealed class GameplayLease : MonoBehaviour
{
    private GameplayPrefabPool owner;
    private IObjectPool<GameplayLease> pool;
    private Vector3 scale;
    private bool leased;
    private float remaining = -1f;
    public uint Generation { get; private set; }
    public bool IsLeased => leased;
    internal void Capture() { scale = transform.localScale; }
    internal void Begin(GameplayPrefabPool source, IObjectPool<GameplayLease> collection)
    {
        owner = source;
        pool = collection;
        leased = true;
        remaining = -1f;
        Generation++;
        transform.localScale = scale;
    }
    public void Release(float delay = 0f)
    {
        if (!leased) return;
        if (delay > 0f) { if (remaining < 0f) remaining = delay; return; }
        leased = false;
        remaining = -1f;
        owner.Release(this, pool);
    }
    private void Update()
    {
        if (remaining < 0f) return;
        remaining -= Time.deltaTime;
        if (remaining <= 0f) Release();
    }
    public static void Despawn(GameObject target, float delay = 0f)
    {
        if (target.TryGetComponent<GameplayLease>(out var lease)) lease.Release(delay);
        else Object.Destroy(target, delay);
    }
}
