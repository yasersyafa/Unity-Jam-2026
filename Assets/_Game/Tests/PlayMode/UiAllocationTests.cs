using System;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UiAllocationTests
{
    private GameObject root;
    private SlotGameplayFlow flow;
    private TextMeshProUGUI label;
    private Slider slider;
    private ShootingController shooting;
    private WeaponData weapon;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    private static Action Bind(object target, string method) => (Action)Delegate.CreateDelegate(typeof(Action), target, target.GetType().GetMethod(method, Private));
    [SetUp] public void SetUp()
    {
        root = new GameObject("UI allocation fixture");
        root.SetActive(false);
        flow = root.AddComponent<SlotGameplayFlow>();
        label = new GameObject("Label", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(root.transform, false);
        slider = new GameObject("Slider", typeof(RectTransform)).AddComponent<Slider>();
        slider.transform.SetParent(root.transform, false);
        weapon = ScriptableObject.CreateInstance<WeaponData>();
        weapon.name = "Uzi";
        Set(weapon, "maxAmmo", 120);
        // The root stays inactive, so the controller never needs its scene references; equipping fills the ammo.
        shooting = root.AddComponent<ShootingController>();
        shooting.EquipWeapon(weapon);
        Set(flow, "shooting", shooting);
        Set(shooting, "ammo", 85);
        flow.ConfigureAmmoUI(slider, label);
    }
    [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(root); UnityEngine.Object.DestroyImmediate(weapon); }
    [Test] public void AmmoText_UnchangedDisplayDoesNotAllocateAndShotRefreshes()
    {
        var update = Bind(flow, "UpdateAmmoUI");
        update();
        string before = label.text;
        Assert.That(before, Is.EqualTo("Uzi: 85 / 120"));
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) update();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        Assert.That(bytes, Is.Zero);
        Assert.That(label.text, Is.SameAs(before));
        Assert.That(slider.value, Is.EqualTo(85f).Within(.0001f));
        Assert.That(slider.maxValue, Is.EqualTo(120f).Within(.0001f));
        Set(shooting, "ammo", 84);
        update();
        Assert.That(label.text, Is.EqualTo("Uzi: 84 / 120"));
        Assert.That(slider.value, Is.EqualTo(84f).Within(.0001f));
    }
    [Test] public void OutOfAmmo_BlocksAttackWithoutOpeningSlot()
    {
        Set(shooting, "ammo", 0);
        Assert.That(shooting.HasAmmo, Is.False);
        Assert.That(flow.IsOutOfAmmo, Is.True);
        Assert.That(flow.AmmoRatio, Is.Zero);
        Assert.That(flow.IsOpen, Is.False);
        Set(weapon, "unlimitedAmmo", true);
        Assert.That(shooting.HasAmmo, Is.True);
        Assert.That(flow.AmmoRatio, Is.EqualTo(1f));
    }
    [Test] public void StatusText_StableStateDoesNotAllocateAndTransitionRefreshes()
    {
        var rewards = root.AddComponent<SlotWeaponRewards>();
        Set(flow, "rewards", rewards);
        Set(flow, "status", label);
        var update = Bind(flow, "UpdateStatusUI");
        update();
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) update();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        Assert.That(bytes, Is.Zero);
        Assert.That(label.text, Is.EqualTo("Interact: buka slot"));
        Set(flow, "open", true);
        update();
        Assert.That(label.text, Does.StartWith("Isi weapon: "));
        Set(flow, "open", false);
    }
    [Test] public void EnemyHp_UnchangedDisplayDoesNotAllocateAndDamageRefreshes()
    {
        var data = ScriptableObject.CreateInstance<EnemyData>();
        var enemyObject = new GameObject("Enemy allocation fixture");
        enemyObject.SetActive(false);
        try
        {
            var enemy = enemyObject.AddComponent<Enemy>();
            Set(enemy, "data", data);
            enemyObject.SetActive(true);
            var bar = enemyObject.AddComponent<EnemyHealthBar>();
            var update = Bind(bar, "UpdateDisplay");
            update();
            var text = enemyObject.GetComponentInChildren<TextMeshPro>();
            string before = text.text;
            long start = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) update();
            long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
            Assert.That(bytes, Is.Zero);
            Assert.That(text.text, Is.SameAs(before));
            enemy.TakeDamage(1f);
            update();
            Assert.That(text.text, Is.EqualTo("29/30"));
            bar.enabled = false;
            bar.enabled = true;
            Assert.That(text.text, Is.EqualTo("29/30"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(enemyObject);
            UnityEngine.Object.DestroyImmediate(data);
        }
    }
}
