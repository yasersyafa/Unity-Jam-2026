using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class BossMechanicsPlayModeTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private readonly List<Object> created = new List<Object>();

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, Private);
        Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}' on {target.GetType().Name}");
        field.SetValue(target, value);
    }

    private (BossEncounter boss, BossConfig config, PlayerHealth player, Camera cam, Transform plane, BoxCollider2D hitbox) CreateHarness(
        float spinDuration = 0.08f,
        float redOrbitDuration = 10f,
        float blueFlightDuration = 8.5f,
        float delayBetween = 0.05f)
    {
        var camObj = new GameObject("TestCamera", typeof(Camera));
        camObj.transform.position = new Vector3(0f, 0f, -10f);
        var cam = camObj.GetComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        created.Add(camObj);

        var planeObj = new GameObject("TestPlane");
        planeObj.transform.position = Vector3.zero;
        created.Add(planeObj);

        var playerObj = new GameObject("TestPlayer", typeof(PlayerHealth));
        var player = playerObj.GetComponent<PlayerHealth>();
        SetField(player, "maximumHealth", 500f);
        typeof(PlayerHealth).GetMethod("Awake", Private).Invoke(player, null);
        created.Add(playerObj);

        var projTemplate = new GameObject("TestBossProjectile", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(BossAttackTarget));
        projTemplate.SetActive(false);
        var projTarget = projTemplate.GetComponent<BossAttackTarget>();
        created.Add(projTemplate);

        var normalData = ScriptableObject.CreateInstance<EnemyData>();
        SetField(normalData, "approachDuration", blueFlightDuration);
        SetField(normalData, "initialScale", 0.4f);
        SetField(normalData, "finalScale", 1.6f);
        created.Add(normalData);

        var config = ScriptableObject.CreateInstance<BossConfig>();
        SetField(config, "health", 2500f);
        SetField(config, "rouletteSpinDuration", spinDuration);
        SetField(config, "redOrbitDuration", redOrbitDuration);
        SetField(config, "delayBetweenAttacks", delayBetween);
        SetField(config, "normalEnemyData", normalData);
        created.Add(config);

        var bossObj = new GameObject("TestBoss", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(BossEncounter));
        var boss = bossObj.GetComponent<BossEncounter>();
        var visual = bossObj.GetComponent<SpriteRenderer>();
        var hitbox = bossObj.GetComponent<BoxCollider2D>();
        SetField(boss, "visual", visual);
        SetField(boss, "hitbox", hitbox);
        SetField(boss, "attackPrefab", projTarget);
        created.Add(bossObj);

        return (boss, config, player, cam, planeObj.transform, hitbox);
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (var item in created)
            if (item != null) Object.Destroy(item);
        created.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator BossAppear_SpinsRouletteInvulnerableFirst_ThenOpensVulnerabilityWhenAttackStarts()
    {
        var (boss, config, player, cam, plane, hitbox) = CreateHarness(spinDuration: 0.12f, redOrbitDuration: 10f);
        var rolls = new Queue<float>(new[] { 0.1f, 0.1f }); // Buff Damage (15)
        boss.SetRandomSource(() => rolls.Count > 0 ? rolls.Dequeue() : 0.1f);

        boss.BeginEncounter(config, cam, plane, player, null, null);

        // Immediately on spawn: boss must be spinning roulette & invulnerable.
        Assert.That(boss.IsSpinningRoulette, Is.True);
        Assert.That(boss.IsVulnerable, Is.False);
        Assert.That(hitbox.enabled, Is.False);
        Assert.That(boss.CurrentHealth, Is.EqualTo(2500f));

        boss.TakeDamage(500f);
        Assert.That(boss.CurrentHealth, Is.EqualTo(2500f), "Boss must ignore damage while spinning roulette.");

        float timeout = Time.realtimeSinceStartup + 2f;
        while (boss.IsSpinningRoulette && Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(boss.IsSpinningRoulette, Is.False);
        Assert.That(boss.LastRouletteResult.Effect, Is.EqualTo(BossRouletteEffect.BuffDamage));
        Assert.That(boss.ActiveTargets.Count, Is.EqualTo(8), "First attack should spawn 8 Red projectiles.");
        Assert.That(boss.IsVulnerable, Is.True);
        Assert.That(hitbox.enabled, Is.True);

        boss.TakeDamage(500f);
        Assert.That(boss.CurrentHealth, Is.EqualTo(2000f), "Boss must take damage once attack launches.");
    }

    [UnityTest]
    public IEnumerator RedAttack_CanBeShotDuringOrbit_BecomesIndestructibleAfterWaitAndDealsBuffedDamage()
    {
        var (boss, config, player, cam, plane, _) = CreateHarness(spinDuration: 0.05f, redOrbitDuration: 0.12f);
        // Buff Damage => 15 per projectile
        var rolls = new Queue<float>(new[] { 0.1f, 0.1f });
        boss.SetRandomSource(() => rolls.Count > 0 ? rolls.Dequeue() : 0.1f);

        boss.BeginEncounter(config, cam, plane, player, null, null);

        float timeout = Time.realtimeSinceStartup + 2f;
        while (boss.ActiveTargets.Count == 0 && Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(boss.ActiveTargets.Count, Is.EqualTo(8));
        BossAttackTarget first = boss.ActiveTargets[0];
        BossAttackTarget survivor = boss.ActiveTargets[1];
        Assert.That(first.Kind, Is.EqualTo(BossProjectileKind.Red));
        Assert.That(first.IsInvulnerable, Is.False);

        // Shoot 1st projectile while orbiting -> destroyed.
        first.TakeDamage(10f);
        Assert.That(first.isActiveAndEnabled, Is.False);

        // Wait until orbit timer expires and charge begins -> indestructible.
        timeout = Time.realtimeSinceStartup + 2f;
        while (!survivor.IsInvulnerable && survivor.isActiveAndEnabled && Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(survivor.IsInvulnerable, Is.True);
        Assert.That(survivor.GetComponent<Collider2D>().enabled, Is.False);
        survivor.TakeDamage(999f);
        Assert.That(survivor.isActiveAndEnabled, Is.True, "Red projectile must be indestructible once charging.");

        // Wait for remaining 7 projectiles to finish charging (0.5s Charged lunge)
        float healthBefore = player.CurrentHealth;
        timeout = Time.realtimeSinceStartup + 2f;
        while (boss.ActiveTargets.Count > 0 && Time.realtimeSinceStartup < timeout)
            yield return null;

        // 7 survivors x 15 damage (Damage Buff) = 105 damage
        Assert.That(healthBefore - player.CurrentHealth, Is.EqualTo(105f).Within(0.01f));
    }

    [UnityTest]
    public IEnumerator BlueProjectile_PhasesInvisibleAndInvulnerableWhileAdvancingForward()
    {
        var playerObj = new GameObject("BluePlayer", typeof(PlayerHealth));
        var player = playerObj.GetComponent<PlayerHealth>();
        SetField(player, "maximumHealth", 200f);
        typeof(PlayerHealth).GetMethod("Awake", Private).Invoke(player, null);
        created.Add(playerObj);

        var projObj = new GameObject("BlueProjectile", typeof(SpriteRenderer), typeof(BoxCollider2D), typeof(BossAttackTarget));
        var proj = projObj.GetComponent<BossAttackTarget>();
        var renderer = projObj.GetComponent<SpriteRenderer>();
        var collider = projObj.GetComponent<BoxCollider2D>();
        created.Add(projObj);

        // Cycle: 0.4s visible, 0.3s invisible, total flight = 1.0s
        var launch = new BossProjectileLaunchData(
            BossProjectileKind.Blue,
            null,
            new Vector2(0.5f, 0.55f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.14f, 0.12f),
            new Vector2(0.28f, 0.23f),
            0f,
            2f,
            0f,
            1.0f,
            new Vector2(0.4f, 1.6f),
            0.4f,
            0.3f,
            15f,
            null,
            null,
            player);

        proj.Launch(launch);

        // Initially visible & shootable
        Assert.That(proj.IsVisible, Is.True);
        Assert.That(renderer.enabled, Is.True);
        Assert.That(collider.enabled, Is.True);
        Assert.That(proj.IsInvulnerable, Is.False);

        // Advance into invisible phase (t = 0.50s within 0.4s..0.7s window)
        proj.Step(0.50f);
        float scaleAtStartOfInvis = proj.transform.localScale.x;
        Vector3 posAtStartOfInvis = proj.transform.position;

        Assert.That(proj.IsVisible, Is.False);
        Assert.That(renderer.enabled, Is.False);
        Assert.That(collider.enabled, Is.False);
        Assert.That(proj.IsInvulnerable, Is.True);

        // Cannot be shot while invisible
        proj.TakeDamage(500f);
        Assert.That(proj.isActiveAndEnabled, Is.True);

        // Advances scale & position while still invisible (t = 0.65s < 0.7s)
        proj.Step(0.15f);
        Assert.That(proj.IsVisible, Is.False);
        Assert.That(proj.transform.localScale.x, Is.GreaterThan(scaleAtStartOfInvis), "Blue projectile must keep advancing scale while invisible.");
        Assert.That(proj.transform.position, Is.Not.EqualTo(posAtStartOfInvis), "Blue projectile must keep moving while invisible.");

        // Re-enters visible phase at t = 0.75s (> 0.7s)
        proj.Step(0.10f);
        Assert.That(proj.IsVisible, Is.True);
        Assert.That(collider.enabled, Is.True);

        // Completes flight at t >= 1.0s and deals damage to player
        proj.Step(0.30f);
        Assert.That(proj.isActiveAndEnabled, Is.False);
        Assert.That(player.CurrentHealth, Is.EqualTo(185f).Within(0.01f));

        yield return null;
    }

    [UnityTest]
    public IEnumerator SecondCycle_LaunchesBlueAttackWithFourProjectiles()
    {
        var (boss, config, player, cam, plane, _) = CreateHarness(
            spinDuration: 0.04f,
            redOrbitDuration: 0.08f,
            blueFlightDuration: 0.5f,
            delayBetween: 0.04f);

        // Roll Debuff Speed -> RedWait = 12% bigger, BlueSpeed = 0.8x
        boss.SetRandomSource(() => 0.8f);
        boss.BeginEncounter(config, cam, plane, player, null, null);

        // Wait for Cycle 1 (Red) to finish charging & Cycle 2 Roulette to start & finish
        float timeout = Time.realtimeSinceStartup + 3f;
        bool sawBlue = false;
        while (Time.realtimeSinceStartup < timeout)
        {
            if (boss.ActiveTargets.Count == 4 && boss.ActiveTargets[0].Kind == BossProjectileKind.Blue)
            {
                sawBlue = true;
                break;
            }
            yield return null;
        }

        Assert.That(sawBlue, Is.True, "Boss second attack cycle must launch 4 Blue projectiles.");
        Assert.That(boss.LastRouletteResult.Effect, Is.EqualTo(BossRouletteEffect.DebuffSpeed));
        Assert.That(boss.IsVulnerable, Is.True);
    }
}
