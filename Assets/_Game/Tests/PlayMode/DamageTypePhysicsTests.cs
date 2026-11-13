using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DamageTypePhysicsTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> created = new();
    private Camera camera;

    /// <summary>Records damage and slows so tests can assert on them.</summary>
    private sealed class TestTarget : MonoBehaviour, IDamageable, ISlowable
    {
        public float Damage;
        public int Hits;
        public float SlowMultiplier = 1f, SlowDuration;
        public void TakeDamage(float damage) { Damage += damage; Hits++; }
        public void ApplySlow(float speedMultiplier, float duration) { SlowMultiplier = speedMultiplier; SlowDuration = duration; }
    }

    [SetUp] public void SetUp()
    {
        var cameraObject = Track(new GameObject("Damage test camera"));
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
    }

    [TearDown] public void TearDown()
    {
        foreach (var item in created) if (item != null) Object.DestroyImmediate(item);
        created.Clear();
    }

    private T Track<T>(T item) where T : Object { created.Add(item); return item; }

    private TestTarget Target(Vector2 position, float size = 0.5f, float depth = 0f)
    {
        var go = Track(new GameObject("Target"));
        go.transform.position = new Vector3(position.x, position.y, depth);
        go.AddComponent<BoxCollider2D>().size = Vector2.one * size;
        return go.AddComponent<TestTarget>();
    }

    private T Damage<T>(params (string field, object value)[] fields) where T : ScriptableObject
    {
        var type = Track(ScriptableObject.CreateInstance<T>());
        foreach (var (field, value) in fields)
            (typeof(T).GetField(field, Private) ?? typeof(DamageType).GetField(field, Private)).SetValue(type, value);
        return type;
    }

    private bool Shoot(DamageType type, Vector2 aimWorld, float damage = 100f)
    {
        Physics2D.SyncTransforms();
        Vector2 aim = camera.WorldToScreenPoint(new Vector3(aimWorld.x, aimWorld.y, 0f));
        return type.Apply(new ShotContext(camera, aim, damage, ~0));
    }

    [Test] public void Single_DamagesOnlyTheTargetUnderTheAim()
    {
        var aimed = Target(Vector2.zero);
        var other = Target(new Vector2(1f, 0f));
        Assert.That(Shoot(Damage<SingleTargetDamage>(), Vector2.zero), Is.True);
        Assert.That(aimed.Damage, Is.EqualTo(100f));
        Assert.That(other.Hits, Is.Zero);
        Assert.That(Shoot(Damage<SingleTargetDamage>(), new Vector2(0f, 4f)), Is.False);
    }

    [UnityTest]
    public IEnumerator VisualOnlyPreview_RetainsDestination_AndNeverReappliesDamage()
    {
        var target = Target(Vector2.zero);
        Physics2D.SyncTransforms();
        Vector2 aim = camera.WorldToScreenPoint(Vector3.zero);
        var shot = new ShotContext(camera, aim, 100f, ~0);
        ShotVisualContext visual = ShotVisualContext.Capture(shot);
        Assert.That(visual.HasImpact, Is.True);
        Assert.That(Vector2.Distance(visual.DestinationScreen, aim), Is.LessThan(0.1f));
        Assert.That(target.Hits, Is.Zero, "Capturing presentation must not damage anything.");
        Assert.That(Damage<SingleTargetDamage>().Apply(shot), Is.True);
        Assert.That(target.Hits, Is.EqualTo(1), "Explicit hitscan damage is independent of a visual-only preview.");

        var hud = Track(new GameObject("Shot HUD", typeof(RectTransform), typeof(Canvas)));
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var effects = new GameObject("Shot effects", typeof(RectTransform));
        effects.transform.SetParent(hud.transform, false);
        var presentation = effects.AddComponent<WeaponShotPresentation>();
        presentation.Initialize(canvas, (RectTransform)hud.transform);
        var profile = Track(ScriptableObject.CreateInstance<WeaponShotVisualProfile>());
        typeof(WeaponShotVisualProfile).GetField("mode", Private).SetValue(profile, WeaponShotVisualProfile.VisualMode.Projectile);
        typeof(WeaponShotVisualProfile).GetField("speed", Private).SetValue(profile, 100000f);
        presentation.SetWeapon(profile, null);
        presentation.PlayShot(visual);
        target.transform.position = Vector3.one * 50f;
        yield return new WaitForSeconds(0.3f);
        Assert.That(target.Hits, Is.EqualTo(1));
        Assert.That(presentation.ActiveEffectCount, Is.Zero);

        Object.DestroyImmediate(target.gameObject);
        presentation.PlayShot(visual);
        yield return new WaitForSeconds(0.3f);
        Assert.That(presentation.ActiveEffectCount, Is.Zero, "A destroyed target must not invalidate the saved destination.");
    }

    [Test] public void Piercing_DamagesStackedTargetsUpToTheLimit()
    {
        var front = Target(Vector2.zero, depth: 0f);
        var middle = Target(Vector2.zero, depth: 1f);
        var back = Target(Vector2.zero, depth: 2f);
        Shoot(Damage<PiercingDamage>(("maxTargets", 2)), Vector2.zero);
        Assert.That(front.Hits + middle.Hits + back.Hits, Is.EqualTo(2));
        Assert.That(front.Hits, Is.EqualTo(1));
    }

    [Test]
    public void ProjectileLock_UsesHitboxCenterAndTracksMovement_WhileHitscanKeepsAimPoint()
    {
        var target = Target(Vector2.zero, size: 2f);
        var collider = target.GetComponent<BoxCollider2D>();
        collider.offset = new Vector2(0.25f, 0.1f);
        Physics2D.SyncTransforms();
        Vector2 aim = camera.WorldToScreenPoint(new Vector3(0.8f, 0.6f, 0f));
        var shot = new ShotContext(camera, aim, 100f, ~0);
        ShotVisualContext projectile = ShotVisualContext.Capture(shot, trackTarget: true);
        Vector2 center = camera.WorldToScreenPoint(collider.bounds.center);
        Assert.That(projectile.HasImpact, Is.True);
        Assert.That(Vector2.Distance(projectile.DestinationScreen, center), Is.LessThan(0.1f));
        Assert.That(Vector2.Distance(projectile.DestinationScreen, aim), Is.GreaterThan(1f));
        Assert.That(Vector2.Distance(ShotVisualContext.Capture(shot).DestinationScreen, aim), Is.LessThan(0.1f));
        target.transform.position = new Vector2(2f, 1f);
        Physics2D.SyncTransforms();
        Assert.That(projectile.TryGetTargetScreenPosition(out Vector2 moved), Is.True);
        Assert.That(Vector2.Distance(moved, camera.WorldToScreenPoint(collider.bounds.center)), Is.LessThan(0.1f));
        Assert.That(target.Hits, Is.Zero);
        collider.enabled = false;
        Assert.That(projectile.TryGetTargetScreenPosition(out _), Is.False);
        Object.DestroyImmediate(target.gameObject);
        Assert.That(projectile.TryGetTargetScreenPosition(out _), Is.False);
    }

    [Test]
    public void ProjectileMiss_KeepsCursorDestination_AndDoesNotLockToOtherEnemies()
    {
        Target(Vector2.zero);
        Physics2D.SyncTransforms();
        Vector2 aim = camera.WorldToScreenPoint(new Vector3(3f, 4f, 0f));
        var visual = ShotVisualContext.Capture(new ShotContext(camera, aim, 100f, ~0), trackTarget: true);
        Assert.That(visual.HasImpact, Is.False);
        Assert.That(visual.DestinationScreen, Is.EqualTo(aim));
        Assert.That(visual.TryGetTargetScreenPosition(out _), Is.False);
    }

    [Test]
    public void ProjectileWallHit_UsesImpactPoint_WithoutTargetLock()
    {
        var wall = Track(new GameObject("Wall", typeof(BoxCollider2D)));
        wall.GetComponent<BoxCollider2D>().size = new Vector2(2f, 2f);
        Physics2D.SyncTransforms();
        Vector2 aim = camera.WorldToScreenPoint(new Vector3(0.7f, 0.3f, 0f));
        var visual = ShotVisualContext.Capture(new ShotContext(camera, aim, 100f, ~0), trackTarget: true);
        Assert.That(visual.HasImpact, Is.True);
        Assert.That(Vector2.Distance(visual.DestinationScreen, aim), Is.LessThan(0.1f));
        Assert.That(visual.TryGetTargetScreenPosition(out _), Is.False);
    }

    [Test] public void Explosion_DamagesWithinRadiusWithFalloffAndOnlyOnHit()
    {
        var direct = Target(Vector2.zero);
        var near = Target(new Vector2(1f, 0f));
        var far = Target(new Vector2(5f, 0f));
        var explosion = Damage<ExplosionDamage>(("radius", 2f), ("edgeDamage", 0.5f));
        Assert.That(Shoot(explosion, Vector2.zero), Is.True);
        Assert.That(direct.Damage, Is.EqualTo(100f));
        // Falloff uses the collider's closest point (~0.75 away; Box2D adds a ~0.01 polygon skin), so derive it.
        float distance = Vector2.Distance(Vector2.zero, near.GetComponent<Collider2D>().ClosestPoint(Vector2.zero));
        Assert.That(near.Damage, Is.EqualTo(100f * ExplosionDamage.Falloff(distance, 2f, 0.5f)).Within(0.01f));
        Assert.That(near.Damage, Is.InRange(80f, 82f), "partial damage between the full centre and the 50% edge");
        Assert.That(far.Hits, Is.Zero);

        Assert.That(Shoot(explosion, new Vector2(1f, 4f)), Is.False, "missing everything must not explode");
        Assert.That(near.Hits, Is.EqualTo(1));
    }

    [Test] public void Cone_DamagesTargetsInsideTheConeFromTheMuzzle()
    {
        // Muzzle is bottom centre of the screen: (0, -5) for an orthographic size of 5.
        var ahead = Target(Vector2.zero);
        var further = Target(new Vector2(0f, 2f));
        var outside = Target(new Vector2(4f, -4f));
        Assert.That(Shoot(Damage<ConeDamage>(("range", 9f), ("halfAngle", 14f), ("edgeDamage", 1f)), new Vector2(0f, 1f)), Is.True);
        Assert.That(ahead.Hits, Is.EqualTo(1));
        Assert.That(further.Hits, Is.EqualTo(1));
        Assert.That(outside.Hits, Is.Zero);
    }

    [Test] public void TargetWithSeveralColliders_IsDamagedOncePerShot()
    {
        var target = Target(Vector2.zero);
        target.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one * 0.8f;
        Shoot(Damage<PiercingDamage>(("maxTargets", 5)), Vector2.zero);
        Assert.That(target.Hits, Is.EqualTo(1));
    }

    [Test] public void SlowEffect_IsAppliedToDamagedTargets()
    {
        var slow = Damage<SlowEffect>(("speedMultiplier", 0.6f), ("duration", 1.5f));
        var single = Damage<SingleTargetDamage>(("effects", new HitEffect[] { slow }));
        var target = Target(Vector2.zero);
        Shoot(single, Vector2.zero);
        Assert.That(target.SlowMultiplier, Is.EqualTo(0.6f).Within(0.0001f));
        Assert.That(target.SlowDuration, Is.EqualTo(1.5f).Within(0.0001f));
    }

    [Test] public void EnemyApproach_StrongestSlowWinsAndRefreshes()
    {
        var go = Track(new GameObject("Slowed enemy"));
        go.SetActive(false); // no Awake: the approach only needs its slow state here
        var approach = go.AddComponent<EnemyApproach>();
        Assert.That(approach.SpeedMultiplier, Is.EqualTo(1f));
        approach.ApplySlow(0.6f, 1.5f);
        approach.ApplySlow(0.8f, 0.5f);
        Assert.That(approach.SpeedMultiplier, Is.EqualTo(0.6f).Within(0.0001f));
    }
}
