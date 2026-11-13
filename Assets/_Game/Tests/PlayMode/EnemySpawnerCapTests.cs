using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class EnemySpawnerCapTests
{
    private const BindingFlags PrivateField = BindingFlags.Instance | BindingFlags.NonPublic;

    private GameObject cameraObj;
    private GameObject parentObj;
    private GameObject playerObj;
    private GameObject spawnerObj;
    private GameObject templateHolder;
    private EnemyData enemyData;
    private EnemyFormation formation;
    private EnemyWave wave;

    [TearDown]
    public void TearDown()
    {
        if (spawnerObj != null) Object.DestroyImmediate(spawnerObj);
        if (templateHolder != null) Object.DestroyImmediate(templateHolder);
        if (parentObj != null) Object.DestroyImmediate(parentObj);
        if (playerObj != null) Object.DestroyImmediate(playerObj);
        if (cameraObj != null) Object.DestroyImmediate(cameraObj);
        if (wave != null) Object.DestroyImmediate(wave);
        if (formation != null) Object.DestroyImmediate(formation);
        if (enemyData != null) Object.DestroyImmediate(enemyData);
    }

    [UnityTest]
    public IEnumerator Wave1_HoldsMaxThreeAliveUntilOneDiesBeforeSpawningFourth()
    {
        cameraObj = new GameObject("TestCamera");
        var cam = cameraObj.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5f;
        cameraObj.transform.position = new Vector3(0f, 0f, -10f);

        parentObj = new GameObject("EnemyParent");
        parentObj.transform.position = Vector3.zero;
        parentObj.transform.rotation = Quaternion.identity;

        playerObj = new GameObject("TestPlayer");
        var player = playerObj.AddComponent<PlayerHealth>();

        enemyData = ScriptableObject.CreateInstance<EnemyData>();
        SetPrivate(enemyData, "approachDuration", 20f);
        SetPrivate(enemyData, "healthPoint", 100f);

        formation = ScriptableObject.CreateInstance<EnemyFormation>();
        SetPrivate(formation, "slots", new[]
        {
            new Vector2(0.25f, 0.5f),
            new Vector2(0.40f, 0.55f),
            new Vector2(0.55f, 0.5f),
            new Vector2(0.70f, 0.55f),
            new Vector2(0.50f, 0.65f)
        });

        templateHolder = new GameObject("TemplateHolder");
        templateHolder.SetActive(false);
        var templateObj = new GameObject("EnemyTemplate");
        templateObj.transform.SetParent(templateHolder.transform);
        templateObj.AddComponent<BoxCollider2D>();
        var enemy = templateObj.AddComponent<Enemy>();
        SetPrivate(enemy, "data", enemyData);
        var approach = templateObj.AddComponent<EnemyApproach>();

        wave = ScriptableObject.CreateInstance<EnemyWave>();
        wave.minSpawnDelay = 0.05f;
        wave.maxSpawnDelay = 0.05f;
        wave.pairedSpawnChance = 0f;
        wave.groups = new[]
        {
            new EnemyWave.Group
            {
                formation = formation,
                enemies = new[] { approach, approach, approach, approach, approach }
            }
        };

        spawnerObj = new GameObject("TestSpawner");
        spawnerObj.SetActive(false);
        var spawner = spawnerObj.AddComponent<EnemySpawner>();
        SetPrivate(spawner, "gameplayCamera", cam);
        SetPrivate(spawner, "enemyParent", parentObj.transform);
        SetPrivate(spawner, "player", player);
        SetPrivate(spawner, "waves", new[] { wave });

        spawnerObj.SetActive(true);

        float deadline = Time.realtimeSinceStartup + 2f;
        while (spawner.AliveEnemyCount < 3 && Time.realtimeSinceStartup < deadline)
            yield return null;

        Assert.That(spawner.AliveEnemyCount, Is.EqualTo(3));

        // Wait longer than spawn delay; 4th enemy must NOT spawn while 3 remain alive
        yield return new WaitForSeconds(0.25f);
        Assert.That(spawner.AliveEnemyCount, Is.EqualTo(3));

        var initialEnemies = parentObj.GetComponentsInChildren<Enemy>();
        Assert.That(initialEnemies.Length, Is.EqualTo(3));
        var initialGenerations = new Dictionary<Enemy, uint>();
        foreach (var initialEnemy in initialEnemies) initialGenerations[initialEnemy] = initialEnemy.GetComponent<GameplayLease>().Generation;

        // Kill one alive enemy; 4th must spawn up to cap of 3
        initialEnemies[0].TakeDamage(999f);

        deadline = Time.realtimeSinceStartup + 2f;
        bool fourthSpawned = false;
        while (Time.realtimeSinceStartup < deadline)
        {
            foreach (var current in parentObj.GetComponentsInChildren<Enemy>())
            {
                if (!current.IsDead && (!initialGenerations.TryGetValue(current, out uint generation) || current.GetComponent<GameplayLease>().Generation != generation))
                {
                    fourthSpawned = true;
                    break;
                }
            }
            if (fourthSpawned && spawner.AliveEnemyCount == 3) break;
            yield return null;
        }

        Assert.That(fourthSpawned, Is.True);
        Assert.That(spawner.AliveEnemyCount, Is.EqualTo(3));
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, PrivateField);
        Assert.That(field, Is.Not.Null, $"Missing field {fieldName} on {target.GetType().Name}");
        field.SetValue(target, value);
    }
}
