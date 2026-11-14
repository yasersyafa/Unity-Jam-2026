using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class TutorialSurviveTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> created = new();
    private SurviveStep step;
    private TutorialDirector director;
    private PlayerHealth player;

    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, Private).SetValue(target, value);

    private GameObject CreateObject(string name)
    {
        var instance = new GameObject(name);
        created.Add(instance);
        return instance;
    }

    [SetUp]
    public void SetUp()
    {
        var camera = CreateObject("Tutorial test camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.orthographic = true;
        player = CreateObject("Tutorial test player").AddComponent<PlayerHealth>();

        var data = ScriptableObject.CreateInstance<EnemyData>();
        created.Add(data);
        SetField(data, "entryDuration", 0.01f);
        SetField(data, "approachDuration", 0.1f);
        var template = CreateObject("Tutorial test enemy");
        template.SetActive(false);
        SetField(template.AddComponent<Enemy>(), "data", data);
        var approach = template.AddComponent<EnemyApproach>();

        var spawnerObject = CreateObject("Tutorial test spawner");
        spawnerObject.SetActive(false);
        var spawner = spawnerObject.AddComponent<EnemySpawner>();
        spawner.StartOnEnable = false;
        SetField(spawner, "gameplayCamera", camera);
        SetField(spawner, "enemyParent", spawnerObject.transform);
        SetField(spawner, "player", player);
        spawnerObject.SetActive(true);

        var directorObject = CreateObject("Tutorial test director");
        directorObject.SetActive(false);
        director = directorObject.AddComponent<TutorialDirector>();
        SetField(director, "spawner", spawner);
        SetField(director, "player", player);
        SetField(director, "scriptedEnemy", approach);
        directorObject.SetActive(true);
        step = new SurviveStep();
        typeof(TutorialStep).GetMethod("Bind", Private).Invoke(step, new object[] { director });
        step.Enter();
    }

    private Enemy CurrentEnemy => (Enemy)typeof(SurviveStep).GetField("enemy", Private).GetValue(step);

    [UnityTest]
    public IEnumerator EnemyHitsPlayer_RetriesRepeatedly_ThenKillCompletesLesson()
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            var enemy = CurrentEnemy;
            float health = player.CurrentHealth;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (enemy.gameObject.activeSelf && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(player.CurrentHealth, Is.LessThan(health), "Enemy must actually hit the player.");
            Assert.That(enemy.gameObject.activeSelf, Is.False);
            Assert.That(step.Tick(), Is.False, "Escaping must not count as a kill.");
            Assert.That(CurrentEnemy, Is.Not.Null);
            Assert.That(CurrentEnemy.gameObject.activeInHierarchy, Is.True, "Retry must provide a shootable enemy.");
            Assert.That(CurrentEnemy.IsDead, Is.False);
        }

        CurrentEnemy.TakeDamage(1000f);
        Assert.That(step.Tick(), Is.True, "Killing the replacement must complete the lesson.");
    }

    [Test]
    public void EnemyKilledBeforeAttack_CompletesWithoutRetry()
    {
        var enemy = CurrentEnemy;
        enemy.TakeDamage(1000f);
        Assert.That(step.Tick(), Is.True);
        Assert.That(player.CurrentHealth, Is.EqualTo(player.MaximumHealth));
        Assert.That(CurrentEnemy, Is.SameAs(enemy));
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        step?.Exit();
        for (int i = created.Count - 1; i >= 0; i--)
            if (created[i] != null) Object.Destroy(created[i]);
        created.Clear();
        yield return null;
    }
}
