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
        Set(flow, "weaponRemainingDuration", 10f);
        Set(flow, "currentWeaponDuration", 20f);
        flow.ConfigureDurationUI(slider, label);
    }
    [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(root); }
    [Test] public void DurationText_UnchangedDisplayDoesNotAllocateButSliderStillUpdates()
    {
        var update = Bind(flow, "UpdateWeaponDurationUI");
        update();
        string before = label.text;
        Set(flow, "weaponRemainingDuration", 9.99f);
        long start = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100; i++) update();
        long bytes = GC.GetAllocatedBytesForCurrentThread() - start;
        Assert.That(bytes, Is.Zero);
        Assert.That(label.text, Is.SameAs(before));
        Assert.That(slider.value, Is.EqualTo(9.99f).Within(.0001f));
        Set(flow, "weaponRemainingDuration", 9.8f);
        update();
        Assert.That(label.text, Does.Contain("9.8s"));
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
