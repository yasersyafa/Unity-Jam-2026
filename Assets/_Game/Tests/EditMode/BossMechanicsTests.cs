using System.Linq;
using GabrielBigardi.SpriteAnimator;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class BossMechanicsTests
{
    [Test]
    public void BossConfigAsset_Has2500HealthAndProjectilesAndEnemySpeedRefsWired()
    {
        var config = AssetDatabase.LoadAssetAtPath<BossConfig>("Assets/_Game/ScriptableObjects/Enemies/BossConfig.asset");
        var charged = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/ChargedEnemyData.asset");
        var normal = AssetDatabase.LoadAssetAtPath<EnemyData>("Assets/_Game/ScriptableObjects/Enemies/NormalEnemyData.asset");

        Assert.That(config, Is.Not.Null);
        Assert.That(config.Health, Is.EqualTo(2500f).Within(0.01f));
        Assert.That(config.RedProjectileCount, Is.EqualTo(8));
        Assert.That(config.RedOrbitDuration, Is.EqualTo(10f).Within(0.01f));
        Assert.That(config.RedOrbitPeriod, Is.EqualTo(6f).Within(0.01f));
        Assert.That(config.BlueProjectileCount, Is.EqualTo(4));
        Assert.That(config.BlueOrbitPeriod, Is.EqualTo(7f).Within(0.01f));
        Assert.That(config.BaseProjectileDamage, Is.EqualTo(10f).Within(0.01f));
        Assert.That(config.BuffChance, Is.EqualTo(0.5f).Within(0.001f));

        Assert.That(config.RedProjectileSprite, Is.Not.Null);
        Assert.That(config.RedProjectileSprite.name, Is.EqualTo("EnemyBossProjectile_0"));
        Assert.That(config.BlueProjectileSprite, Is.Not.Null);
        Assert.That(config.BlueProjectileSprite.name, Is.EqualTo("EnemyBossProjectile_1"));

        Assert.That(charged, Is.Not.Null);
        Assert.That(normal, Is.Not.Null);
        Assert.That(config.RedChargeDuration, Is.EqualTo(EnemyApproach.ChargedLungeDuration).Within(0.001f));
        Assert.That(config.BlueBaseFlightDuration, Is.EqualTo(normal.ApproachDuration).Within(0.001f));
    }

    [Test]
    public void BossPrefab_HasSpinnerAnimatorAndHitboxWired()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Boss/Boss.prefab");
        Assert.That(prefab, Is.Not.Null);
        var encounter = prefab.GetComponent<BossEncounter>();
        Assert.That(encounter, Is.Not.Null);

        var serialized = new SerializedObject(encounter);
        var spinner = serialized.FindProperty("spinnerAnimator").objectReferenceValue as SpriteAnimator;
        Assert.That(spinner, Is.Not.Null);
        Assert.That(spinner.HasAnimation("Spin"), Is.True);
        Assert.That(spinner.HasAnimation("Idle"), Is.True);
        Assert.That(serialized.FindProperty("hitbox").objectReferenceValue, Is.Not.Null);
        Assert.That(serialized.FindProperty("attackPrefab").objectReferenceValue, Is.Not.Null);
    }

    [Test]
    public void BossRoulette_EvaluatesSymmetricBuffAndDebuffPercentages()
    {
        var config = ScriptableObject.CreateInstance<BossConfig>();
        try
        {
            BossRouletteResult buffDmg = config.EvaluateRoulette(BossRouletteEffect.BuffDamage);
            BossRouletteResult debuffDmg = config.EvaluateRoulette(BossRouletteEffect.DebuffDamage);
            BossRouletteResult buffSpeed = config.EvaluateRoulette(BossRouletteEffect.BuffSpeed);
            BossRouletteResult debuffSpeed = config.EvaluateRoulette(BossRouletteEffect.DebuffSpeed);

            // Damage: base 10, +5 = 15 (+50%), debuff = -50% = 5.
            Assert.That(buffDmg.IsBuff, Is.True);
            Assert.That(buffDmg.ProjectileDamage, Is.EqualTo(15f).Within(0.001f));
            Assert.That(debuffDmg.IsBuff, Is.False);
            Assert.That(debuffDmg.ProjectileDamage, Is.EqualTo(5f).Within(0.001f));

            // Speed: Red 10s -> 8s (-20%) / 12s (+20%), Blue 8.5s at 1.2x (+20%) / 0.8x (-20%).
            Assert.That(buffSpeed.IsBuff, Is.True);
            Assert.That(buffSpeed.RedWaitDuration, Is.EqualTo(8f).Within(0.001f));
            Assert.That(buffSpeed.BlueSpeedMultiplier, Is.EqualTo(1.2f).Within(0.001f));
            Assert.That(buffSpeed.BlueFlightDuration, Is.EqualTo(8.5f / 1.2f).Within(0.01f));

            Assert.That(debuffSpeed.IsBuff, Is.False);
            Assert.That(debuffSpeed.RedWaitDuration, Is.EqualTo(12f).Within(0.001f));
            Assert.That(debuffSpeed.BlueSpeedMultiplier, Is.EqualTo(0.8f).Within(0.001f));
            Assert.That(debuffSpeed.BlueFlightDuration, Is.EqualTo(8.5f / 0.8f).Within(0.01f));

            // 50/50 outcome selection
            Assert.That(config.RollRoulette(0.25f, 0.2f).Effect, Is.EqualTo(BossRouletteEffect.BuffDamage));
            Assert.That(config.RollRoulette(0.25f, 0.8f).Effect, Is.EqualTo(BossRouletteEffect.BuffSpeed));
            Assert.That(config.RollRoulette(0.75f, 0.2f).Effect, Is.EqualTo(BossRouletteEffect.DebuffDamage));
            Assert.That(config.RollRoulette(0.75f, 0.8f).Effect, Is.EqualTo(BossRouletteEffect.DebuffSpeed));
        }
        finally
        {
            Object.DestroyImmediate(config);
        }
    }
}
