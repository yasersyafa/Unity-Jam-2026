using NUnit.Framework;
using UnityEngine;

public sealed class DamageTypeTests
{
    [TestCase(0f, 1f)]
    [TestCase(1f, 0.75f)]
    [TestCase(2f, 0.5f)]
    [TestCase(2.01f, 0f)]
    public void ExplosionFalloff_FullAtCentreEdgeDamageAtRadiusZeroOutside(float distance, float expected)
    {
        Assert.That(ExplosionDamage.Falloff(distance, 2f, 0.5f), Is.EqualTo(expected).Within(0.0001f));
    }

    [TestCase(0f, 5f, true)]    // straight ahead
    [TestCase(1f, 5f, true)]    // ~11 degrees off axis
    [TestCase(3f, 5f, false)]   // ~31 degrees off axis
    [TestCase(0f, 10f, false)]  // beyond range
    [TestCase(0f, -1f, false)]  // behind the origin
    public void IsInsideCone_ChecksAngleAndRange(float x, float y, bool expected)
    {
        Assert.That(ConeDamage.IsInsideCone(Vector2.zero, Vector2.up, new Vector2(x, y), 9f, 14f), Is.EqualTo(expected));
    }

    [Test]
    public void WeaponWithoutDamageType_UsesSingleTargetDefault()
    {
        var weapon = ScriptableObject.CreateInstance<WeaponData>();
        try
        {
            Assert.That(weapon.DamageType, Is.InstanceOf<SingleTargetDamage>());
            Assert.That(weapon.DamageType, Is.SameAs(DamageType.Default));
        }
        finally { Object.DestroyImmediate(weapon); }
    }
}
