using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class UziAudioCueTests
{
    [TestCase(
        "Assets/_Game/ScriptableObjects/Weapons/Epic/Uzi.asset",
        "Assets/_Game/ScriptableObjects/Audio/WeaponUzi.asset",
        "Assets/_Game/Audio/SFX/Weapon/Uzi.wav",
        WeaponData.FireMode.Automatic,
        15f)]
    [TestCase(
        "Assets/_Game/ScriptableObjects/Weapons/Epic/M4A1.asset",
        "Assets/_Game/ScriptableObjects/Audio/WeaponM4.asset",
        "Assets/_Game/Audio/SFX/Weapon/M4.wav",
        WeaponData.FireMode.Automatic,
        10f)]
    [TestCase(
        "Assets/_Game/ScriptableObjects/Weapons/Jackpot/Machine_Gun_M134-A2.asset",
        "Assets/_Game/ScriptableObjects/Audio/WeaponMachineGun.asset",
        "Assets/_Game/Audio/SFX/Weapon/Machine Gun.wav",
        WeaponData.FireMode.Automatic,
        25f)]
    [TestCase(
        "Assets/_Game/ScriptableObjects/Weapons/Rare/Glock_Pistol.asset",
        "Assets/_Game/ScriptableObjects/Audio/WeaponGlock.asset",
        "Assets/_Game/Audio/SFX/Weapon/Glock.wav",
        WeaponData.FireMode.SemiAutomatic,
        4.5f)]
    [TestCase(
        "Assets/_Game/ScriptableObjects/Weapons/Jackpot/AWP.asset",
        "Assets/_Game/ScriptableObjects/Audio/WeaponAwp.asset",
        "Assets/_Game/Audio/SFX/Weapon/Awp.wav",
        WeaponData.FireMode.SemiAutomatic,
        1.5f)]
    public void Weapon_HasExpectedFireModeAndSyncedOneShotAudioCue(
        string weaponPath,
        string cuePath,
        string clipPath,
        WeaponData.FireMode expectedMode,
        float expectedFireRate)
    {
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(weaponPath);
        var cue = AssetDatabase.LoadAssetAtPath<AudioCue>(cuePath);
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);

        Assert.That(weapon, Is.Not.Null);
        Assert.That(cue, Is.Not.Null);
        Assert.That(clip, Is.Not.Null);

        Assert.That(weapon.FiringMode, Is.EqualTo(expectedMode));
        Assert.That(weapon.ShotInterval, Is.EqualTo(1f / expectedFireRate).Within(0.0001f));
        Assert.That(weapon.ShotSound, Is.SameAs(cue));

        Assert.That(cue.ui, Is.False);
        Assert.That(cue.clips, Is.Not.Null);
        Assert.That(cue.clips, Contains.Item(clip));
        Assert.That(cue.cooldown, Is.EqualTo(0f).Within(0.0001f));

        float maxPlaybackSeconds = clip.length / Mathf.Max(0.1f, Mathf.Min(cue.pitch.x, cue.pitch.y));
        int requiredVoices = Mathf.CeilToInt(maxPlaybackSeconds / weapon.ShotInterval);
        Assert.That(cue.maximumVoices, Is.GreaterThanOrEqualTo(requiredVoices));
    }

    [Test]
    public void Flamethrower_HasAutomaticStreamLoopCueStartingAtPointTwoSeconds()
    {
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/ScriptableObjects/Weapons/Epic/Flamethrower.asset");
        var cue = AssetDatabase.LoadAssetAtPath<AudioCue>("Assets/_Game/ScriptableObjects/Audio/WeaponFlamethrower.asset");
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/Weapon/Flamethrower.wav");

        Assert.That(weapon, Is.Not.Null);
        Assert.That(cue, Is.Not.Null);
        Assert.That(clip, Is.Not.Null);

        Assert.That(weapon.FiringMode, Is.EqualTo(WeaponData.FireMode.Automatic));
        Assert.That(weapon.ShotSound, Is.SameAs(cue));
        Assert.That(cue.ui, Is.False);
        Assert.That(cue.clips, Contains.Item(clip));
        Assert.That(cue.loop, Is.True);
        Assert.That(cue.loopStartSeconds, Is.EqualTo(0.2f).Within(0.0001f));
        Assert.That(clip.length, Is.GreaterThan(cue.loopStartSeconds));
    }

    [Test]
    public void EnemyHitGeneral_AndExplosionCues_AreWiredWithExpectedSuppression()
    {
        var hitCue = AssetDatabase.LoadAssetAtPath<AudioCue>("Assets/_Game/ScriptableObjects/Audio/EnemyHit.asset");
        var hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/Enemy/Enemy Hit General.wav");
        Assert.That(hitCue, Is.Not.Null);
        Assert.That(hitClip, Is.Not.Null);
        Assert.That(hitCue.ui, Is.False);
        Assert.That(hitCue.loop, Is.False);
        Assert.That(hitCue.clips, Contains.Item(hitClip));

        var lpgWeapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/ScriptableObjects/Weapons/Rare/LPG.asset");
        var lpgCue = AssetDatabase.LoadAssetAtPath<AudioCue>("Assets/_Game/ScriptableObjects/Audio/WeaponLpgExplosion.asset");
        var lpgClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/Weapon/LPG.wav");
        Assert.That(lpgWeapon.DamageType, Is.InstanceOf<ExplosionDamage>());
        Assert.That(lpgCue.clips, Contains.Item(lpgClip));
        Assert.That(new SerializedObject(lpgWeapon.DamageType).FindProperty("impactSound").objectReferenceValue, Is.SameAs(lpgCue));

        var rocketWeapon = AssetDatabase.LoadAssetAtPath<WeaponData>("Assets/_Game/ScriptableObjects/Weapons/Jackpot/Rocket_Launcher.asset");
        var rocketCue = AssetDatabase.LoadAssetAtPath<AudioCue>("Assets/_Game/ScriptableObjects/Audio/WeaponRocketExplosion.asset");
        var rocketClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/Weapon/Rocket Launcher.wav");
        Assert.That(rocketWeapon.DamageType, Is.InstanceOf<ExplosionDamage>());
        Assert.That(rocketCue.clips, Contains.Item(rocketClip));
        Assert.That(new SerializedObject(rocketWeapon.DamageType).FindProperty("impactSound").objectReferenceValue, Is.SameAs(rocketCue));

        var flameDamage = AssetDatabase.LoadAssetAtPath<DamageType>("Assets/_Game/ScriptableObjects/DamageTypes/Cone_Flamethrower.asset");
        var waterDamage = AssetDatabase.LoadAssetAtPath<DamageType>("Assets/_Game/ScriptableObjects/DamageTypes/Single_Slow.asset");
        var durianDamage = AssetDatabase.LoadAssetAtPath<DamageType>("Assets/_Game/ScriptableObjects/DamageTypes/Explosion_Durian.asset");
        var awpDamage = AssetDatabase.LoadAssetAtPath<DamageType>("Assets/_Game/ScriptableObjects/DamageTypes/Piercing_AWP.asset");

        Assert.That(new SerializedObject(flameDamage).FindProperty("suppressEnemyHitSound").boolValue, Is.True);
        Assert.That(new SerializedObject(waterDamage).FindProperty("suppressEnemyHitSound").boolValue, Is.True);
        Assert.That(durianDamage, Is.InstanceOf<ExplosionDamage>());
        Assert.That(new SerializedObject(awpDamage).FindProperty("suppressEnemyHitSound").boolValue, Is.False);
    }

    [Test]
    public void EnemyDeathCues_AreWiredToEnemyPrefabs()
    {
        var defaultCue = AssetDatabase.LoadAssetAtPath<AudioCue>("Assets/_Game/ScriptableObjects/Audio/EnemyDeath.asset");
        var tankCue = AssetDatabase.LoadAssetAtPath<AudioCue>("Assets/_Game/ScriptableObjects/Audio/EnemyDeathTank.asset");
        var rangedCue = AssetDatabase.LoadAssetAtPath<AudioCue>("Assets/_Game/ScriptableObjects/Audio/EnemyDeathRanged.asset");
        var defaultClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/Enemy/Normal and Charged enemy.wav");
        var tankClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/Enemy/Tank Enemy.wav");
        var rangedClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/Enemy/Ranged Enemy.wav");

        Assert.That(defaultCue, Is.Not.Null);
        Assert.That(tankCue, Is.Not.Null);
        Assert.That(rangedCue, Is.Not.Null);
        Assert.That(defaultClip, Is.Not.Null);
        Assert.That(tankClip, Is.Not.Null);
        Assert.That(rangedClip, Is.Not.Null);

        Assert.That(defaultCue.ui, Is.False);
        Assert.That(defaultCue.loop, Is.False);
        Assert.That(defaultCue.clips, Contains.Item(defaultClip));
        Assert.That(tankCue.ui, Is.False);
        Assert.That(tankCue.loop, Is.False);
        Assert.That(tankCue.clips, Contains.Item(tankClip));
        Assert.That(rangedCue.ui, Is.False);
        Assert.That(rangedCue.loop, Is.False);
        Assert.That(rangedCue.clips, Contains.Item(rangedClip));

        string[] defaultDeathPrefabs =
        {
            "Assets/_Game/Prefabs/Enemies/NormalEnemy.prefab",
            "Assets/_Game/Prefabs/Enemies/ChargedEnemy.prefab",
            "Assets/_Game/Prefabs/Enemies/ScoutEnemy.prefab",
            "Assets/_Game/Prefabs/Enemies/EliteEnemy.prefab"
        };
        foreach (string path in defaultDeathPrefabs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.That(prefab, Is.Not.Null, path);
            var combatAudio = prefab.GetComponentInChildren<CombatAudio>(true);
            Assert.That(combatAudio, Is.Not.Null, path);
            Assert.That(new SerializedObject(combatAudio).FindProperty("died").objectReferenceValue, Is.SameAs(defaultCue), path);
        }

        var tankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/TankEnemy.prefab");
        Assert.That(tankPrefab, Is.Not.Null);
        var tankAudio = tankPrefab.GetComponentInChildren<CombatAudio>(true);
        Assert.That(tankAudio, Is.Not.Null);
        Assert.That(new SerializedObject(tankAudio).FindProperty("died").objectReferenceValue, Is.SameAs(tankCue));

        var rangedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/RangedEnemy.prefab");
        Assert.That(rangedPrefab, Is.Not.Null);
        var rangedAudio = rangedPrefab.GetComponentInChildren<CombatAudio>(true);
        Assert.That(rangedAudio, Is.Not.Null);
        Assert.That(new SerializedObject(rangedAudio).FindProperty("died").objectReferenceValue, Is.SameAs(rangedCue));
    }
}
