using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class WeaponDataTests
{
    private WeaponData weapon;
    [SetUp] public void SetUp() { weapon = ScriptableObject.CreateInstance<WeaponData>(); }
    [TearDown] public void TearDown() { Object.DestroyImmediate(weapon); }

    [TestCase(10f, 0.1f)]
    [TestCase(0f, 10f)]
    [TestCase(-5f, 10f)]
    public void ShotInterval_UsesFireRateAndProtectsAgainstZero(float rate, float interval)
    {
        var serialized = new SerializedObject(weapon);
        serialized.FindProperty("fireRate").floatValue = rate;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(weapon.ShotInterval, Is.EqualTo(interval).Within(0.0001f));
    }

    [TestCase(120, 120)]
    [TestCase(0, 1)]
    [TestCase(-5, 1)]
    public void MaxAmmo_ClampsToAtLeastOneShot(int value, int expected)
    {
        var serialized = new SerializedObject(weapon);
        serialized.FindProperty("maxAmmo").intValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(weapon.MaxAmmo, Is.EqualTo(expected));
    }

    [Test]
    public void UnlimitedAmmo_DefaultsToLimited()
    {
        Assert.That(weapon.UnlimitedAmmo, Is.False);
        var serialized = new SerializedObject(weapon);
        serialized.FindProperty("unlimitedAmmo").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(weapon.UnlimitedAmmo, Is.True);
    }

    [TestCase("Assets/_Game/ScriptableObjects/Weapons/Uncommon/Ketapel.asset", 30)]
    [TestCase("Assets/_Game/ScriptableObjects/Weapons/Rare/LPG.asset", 20)]
    [TestCase("Assets/_Game/ScriptableObjects/Weapons/Epic/M4A1.asset", 75)]
    [TestCase("Assets/_Game/ScriptableObjects/Weapons/Jackpot/Machine_Gun_M134-A2.asset", 200)]
    public void WeaponAssets_HaveExpectedMaxAmmo(string assetPath, int expectedMaxAmmo)
    {
        var asset = AssetDatabase.LoadAssetAtPath<WeaponData>(assetPath);
        Assert.That(asset, Is.Not.Null, assetPath);
        Assert.That(asset.UnlimitedAmmo, Is.False, assetPath);
        Assert.That(asset.MaxAmmo, Is.EqualTo(expectedMaxAmmo), assetPath);
    }
}
