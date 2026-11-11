using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class WeaponVisualProfileTests
{
    [TestCase("Bambu_Runcing", WeaponShotVisualProfile.VisualMode.Projectile)]
    [TestCase("Batu_Biasa", WeaponShotVisualProfile.VisualMode.Projectile)]
    [TestCase("Sandal_Slipper", WeaponShotVisualProfile.VisualMode.Projectile)]
    [TestCase("Nerf_Gun", WeaponShotVisualProfile.VisualMode.Projectile)]
    [TestCase("Ketapel", WeaponShotVisualProfile.VisualMode.Projectile)]
    [TestCase("Water_Gun", WeaponShotVisualProfile.VisualMode.Stream)]
    [TestCase("Durian", WeaponShotVisualProfile.VisualMode.Projectile)]
    [TestCase("LPG", WeaponShotVisualProfile.VisualMode.Projectile)]
    [TestCase("Glock_Pistol", WeaponShotVisualProfile.VisualMode.Hitscan)]
    [TestCase("Uzi", WeaponShotVisualProfile.VisualMode.Hitscan)]
    [TestCase("M4A1", WeaponShotVisualProfile.VisualMode.Hitscan)]
    [TestCase("Flamethrower", WeaponShotVisualProfile.VisualMode.Stream)]
    [TestCase("AWP", WeaponShotVisualProfile.VisualMode.Hitscan)]
    [TestCase("Machine_Gun_M134-A2", WeaponShotVisualProfile.VisualMode.Hitscan)]
    [TestCase("Rocket_Launcher", WeaponShotVisualProfile.VisualMode.Projectile)]
    public void Catalog_HasExpectedVisualAndExplicitMuzzle(string name, WeaponShotVisualProfile.VisualMode expected)
    {
        var weapons = AssetDatabase.FindAssets("t:WeaponData", new[] { "Assets/_Game/ScriptableObjects/Weapons" })
            .Select(g => AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(g)));
        WeaponData weapon = weapons.Single(w => w.name == name);
        Assert.That(weapon.ShotVisualProfile, Is.Not.Null);
        Assert.That(weapon.ShotVisualProfile.Mode, Is.EqualTo(expected));
        Assert.That(weapon.VisualPrefab, Is.Not.Null);
        Assert.That(weapon.VisualPrefab.MuzzleAnchor, Is.Not.Null);
        Assert.That(weapon.VisualPrefab.MuzzleAnchor.IsChildOf(weapon.VisualPrefab.transform), Is.True);
    }

    [TestCase("Durian")]
    [TestCase("LPG")]
    public void UprightThrows_HaveNoSpinOrTravelRotation(string name)
    {
        var profile = AssetDatabase.LoadAssetAtPath<WeaponShotVisualProfile>("Assets/_Game/ScriptableObjects/WeaponVisuals/" + name + "_Visual.asset");
        Assert.That(profile.Spin, Is.Zero);
        Assert.That(profile.OrientToTravel, Is.False);
    }

    [TestCase("Durian")]
    [TestCase("LPG")]
    [TestCase("Rocket")]
    public void Explosion_HasOnePresentationOwner(string name)
    {
        var damage = AssetDatabase.LoadAssetAtPath<ExplosionDamage>("Assets/_Game/ScriptableObjects/DamageTypes/Explosion_" + name + ".asset");
        Assert.That(new SerializedObject(damage).FindProperty("impactVfx").objectReferenceValue, Is.Null);
    }

    [Test]
    public void InvalidNumericConfiguration_HasSafeRuntimeBounds()
    {
        var profile = ScriptableObject.CreateInstance<WeaponShotVisualProfile>();
        try
        {
            var data = new SerializedObject(profile);
            foreach (string key in new[] { "speed", "impactDuration", "streamLength", "streamWidth", "impactSize" })
                data.FindProperty(key).floatValue = -1f;
            data.FindProperty("size").vector2Value = Vector2.zero;
            data.FindProperty("fragmentCount").intValue = 100;
            data.ApplyModifiedPropertiesWithoutUndo();
            Assert.That(profile.Speed, Is.GreaterThan(0f));
            Assert.That(profile.ImpactDuration, Is.GreaterThan(0f));
            Assert.That(profile.StreamLength, Is.GreaterThan(0f));
            Assert.That(profile.StreamWidth, Is.GreaterThan(0f));
            Assert.That(profile.ImpactSize, Is.GreaterThan(0f));
            Assert.That(profile.Size.x, Is.GreaterThan(0f));
            Assert.That(profile.FragmentCount, Is.EqualTo(16));
        }
        finally { Object.DestroyImmediate(profile); }
    }

    [Test]
    public void LegacyWeapon_WithoutProfileRetainsDamageFallback()
    {
        var weapon = ScriptableObject.CreateInstance<WeaponData>();
        try
        {
            Assert.That(weapon.ShotVisualProfile, Is.Null);
            Assert.That(weapon.DamageType, Is.Not.Null);
        }
        finally { Object.DestroyImmediate(weapon); }
    }
}
