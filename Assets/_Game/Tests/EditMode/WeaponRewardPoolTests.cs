using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class WeaponRewardPoolTests
{
    private readonly List<WeaponData> assets = new();
    private WeaponData Weapon(SlotTier tier)
    {
        var weapon = ScriptableObject.CreateInstance<WeaponData>();
        assets.Add(weapon);
        var serialized = new SerializedObject(weapon);
        serialized.FindProperty("tier").enumValueIndex = (int)tier;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return weapon;
    }
    [TearDown] public void TearDown()
    {
        foreach (var asset in assets) UnityEngine.Object.DestroyImmediate(asset);
        assets.Clear();
    }
    [Test] public void Pick_ReturnsWeaponFromRequestedTier()
    {
        var weapons = new List<WeaponData>();
        foreach (SlotTier tier in Enum.GetValues(typeof(SlotTier))) weapons.Add(Weapon(tier));
        weapons.Add(null);
        weapons.Add(weapons[0]);
        var pool = new WeaponRewardPool(weapons, new System.Random(10));
        Assert.That(pool.IsComplete, Is.True);
        foreach (SlotTier tier in Enum.GetValues(typeof(SlotTier)))
            for (int i = 0; i < 10; i++) Assert.That(pool.Pick(tier).Tier, Is.EqualTo(tier));
    }
    [Test] public void IncompletePool_ReportsMissingTiersAndRejectsPick()
    {
        var pool = new WeaponRewardPool(new[] { Weapon(SlotTier.Common) });
        Assert.That(pool.IsComplete, Is.False);
        Assert.That(pool.MissingTiers, Is.EqualTo("Uncommon, Rare, Epic, Jackpot"));
        Assert.Throws<InvalidOperationException>(() => pool.Pick(SlotTier.Common));
    }
}
