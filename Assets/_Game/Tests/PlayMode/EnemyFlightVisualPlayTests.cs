using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PrimeTween;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class EnemyFlightVisualPlayTests
{
    private GameObject root;
    private EnemyData data;
    private Enemy enemy;
    private Transform left, right;

    [SetUp] public void SetUp()
    {
        data = ScriptableObject.CreateInstance<EnemyData>();
        root = new GameObject("Flying enemy");
        root.SetActive(false); // assign data before Enemy.Awake runs
        enemy = root.AddComponent<Enemy>();
        typeof(Enemy).GetField("data", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(enemy, data);
        left = Wing("LeftWing");
        right = Wing("RightWing");
        root.AddComponent<EnemyFlightVisual>();
        root.SetActive(true);
    }

    [TearDown] public void TearDown()
    {
        if (root != null) Object.DestroyImmediate(root);
        Object.DestroyImmediate(data);
    }

    private Transform Wing(string name)
    {
        var wing = new GameObject(name, typeof(SpriteRenderer)).transform;
        wing.SetParent(root.transform, false);
        return wing;
    }

    [UnityTest] public IEnumerator Wings_FlapWhileAliveAndResetWhenDisabled()
    {
        Assert.That(Tween.GetTweensCount(left), Is.EqualTo(1));
        Assert.That(Tween.GetTweensCount(right), Is.EqualTo(1));
        yield return new WaitForSeconds(0.3f); // past the random start offset
        Assert.That(Quaternion.Angle(left.localRotation, Quaternion.identity), Is.GreaterThan(0.01f));

        root.SetActive(false);
        Assert.That(Tween.GetTweensCount(left), Is.Zero);
        Assert.That(Tween.GetTweensCount(right), Is.Zero);
        Assert.That(left.localRotation, Is.EqualTo(Quaternion.identity));
        Assert.That(right.localRotation, Is.EqualTo(Quaternion.identity));
    }

    [UnityTest] public IEnumerator Destroy_LeavesNoTweens()
    {
        Object.Destroy(root);
        yield return null;
        Assert.That(Tween.GetTweensCount(left), Is.Zero);
        Assert.That(Tween.GetTweensCount(right), Is.Zero);
    }

    [Test] public void Death_StopsAndHidesWings()
    {
        Assert.That(Tween.GetTweensCount(left), Is.EqualTo(1), "wings must be flapping before death");
        Assert.That(enemy.isActiveAndEnabled, Is.True, "enemy must be alive and enabled");
        bool died = false;
        enemy.Died += () => died = true;
        enemy.TakeDamage(float.MaxValue);
        Assert.That(died, Is.True, "lethal damage must raise Enemy.Died");
        Assert.That(Tween.GetTweensCount(left), Is.Zero, "flap tween must stop on death");
        Assert.That(left.GetComponent<SpriteRenderer>().enabled, Is.False, "left wing must hide on death");
        Assert.That(right.GetComponent<SpriteRenderer>().enabled, Is.False, "right wing must hide on death");
    }
}
