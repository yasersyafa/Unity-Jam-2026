using System.Reflection;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.TestTools;

/// <summary>Tests accepted shots independently of OS focus and physical input.</summary>
public sealed class WeaponShootingPresentationTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private GameObject root;
    private ShootingController shooting;
    private WeaponFeedback feedback;
    private WeaponData weapon;
    private WeaponShotVisualProfile profile;
    private InputActionAsset actions;
    private InputActionReference reference;
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Shooting presentation integration");
        root.SetActive(false);
        var cameraObject = new GameObject("Camera", typeof(Camera));
        cameraObject.transform.SetParent(root.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.GetComponent<Camera>();
        camera.orthographic = true;
        var hud = new GameObject("HUD", typeof(RectTransform), typeof(Canvas));
        hud.transform.SetParent(root.transform, false);
        var canvas = hud.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var crosshair = new GameObject("Crosshair", typeof(RectTransform), typeof(Image));
        crosshair.transform.SetParent(hud.transform, false);
        var image = new GameObject("Weapon", typeof(RectTransform), typeof(Image));
        image.transform.SetParent(hud.transform, false);
        feedback = root.AddComponent<WeaponFeedback>();
        feedback.Configure(canvas, (RectTransform)crosshair.transform, image.GetComponent<Image>(), camera.transform);
        profile = ScriptableObject.CreateInstance<WeaponShotVisualProfile>();
        Set(profile, "mode", WeaponShotVisualProfile.VisualMode.Stream);
        weapon = ScriptableObject.CreateInstance<WeaponData>();
        Set(weapon, "shotVisualProfile", profile);
        Set(weapon, "maxAmmo", 3);
        Set(weapon, "enableRecoil", false);
        actions = ScriptableObject.CreateInstance<InputActionAsset>();
        var map = new InputActionMap("Player");
        actions.AddActionMap(map);
        InputAction action = map.AddAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
        reference = InputActionReference.Create(action);
        shooting = root.AddComponent<ShootingController>();
        shooting.Configure(camera, weapon, feedback, reference);
        root.SetActive(true);
        Canvas.ForceUpdateCanvases();
        // Prevent physical input from driving Update; invoke only its accepted-shot boundary below.
        shooting.enabled = false;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(weapon);
        Object.DestroyImmediate(profile);
        Object.DestroyImmediate(reference);
        Object.DestroyImmediate(actions);
    }

    private void Shoot(bool held = true)
    {
        typeof(ShootingController).GetMethod("FireShot", Private).Invoke(shooting,
            new object[] { new Vector2(Screen.width * 0.5f, Screen.height * 0.5f), held });
    }

    [Test]
    public void FinalAmmo_StopsStreamInTheAcceptedShot_AndRaisesOneEventPerShot()
    {
        int shots = 0;
        shooting.ShotFired += () => shots++;
        Shoot();
        var effects = root.GetComponentInChildren<WeaponShotPresentation>();
        Assert.That(shooting.Ammo, Is.EqualTo(2));
        Assert.That(effects.IsStreaming, Is.True);
        Shoot();
        Shoot();
        Assert.That(shooting.Ammo, Is.Zero);
        Assert.That(shooting.HasAmmo, Is.False);
        Assert.That(effects.IsStreaming, Is.False);
        Assert.That(effects.ActiveEffectCount, Is.Zero);
        Assert.That(shots, Is.EqualTo(3));
    }

    [Test]
    public void ShootingPermissionReset_CancelsStream()
    {
        Shoot();
        var effects = root.GetComponentInChildren<WeaponShotPresentation>();
        Assert.That(effects.IsStreaming, Is.True);
        shooting.SetShootingAllowed(false);
        Assert.That(effects.ActiveEffectCount, Is.Zero);
    }

    [Test]
    public void EquippingLegacyWeapon_CancelsStreamAndRetainsSpriteFallback()
    {
        Shoot();
        var legacy = ScriptableObject.CreateInstance<WeaponData>();
        try
        {
            shooting.EquipWeapon(legacy);
            var effects = root.GetComponentInChildren<WeaponShotPresentation>();
            Assert.That(effects.ActiveEffectCount, Is.Zero);
            Assert.That(shooting.CurrentWeapon, Is.SameAs(legacy));
            Assert.That(root.GetComponentInChildren<WeaponVisual>(), Is.Null);
        }
        finally { Object.DestroyImmediate(legacy); }
    }

    [Test]
    public void TapWithoutHold_DoesNotLeaveAStream()
    {
        Shoot(false);
        Assert.That(root.GetComponentInChildren<WeaponShotPresentation>().ActiveEffectCount, Is.Zero);
        Assert.That(shooting.Ammo, Is.EqualTo(2));
    }

    [UnityTest]
    public IEnumerator AcceptedProjectileShot_DamagesLockedEnemyOnlyAfterArrival()
    {
        Set(profile, "mode", WeaponShotVisualProfile.VisualMode.Projectile);
        Set(profile, "speed", 500f);
        Set(weapon, "damage", 100f);
        var target = new GameObject("Enemy", typeof(ShootingDummy));
        target.transform.SetParent(root.transform, false);
        var hitbox = target.GetComponent<BoxCollider2D>();
        hitbox.size = Vector2.one * 2f;
        var camera = root.GetComponentInChildren<Camera>();
        Physics2D.SyncTransforms();
        Vector2 expected = camera.WorldToScreenPoint(hitbox.bounds.center);
        Vector2 cursor = camera.WorldToScreenPoint(new Vector3(0.6f, 0.3f, 0f));
        typeof(ShootingController).GetMethod("FireShot", Private).Invoke(shooting, new object[] { cursor, true });
        Assert.That(hitbox.enabled, Is.True, "The shot only locks the target at launch.");
        Assert.That(shooting.Ammo, Is.EqualTo(2), "Ammo is spent at launch.");
        yield return new WaitForSeconds(0.05f);
        Assert.That(hitbox.enabled, Is.True);
        yield return new WaitForSeconds(0.22f);
        Assert.That(hitbox.enabled, Is.False, "Lethal damage is delivered at arrival.");
        var effects = root.GetComponentInChildren<WeaponShotPresentation>();
        var impact = effects.GetComponentInChildren<WeaponEffectGraphic>();
        Vector2 actual = RectTransformUtility.WorldToScreenPoint(null, impact.transform.parent.position);
        Assert.That(Vector2.Distance(actual, expected), Is.LessThan(0.1f));
        Assert.That(Vector2.Distance(actual, cursor), Is.GreaterThan(1f));
    }

    [UnityTest]
    public IEnumerator WeaponSwapAndInputReset_KeepAlreadyLaunchedProjectiles()
    {
        Set(profile, "mode", WeaponShotVisualProfile.VisualMode.Projectile);
        Set(profile, "speed", 500f);
        Set(weapon, "damage", 100f);
        var target = new GameObject("Enemy", typeof(ShootingDummy));
        target.transform.SetParent(root.transform, false);
        var hitbox = target.GetComponent<BoxCollider2D>();
        hitbox.size = Vector2.one * 2f;
        Physics2D.SyncTransforms();
        var camera = root.GetComponentInChildren<Camera>();
        Vector2 aim = camera.WorldToScreenPoint(hitbox.bounds.center);
        var visual = ShotVisualContext.Capture(new ShotContext(camera, aim, 100f, ~0), trackTarget: true);
        float travel = feedback.GetProjectileTravelDuration(visual);
        typeof(ShootingController).GetMethod("FireShot", Private).Invoke(shooting, new object[] { aim, true });
        var legacy = ScriptableObject.CreateInstance<WeaponData>();
        try
        {
            shooting.EquipWeapon(legacy);
            shooting.SetShootingAllowed(false);
            var effects = root.GetComponentInChildren<WeaponShotPresentation>();
            Assert.That(effects.ActiveEffectCount, Is.EqualTo(1));
            Assert.That(hitbox.enabled, Is.True);
            yield return new WaitForSeconds(travel + 0.05f);
            Assert.That(hitbox.enabled, Is.False, "The original weapon's damage survives equipping another weapon.");
            Assert.That(root.GetComponent<ProjectileShotRunner>().ActiveShotCount, Is.Zero);
        }
        finally { Object.DestroyImmediate(legacy); }
    }
}
