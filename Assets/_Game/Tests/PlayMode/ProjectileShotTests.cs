using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class ProjectileShotTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private readonly List<Object> created = new List<Object>();
    private GameObject root;
    private Camera camera;
    private ProjectileShotRunner runner;

    private sealed class Target : MonoBehaviour, IDamageable, ISlowable
    {
        public int Hits;
        public float Damage;
        public float Slow = 1f;
        public void TakeDamage(float damage) { Damage += damage; Hits++; }
        public void ApplySlow(float multiplier, float duration) { Slow = multiplier; }
    }

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Projectile tests");
        runner = root.AddComponent<ProjectileShotRunner>();
        var cameraObject = new GameObject("Camera", typeof(Camera));
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        Object.DestroyImmediate(root);
        foreach (Object item in created) if (item != null) Object.DestroyImmediate(item);
        created.Clear();
    }

    private Target Enemy(Vector2 position)
    {
        var go = new GameObject("Target", typeof(BoxCollider2D), typeof(Target));
        go.transform.SetParent(root.transform, false);
        go.transform.position = position;
        go.GetComponent<BoxCollider2D>().size = Vector2.one;
        return go.GetComponent<Target>();
    }

    private T Data<T>() where T : ScriptableObject
    {
        var asset = ScriptableObject.CreateInstance<T>();
        created.Add(asset);
        return asset;
    }

    private ProjectileShot Launch(DamageType type, Vector2 aim, float damage = 100f, float duration = 0.15f)
    {
        Physics2D.SyncTransforms();
        Vector2 screen = camera.WorldToScreenPoint(aim);
        var shot = new ShotContext(camera, screen, damage, ~0);
        return runner.Launch(shot, ShotVisualContext.Capture(shot, trackTarget: true), type, duration);
    }

    [UnityTest]
    public IEnumerator MovingLock_DamagesOnlyOnArrival_WithoutRequeryingTheCursor()
    {
        var locked = Enemy(Vector2.zero);
        var other = Enemy(new Vector2(4f, 4f));
        var flight = Launch(Data<SingleTargetDamage>(), new Vector2(0.2f, 0.2f));
        Assert.That(locked.Hits, Is.Zero);
        locked.transform.position = new Vector2(2f, 1f);
        other.transform.position = new Vector2(0.2f, 0.2f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.05f);
        Assert.That(locked.Hits, Is.Zero);
        yield return new WaitForSeconds(0.15f);
        Assert.That(locked.Damage, Is.EqualTo(100f));
        Assert.That(other.Hits, Is.Zero);
        Assert.That(flight.IsComplete, Is.True);
        yield return new WaitForSeconds(0.2f);
        Assert.That(locked.Hits, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator Explosion_UsesTargetAndNeighborsAtArrival()
    {
        var direct = Enemy(Vector2.zero);
        var incoming = Enemy(new Vector2(5f, 5f));
        var outgoing = Enemy(new Vector2(1f, 0f));
        Launch(Data<ExplosionDamage>(), Vector2.zero);
        direct.transform.position = new Vector2(1f, 1f);
        incoming.transform.position = new Vector2(2f, 1f);
        outgoing.transform.position = new Vector2(5f, 5f);
        Physics2D.SyncTransforms();
        Assert.That(direct.Hits + incoming.Hits + outgoing.Hits, Is.Zero);
        yield return new WaitForSeconds(0.2f);
        Assert.That(direct.Damage, Is.EqualTo(100f));
        Assert.That(direct.Hits, Is.EqualTo(1));
        Assert.That(incoming.Damage, Is.GreaterThan(0f));
        Assert.That(outgoing.Hits, Is.Zero);
    }

    [UnityTest]
    public IEnumerator HitEffects_AreDelayedWithDamage()
    {
        var target = Enemy(Vector2.zero);
        var type = Data<SingleTargetDamage>();
        typeof(DamageType).GetField("effects", Private).SetValue(type, new HitEffect[] { Data<SlowEffect>() });
        Launch(type, Vector2.zero);
        Assert.That(target.Slow, Is.EqualTo(1f));
        yield return new WaitForSeconds(0.2f);
        Assert.That(target.Slow, Is.EqualTo(0.6f));
        Assert.That(target.Hits, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator Miss_DoesNotAcquireAnEnemyThatMovesUnderTheCursor()
    {
        var target = Enemy(Vector2.zero);
        var flight = Launch(Data<ExplosionDamage>(), new Vector2(4f, 4f));
        Assert.That(flight.HasImpact, Is.False);
        target.transform.position = new Vector2(4f, 4f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.2f);
        Assert.That(target.Hits, Is.Zero);
    }

    [UnityTest]
    public IEnumerator LostLock_DoesNotDamageAReactivatedTarget()
    {
        var target = Enemy(Vector2.zero);
        Launch(Data<SingleTargetDamage>(), Vector2.zero, duration: 0.25f);
        target.GetComponent<Collider2D>().enabled = false;
        yield return new WaitForSeconds(0.05f);
        target.GetComponent<Collider2D>().enabled = true;
        yield return new WaitForSeconds(0.3f);
        Assert.That(target.Hits, Is.Zero);
    }

    [UnityTest]
    public IEnumerator LostExplosionTarget_ExplodesAtLastPosition()
    {
        var target = Enemy(Vector2.zero);
        var neighbor = Enemy(new Vector2(1f, 0f));
        Launch(Data<ExplosionDamage>(), Vector2.zero);
        Object.DestroyImmediate(target.gameObject);
        yield return new WaitForSeconds(0.2f);
        Assert.That(neighbor.Hits, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator Pause_FreezesDamageUntilFlightResumes()
    {
        var target = Enemy(Vector2.zero);
        Launch(Data<SingleTargetDamage>(), Vector2.zero);
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(target.Hits, Is.Zero);
        Time.timeScale = 1f;
        yield return new WaitForSeconds(0.2f);
        Assert.That(target.Hits, Is.EqualTo(1));
    }

    [UnityTest]
    public IEnumerator DisablingOwner_CancelsPendingDamage()
    {
        var target = Enemy(Vector2.zero);
        var flight = Launch(Data<SingleTargetDamage>(), Vector2.zero);
        runner.enabled = false;
        Assert.That(flight.IsCanceled, Is.True);
        yield return new WaitForSeconds(0.2f);
        Assert.That(target.Hits, Is.Zero);
        Assert.That(runner.ActiveShotCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator VisualPoolLimit_DoesNotDiscardGameplayDamage()
    {
        var target = Enemy(Vector2.zero);
        var hud = new GameObject("HUD", typeof(RectTransform), typeof(Canvas));
        hud.transform.SetParent(root.transform, false);
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var go = new GameObject("Effects", typeof(RectTransform));
        go.transform.SetParent(hud.transform, false);
        var presentation = go.AddComponent<WeaponShotPresentation>();
        presentation.Initialize(canvas, (RectTransform)hud.transform);
        var profile = Data<WeaponShotVisualProfile>();
        typeof(WeaponShotVisualProfile).GetField("mode", Private).SetValue(profile, WeaponShotVisualProfile.VisualMode.Projectile);
        presentation.SetWeapon(profile, null);
        Physics2D.SyncTransforms();
        var shot = new ShotContext(camera, camera.WorldToScreenPoint(Vector3.zero), 1f, ~0);
        var visual = ShotVisualContext.Capture(shot, trackTarget: true);
        var type = Data<SingleTargetDamage>();
        for (int i = 0; i < 129; i++) presentation.PlayShot(visual, runner.Launch(shot, visual, type, 0.1f));
        Assert.That(presentation.ActiveEffectCount, Is.EqualTo(128));
        Assert.That(runner.ActiveShotCount, Is.EqualTo(129));
        yield return new WaitForSeconds(0.15f);
        Assert.That(target.Hits, Is.EqualTo(129));
        Assert.That(target.Damage, Is.EqualTo(129f));
    }
}
