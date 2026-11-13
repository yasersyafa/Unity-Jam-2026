using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class WeaponShotPresentationTests
{
    private GameObject root;
    private WeaponShotPresentation presentation;
    private WeaponShotVisualProfile profile;
    private RectTransform muzzle;
    private Canvas canvas;
    private sealed class VisualTarget : MonoBehaviour, IDamageable
    {
        public void TakeDamage(float damage) { }
    }
    private static void Set(object target, string field, object value) => target.GetType()
        .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Weapon visual test", typeof(RectTransform), typeof(Canvas));
        canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        muzzle = (RectTransform)new GameObject("Muzzle", typeof(RectTransform)).transform;
        muzzle.SetParent(root.transform, false);
        var go = new GameObject("Effects", typeof(RectTransform));
        go.transform.SetParent(root.transform, false);
        presentation = go.AddComponent<WeaponShotPresentation>();
        presentation.Initialize(canvas, muzzle);
        profile = ScriptableObject.CreateInstance<WeaponShotVisualProfile>();
        Set(profile, "mode", WeaponShotVisualProfile.VisualMode.Projectile);
        Set(profile, "speed", 10000f);
        Set(profile, "impactDuration", 0.3f);
        Canvas.ForceUpdateCanvases();
        presentation.SetWeapon(profile, muzzle);
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(profile);
    }

    private ShotVisualContext Shot(bool impact) => new ShotVisualContext(
        RectTransformUtility.WorldToScreenPoint(null, muzzle.position) + new Vector2(200f, 100f), impact);

    private ShotVisualContext TrackingShot(out Camera camera, out BoxCollider2D target)
    {
        var cameraObject = new GameObject("Target camera", typeof(Camera));
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        var targetObject = new GameObject("Moving target", typeof(BoxCollider2D), typeof(VisualTarget));
        targetObject.transform.SetParent(root.transform, false);
        targetObject.transform.position = Vector3.zero;
        target = targetObject.GetComponent<BoxCollider2D>();
        target.size = Vector2.one * 2f;
        muzzle.anchoredPosition = new Vector2(-200f, -150f);
        Set(profile, "speed", 500f);
        Physics2D.SyncTransforms();
        Vector2 cursor = camera.WorldToScreenPoint(new Vector3(0.6f, 0.3f, 0f));
        return ShotVisualContext.Capture(new ShotContext(camera, cursor, 100f, ~0), trackTarget: true);
    }

    [UnityTest]
    public IEnumerator Hit_TravelsThenShowsImpact_AndReleasesEverything()
    {
        presentation.PlayShot(Shot(true));
        var graphic = presentation.GetComponentInChildren<WeaponEffectGraphic>();
        Assert.That(graphic.transform.parent.position, Is.EqualTo(muzzle.position));
        Assert.That(presentation.ActiveEffectCount, Is.EqualTo(1));
        yield return new WaitForSeconds(0.08f);
        Assert.That(presentation.ActiveEffectCount, Is.EqualTo(1), "Arrival retains one impact, rather than a projectile plus impact.");
        yield return new WaitForSeconds(0.35f);
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator Miss_HasNoImpactAndPoolResetsTrailOnReuse()
    {
        Set(profile, "trailColor", Color.red);
        presentation.PlayShot(Shot(false));
        yield return new WaitForSeconds(0.08f);
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
        int objects = presentation.transform.childCount;
        Set(profile, "trailColor", Color.clear);
        presentation.PlayShot(Shot(false));
        Assert.That(presentation.transform.childCount, Is.EqualTo(objects));
        Assert.That(presentation.GetComponentsInChildren<WeaponEffectGraphic>().Count(g => g.enabled), Is.EqualTo(1));
        yield return new WaitForSeconds(0.08f);
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator Stream_RequiresActualShot_FollowsMuzzle_AndStopsImmediately()
    {
        Set(profile, "mode", WeaponShotVisualProfile.VisualMode.Stream);
        presentation.SetContinuousFire(true);
        Assert.That(presentation.IsStreaming, Is.False);
        presentation.SetAim(Shot(false).DestinationScreen);
        presentation.PlayShot(Shot(false));
        Assert.That(presentation.IsStreaming, Is.True);
        for (int i = 0; i < 30; i++) presentation.PlayShot(Shot(false));
        Assert.That(presentation.ActiveEffectCount, Is.EqualTo(1));
        yield return null;
        Vector3 before = presentation.GetComponentInChildren<WeaponEffectGraphic>().transform.position;
        muzzle.anchoredPosition += new Vector2(40f, 0f);
        yield return null;
        Assert.That(presentation.GetComponentInChildren<WeaponEffectGraphic>().transform.position, Is.Not.EqualTo(before));
        presentation.SetContinuousFire(false);
        Assert.That(presentation.IsStreaming, Is.False);
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator ResetAndDisableCancelEffects_ReplacementPreservesFlights()
    {
        presentation.PlayShot(Shot(true));
        presentation.ResetEffects();
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
        presentation.PlayShot(Shot(true));
        presentation.enabled = false;
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
        presentation.enabled = true;
        presentation.PlayShot(Shot(true));
        presentation.SetWeapon(null, null);
        Assert.That(presentation.ActiveEffectCount, Is.EqualTo(1));
        presentation.PlayShot(Shot(true));
        yield return null;
        Assert.That(presentation.ActiveEffectCount, Is.EqualTo(1), "Replacing a weapon does not erase its in-flight projectile.");
        presentation.ResetEffects();
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator Hitscan_ShowsOnlyImmediateImpact_AndNoMissVisual()
    {
        Set(profile, "mode", WeaponShotVisualProfile.VisualMode.Hitscan);
        presentation.PlayShot(Shot(false));
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
        presentation.PlayShot(Shot(true));
        Assert.That(presentation.ActiveEffectCount, Is.EqualTo(1));
        yield return new WaitForSeconds(0.35f);
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator UprightProjectile_DoesNotRotate_AndPauseFreezesTravel()
    {
        Set(profile, "speed", 100f);
        Set(profile, "orientToTravel", false);
        presentation.PlayShot(Shot(false));
        yield return null;
        var graphic = presentation.GetComponentInChildren<WeaponEffectGraphic>();
        Assert.That(Quaternion.Angle(graphic.rectTransform.localRotation, Quaternion.identity), Is.LessThan(0.01f));
        Time.timeScale = 0f;
        Vector3 position = graphic.transform.position;
        yield return new WaitForSecondsRealtime(0.06f);
        Assert.That(graphic.transform.position, Is.EqualTo(position));
    }

    [UnityTest]
    public IEnumerator BurstLoad_IsBounded_AndResetAllowsReuse()
    {
        Set(profile, "speed", 1f);
        for (int i = 0; i < 300; i++) presentation.PlayShot(Shot(true));
        Assert.That(presentation.ActiveEffectCount, Is.EqualTo(128));
        Assert.That(presentation.transform.childCount, Is.EqualTo(128));
        presentation.ResetEffects();
        presentation.PlayShot(Shot(false));
        Assert.That(presentation.ActiveEffectCount, Is.EqualTo(1));
        Assert.That(presentation.transform.childCount, Is.EqualTo(128));
        yield return null;
    }

    [UnityTest]
    public IEnumerator DurianArrival_EmitsFragmentsAndReleasesThem()
    {
        Set(profile, "fragmentCount", 12);
        presentation.PlayShot(Shot(true));
        yield return new WaitForSeconds(0.08f);
        Assert.That(presentation.ActiveEffectCount, Is.EqualTo(13));
        yield return new WaitForSeconds(0.35f);
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator CanvasScaling_KeepsMuzzleAndImpactAtScreenPositions()
    {
        foreach (float scale in new[] { 0.75f, 2f })
        {
            canvas.scaleFactor = scale;
            Canvas.ForceUpdateCanvases();
            ShotVisualContext shot = Shot(true);
            presentation.PlayShot(shot);
            Assert.That(presentation.GetComponentInChildren<WeaponEffectGraphic>().transform.parent.position, Is.EqualTo(muzzle.position));
            yield return new WaitForSeconds(0.08f);
            var impact = presentation.GetComponentInChildren<WeaponEffectGraphic>();
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, impact.transform.parent.position);
            Assert.That(Vector2.Distance(screen, shot.DestinationScreen), Is.LessThan(0.1f));
            presentation.ResetEffects();
        }
    }

    [UnityTest]
    public IEnumerator LockedProjectile_FollowsMovingTarget_AndIgnoresCursorChanges()
    {
        var shot = TrackingShot(out Camera camera, out BoxCollider2D target);
        Set(profile, "trailColor", Color.white);
        presentation.PlayShot(shot);
        yield return null;
        target.transform.position = new Vector3(1.5f, 1f, 0f);
        camera.transform.position += new Vector3(0.5f, 0.25f, 0f);
        Physics2D.SyncTransforms();
        presentation.SetAim(new Vector2(Screen.width * 0.1f, Screen.height * 0.1f));
        yield return new WaitForSeconds(0.55f);
        var impact = presentation.GetComponentInChildren<WeaponEffectGraphic>();
        Vector2 actual = RectTransformUtility.WorldToScreenPoint(null, impact.transform.parent.position);
        Vector2 expected = camera.WorldToScreenPoint(target.bounds.center);
        Assert.That(Vector2.Distance(actual, expected), Is.LessThan(0.1f));
        Assert.That(Vector2.Distance(actual, shot.DestinationScreen), Is.GreaterThan(1f));
        yield return new WaitForSeconds(0.35f);
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
    }

    [UnityTest]
    public IEnumerator LostTarget_FinishesAtLastPosition_AndDoesNotFollowRespawn()
    {
        var shot = TrackingShot(out Camera camera, out BoxCollider2D target);
        presentation.PlayShot(shot);
        target.transform.position = new Vector3(1f, 0f, 0f);
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.05f);
        Vector2 last = camera.WorldToScreenPoint(target.bounds.center);
        target.enabled = false;
        yield return new WaitForSeconds(0.05f);
        target.transform.position = new Vector3(4f, 3f, 0f);
        target.enabled = true;
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(0.5f);
        var impact = presentation.GetComponentInChildren<WeaponEffectGraphic>();
        Vector2 actual = RectTransformUtility.WorldToScreenPoint(null, impact.transform.parent.position);
        Assert.That(Vector2.Distance(actual, last), Is.LessThan(0.1f));
    }

    [UnityTest]
    public IEnumerator TargetDestroyedByInstantDamage_UsesCapturedCenter()
    {
        var shot = TrackingShot(out _, out BoxCollider2D target);
        Object.DestroyImmediate(target.gameObject);
        presentation.PlayShot(shot);
        yield return new WaitForSeconds(0.55f);
        var impact = presentation.GetComponentInChildren<WeaponEffectGraphic>();
        Vector2 actual = RectTransformUtility.WorldToScreenPoint(null, impact.transform.parent.position);
        Assert.That(Vector2.Distance(actual, shot.DestinationScreen), Is.LessThan(0.1f));
    }
}
