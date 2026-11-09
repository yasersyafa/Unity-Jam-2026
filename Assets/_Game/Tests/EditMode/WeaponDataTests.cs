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

    [Test]
    public void UsageDuration_ClampsNegativeValues()
    {
        var serialized = new SerializedObject(weapon);
        serialized.FindProperty("usageDuration").floatValue = -1;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Assert.That(weapon.UsageDuration, Is.Zero);
    }
}
