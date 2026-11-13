using NUnit.Framework;
using UnityEngine;

public sealed class GameplayPoolTests
{
    [Test]
    public void ReleasedLease_IsReusedWithNewGeneration_AndRestoresScale()
    {
        var owner = new GameObject("Pool owner");
        var template = new GameObject("Target template");
        template.SetActive(false);
        template.transform.localScale = new Vector3(2, 3, 1);
        var prefab = template.AddComponent<BossAttackTarget>();
        var pool = new GameplayPrefabPool(owner.transform);
        try
        {
            var first = pool.Acquire(prefab, owner.transform, Vector3.zero, Quaternion.identity);
            first.gameObject.SetActive(true);
            var lease = first.GetComponent<GameplayLease>();
            uint generation = lease.Generation;
            first.transform.localScale = Vector3.one * 9;
            first.TakeDamage(1);
            GameplayLease.Despawn(first.gameObject); // duplicate release must not enqueue twice
            var second = pool.Acquire(prefab, owner.transform, Vector3.one, Quaternion.identity);
            Assert.That(second, Is.SameAs(first));
            Assert.That(lease.Generation, Is.Not.EqualTo(generation));
            Assert.That(second.transform.localScale, Is.EqualTo(template.transform.localScale));
            Assert.That(second.transform.position, Is.EqualTo(Vector3.one));
            var third = pool.Acquire(prefab, owner.transform, Vector3.zero, Quaternion.identity);
            Assert.That(third, Is.Not.SameAs(second));
        }
        finally
        {
            pool.Dispose();
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(template);
        }
    }

    [Test]
    public void Prewarm_DoesNotAdvanceGameplayRandom_OrActivateTargets()
    {
        var owner = new GameObject("Pool owner");
        var template = new GameObject("Target template");
        template.SetActive(false);
        var prefab = template.AddComponent<BossAttackTarget>();
        var pool = new GameplayPrefabPool(owner.transform);
        var previous = Random.state;
        try
        {
            Random.InitState(1234);
            var state = Random.state;
            float expected = Random.value;
            Random.state = state;
            pool.Prewarm(prefab, 9);
            Assert.That(Random.value, Is.EqualTo(expected));
            Assert.That(owner.GetComponentsInChildren<BossAttackTarget>().Length, Is.Zero);
            int misses = pool.MissCount;
            var target = pool.Acquire(prefab, owner.transform, Vector3.zero, Quaternion.identity);
            Assert.That(target.gameObject.activeSelf, Is.False);
            Assert.That(pool.MissCount, Is.EqualTo(misses));
        }
        finally
        {
            Random.state = previous;
            pool.Dispose();
            Object.DestroyImmediate(owner);
            Object.DestroyImmediate(template);
        }
    }
}
