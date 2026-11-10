using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class EnemyBehaviorTests
{
    [Test]
    public void EvaluateMaxAliveEnemies_ProgressesAtWaves1_5_9_And_10()
    {
        int[] expected = { 3, 3, 3, 3, 4, 4, 4, 4, 5, 6 };
        for (int index = 0; index < expected.Length; index++)
        {
            Assert.That(EnemySpawner.EvaluateMaxAliveEnemies(index, 3, 0), Is.EqualTo(expected[index]), $"Wave {index + 1}");
        }

        Assert.That(EnemySpawner.EvaluateMaxAliveEnemies(0, 3, 7), Is.EqualTo(7));
    }

    [Test]
    public void ComputeBatchSize_NeverExceedsMaxAliveCap()
    {
        Assert.That(EnemySpawner.ComputeBatchSize(5, 0, 3, 1f, 0f), Is.EqualTo(2));
        Assert.That(EnemySpawner.ComputeBatchSize(5, 1, 3, 1f, 0f), Is.EqualTo(2));
        Assert.That(EnemySpawner.ComputeBatchSize(5, 2, 3, 1f, 0f), Is.EqualTo(1));
        Assert.That(EnemySpawner.ComputeBatchSize(5, 3, 3, 1f, 0f), Is.EqualTo(0));
        Assert.That(EnemySpawner.ComputeBatchSize(1, 0, 3, 1f, 0f), Is.EqualTo(1));
    }

    [Test]
    public void EvaluateSpawnDelay_RespectsRangeAcrossAllWaves()
    {
        for (int wave = 0; wave < 10; wave++)
        {
            Assert.That(EnemySpawner.EvaluateSpawnDelay(1f, 2.5f, wave, 10, 3.5f, 0f), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(EnemySpawner.EvaluateSpawnDelay(1f, 2.5f, wave, 10, 3.5f, 1f), Is.EqualTo(2.5f).Within(0.0001f));
            float mid = EnemySpawner.EvaluateSpawnDelay(1f, 2.5f, wave, 10, 3.5f, 0.5f);
            Assert.That(mid, Is.InRange(1f, 2.5f));
        }
    }

    [Test]
    public void EvaluateSpawnDelay_FavorsFasterDelaysAsWaveIncreases()
    {
        float previous = float.MaxValue;
        for (int wave = 0; wave < 10; wave++)
        {
            float sample = EnemySpawner.EvaluateSpawnDelay(1f, 2.5f, wave, 10, 3.5f, 0.5f);
            Assert.That(sample, Is.LessThan(previous));
            previous = sample;
        }
    }

    [Test]
    public void EvaluateCenterEntryViewport_StaysCenteredAroundEntryPoint()
    {
        Vector2 center = new Vector2(0.5f, 0.5f);
        Vector2 offset = new Vector2(0.08f, 0.05f);

        Vector2 minEntry = EnemySpawner.EvaluateCenterEntryViewport(center, offset, -1f, -1f);
        Vector2 maxEntry = EnemySpawner.EvaluateCenterEntryViewport(center, offset, 1f, 1f);

        Assert.That(minEntry.x, Is.EqualTo(0.42f).Within(0.0001f));
        Assert.That(minEntry.y, Is.EqualTo(0.45f).Within(0.0001f));
        Assert.That(maxEntry.x, Is.EqualTo(0.58f).Within(0.0001f));
        Assert.That(maxEntry.y, Is.EqualTo(0.55f).Within(0.0001f));
    }

    [Test]
    public void EvaluateSpiralPosition_StartsAtDestinationAndExpandsAlmostFullScreenAtShootableSpeed()
    {
        Vector3 destination = new Vector3(-4f, 2f, 0f);
        Vector3 center = Vector3.zero;
        Vector3 right = new Vector3(17.78f, 0f, 0f);
        Vector3 up = new Vector3(0f, 10f, 0f);

        Vector3 start = EnemyApproach.EvaluateSpiralPosition(
            destination, center, right, up,
            elapsed: 0f, approachProgress: 0f, phase: 0.7f, spinDirection: 1f,
            startRadius: 0.06f, endRadius: 0.42f, period: 5.5f, centerPull: 0.9f,
            amplitudeMultiplier: 1f, periodMultiplier: 1f, verticalScale: 0.84f);
        Assert.That(Vector3.Distance(start, destination), Is.LessThan(0.0001f));

        Vector3 earlyOrbitCenter = Vector3.Lerp(destination, center, 0.9f * 0.15f);
        Vector3 lateOrbitCenter = Vector3.Lerp(destination, center, 0.9f * 0.95f);
        Vector3 earlyPos = EnemyApproach.EvaluateSpiralPosition(
            destination, center, right, up,
            elapsed: 1.275f, approachProgress: 0.15f, phase: 0f, spinDirection: 1f,
            startRadius: 0.06f, endRadius: 0.42f, period: 5.5f, centerPull: 0.9f,
            amplitudeMultiplier: 1f, periodMultiplier: 1f, verticalScale: 0.84f);
        Vector3 latePos = EnemyApproach.EvaluateSpiralPosition(
            destination, center, right, up,
            elapsed: 8.075f, approachProgress: 0.95f, phase: 0f, spinDirection: 1f,
            startRadius: 0.06f, endRadius: 0.42f, period: 5.5f, centerPull: 0.9f,
            amplitudeMultiplier: 1.4f, periodMultiplier: 0.8f, verticalScale: 0.84f);

        Assert.That(Vector3.Distance(latePos, lateOrbitCenter), Is.GreaterThan(Vector3.Distance(earlyPos, earlyOrbitCenter)));
        Assert.That(Vector3.Distance(lateOrbitCenter, center), Is.LessThan(Vector3.Distance(earlyOrbitCenter, center)));

        // Verify continuous trajectory stays inside screen and keeps trackable speed even on Wave 10
        float maxSpeed = 0f;
        Vector3 prev = start;
        const float duration = 8.5f;
        const int steps = 170;
        float dt = duration / steps;
        for (int step = 1; step <= steps; step++)
        {
            float progress = step / (float)steps;
            float elapsed = progress * duration;
            Vector3 p = EnemyApproach.EvaluateSpiralPosition(
                destination, center, right, up,
                elapsed: elapsed, approachProgress: progress, phase: 0f, spinDirection: 1f,
                startRadius: 0.06f, endRadius: 0.42f, period: 5.5f, centerPull: 0.9f,
                amplitudeMultiplier: 1.4f, periodMultiplier: 0.8f, verticalScale: 0.84f);

            float speed = Vector3.Distance(p, prev) / dt;
            maxSpeed = Mathf.Max(maxSpeed, speed);
            prev = p;

            Assert.That(Mathf.Abs(p.x), Is.LessThanOrEqualTo(right.x * EnemyApproach.SafeSpiralViewportHalfWidth + 0.01f));
            Assert.That(Mathf.Abs(p.y), Is.LessThanOrEqualTo(up.y * EnemyApproach.SafeSpiralViewportHalfHeight + 0.01f));
        }
        Assert.That(maxSpeed, Is.LessThan(6.5f));

        // Verify outer spiral envelope across phases covers almost 1 full screen
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        for (int pStep = 0; pStep < 32; pStep++)
        {
            float phase = (pStep / 32f) * Mathf.PI * 2f;
            Vector3 p = EnemyApproach.EvaluateSpiralPosition(
                destination, center, right, up,
                elapsed: 8.5f, approachProgress: 1f, phase: phase, spinDirection: 1f,
                startRadius: 0.06f, endRadius: 0.42f, period: 5.5f, centerPull: 0.9f,
                amplitudeMultiplier: 1f, periodMultiplier: 1f, verticalScale: 0.84f);
            minX = Mathf.Min(minX, p.x);
            maxX = Mathf.Max(maxX, p.x);
            minY = Mathf.Min(minY, p.y);
            maxY = Mathf.Max(maxY, p.y);
        }

        Assert.That(maxX - minX, Is.GreaterThan(right.x * 0.78f));
        Assert.That(maxY - minY, Is.GreaterThan(up.y * 0.65f));
    }

    [Test]
    public void EvaluateSpiralPosition_OppositeSpinDirectionsMoveSymmetricallyOppositeInY()
    {
        Vector3 right = new Vector3(17.78f, 0f, 0f);
        Vector3 up = new Vector3(0f, 10f, 0f);

        Vector3 ccw = EnemyApproach.EvaluateSpiralPosition(
            Vector3.zero, Vector3.zero, right, up,
            elapsed: 1.1458f, approachProgress: 0.5f, phase: 0f, spinDirection: 1f,
            startRadius: 0.06f, endRadius: 0.42f, period: 5.5f, centerPull: 0f,
            amplitudeMultiplier: 1f, periodMultiplier: 1f, verticalScale: 0.84f);
        Vector3 cw = EnemyApproach.EvaluateSpiralPosition(
            Vector3.zero, Vector3.zero, right, up,
            elapsed: 1.1458f, approachProgress: 0.5f, phase: 0f, spinDirection: -1f,
            startRadius: 0.06f, endRadius: 0.42f, period: 5.5f, centerPull: 0f,
            amplitudeMultiplier: 1f, periodMultiplier: 1f, verticalScale: 0.84f);

        Assert.That(ccw.y, Is.EqualTo(-cw.y).Within(0.001f));
        Assert.That(Mathf.Abs(ccw.y), Is.GreaterThan(1.5f));
    }

    [Test]
    public void TransformSlotViewport_MirrorsAndClampsSafely()
    {
        Vector2 mirrored = EnemySpawner.TransformSlotViewport(new Vector2(0.24f, 0.56f), true, Vector2.zero, Vector2.zero);
        Assert.That(mirrored.x, Is.EqualTo(0.76f).Within(0.0001f));
        Assert.That(mirrored.y, Is.EqualTo(0.56f).Within(0.0001f));

        Vector2 clamped = EnemySpawner.TransformSlotViewport(new Vector2(0.01f, 0.99f), false, Vector2.zero, Vector2.zero);
        Assert.That(clamped.x, Is.InRange(0.18f, 0.82f));
        Assert.That(clamped.y, Is.InRange(0.38f, 0.74f));
    }

    [Test]
    public void Waves01To10_HaveValidFormationsAndMatchingEnemyCounts()
    {
        for (int i = 1; i <= 10; i++)
        {
            string path = $"Assets/_Game/ScriptableObjects/Waves/Wave{i:D2}.asset";
            var wave = AssetDatabase.LoadAssetAtPath<EnemyWave>(path);
            Assert.That(wave, Is.Not.Null, path);
            Assert.That(wave.groups, Is.Not.Null.And.Not.Empty, path);
            foreach (var group in wave.groups)
            {
                Assert.That(group.formation, Is.Not.Null, path);
                Assert.That(group.enemies, Is.Not.Null, path);
                Assert.That(group.enemies.Length, Is.EqualTo(group.formation.Count), path);
                Assert.That(group.enemies, Is.All.Not.Null, path);
            }
            Assert.That(wave.minSpawnDelay, Is.EqualTo(1f).Within(0.001f), path);
            Assert.That(wave.maxSpawnDelay, Is.EqualTo(2.5f).Within(0.001f), path);
        }
    }
}
