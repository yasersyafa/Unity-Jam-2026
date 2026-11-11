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
    public IEnumerator ResetDisableAndReplacement_CancelAllEffects()
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
        Assert.That(presentation.ActiveEffectCount, Is.Zero);
        presentation.PlayShot(Shot(true));
        yield return null;
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
}
