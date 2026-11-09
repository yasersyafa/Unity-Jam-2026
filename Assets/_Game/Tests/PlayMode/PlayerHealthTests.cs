using NUnit.Framework;
using UnityEngine;

public sealed class PlayerHealthTests
{
    private GameObject player;
    private PlayerHealth health;
    [SetUp] public void SetUp()
    {
        player = new GameObject("Test player");
        health = player.AddComponent<PlayerHealth>();
    }
    [TearDown] public void TearDown() { Object.DestroyImmediate(player); }

    [Test] public void LethalDamage_ClampsHealthAndRaisesDeathOnce()
    {
        int deaths = 0, changes = 0;
        float applied = 0;
        health.Died += () => deaths++;
        health.HealthChanged += () => changes++;
        health.Damaged += damage => applied += damage;
        float maximum = health.MaximumHealth;
        health.TakeDamage(25);
        Assert.That(health.CurrentHealth, Is.EqualTo(maximum - 25));
        health.TakeDamage(maximum * 2);
        health.TakeDamage(1);
        Assert.That(health.CurrentHealth, Is.Zero);
        Assert.That(health.IsDead, Is.True);
        Assert.That(applied, Is.EqualTo(maximum));
        Assert.That(changes, Is.EqualTo(2));
        Assert.That(deaths, Is.EqualTo(1));
    }

    [Test] public void InvalidDamage_LeavesHealthAndEventsUnchanged()
    {
        int changes = 0;
        health.HealthChanged += () => changes++;
        foreach (float damage in new[] { 0f, -1f, float.NaN }) health.TakeDamage(damage);
        Assert.That(health.CurrentHealth, Is.EqualTo(health.MaximumHealth));
        Assert.That(changes, Is.Zero);
    }

    [Test] public void DisabledPlayer_IgnoresDamageUntilReenabled()
    {
        player.SetActive(false);
        health.TakeDamage(10);
        Assert.That(health.CurrentHealth, Is.EqualTo(health.MaximumHealth));
        player.SetActive(true);
        health.TakeDamage(10);
        Assert.That(health.CurrentHealth, Is.EqualTo(health.MaximumHealth - 10));
    }
}
