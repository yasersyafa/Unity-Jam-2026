using System.Collections;
using System.Reflection;
using NUnit.Framework;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class WeaponStickerTests
{
    private GameObject root;
    private WeaponStickerPresentation sticker;
    private WeaponData weapon;
    private CombatHudAnimationProfile profile;
    private CanvasGroup group;
    private RectTransform visual;
    private Image icon;
    private TextMeshProUGUI label;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Set(object target, string field, object value) => target.GetType().GetField(field, Private).SetValue(target, value);
    private static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, Private).GetValue(target);

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("Sticker test", typeof(RectTransform), typeof(CanvasGroup));
        root.SetActive(false);
        visual = (RectTransform)root.transform;
        visual.anchoredPosition = new Vector2(40f, 20f);
        visual.localRotation = Quaternion.Euler(0f, 0f, -10f);
        icon = new GameObject("Sprite", typeof(RectTransform)).AddComponent<Image>();
        icon.transform.SetParent(root.transform, false);
        label = new GameObject("Text", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(root.transform, false);
        profile = ScriptableObject.CreateInstance<CombatHudAnimationProfile>();
        weapon = ScriptableObject.CreateInstance<WeaponData>();
        Set(weapon, "stickerText", "BANG!");
        sticker = root.AddComponent<WeaponStickerPresentation>();
        Set(sticker, "profile", profile);
        Set(sticker, "icon", icon);
        Set(sticker, "label", label);
        group = root.GetComponent<CanvasGroup>();
        root.SetActive(true);
        sticker.SetWeapon(weapon);
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        Object.DestroyImmediate(root);
        Object.DestroyImmediate(weapon);
        Object.DestroyImmediate(profile);
    }

    [UnityTest]
    public IEnumerator Tap_PopsAgainAndThenHides()
    {
        sticker.PlayShot();
        yield return new WaitForSecondsRealtime(0.14f);
        Assert.That(group.alpha, Is.EqualTo(1f).Within(0.01f));
        sticker.PlayShot();
        Assert.That(visual.localScale.x, Is.EqualTo(0.55f).Within(0.01f));
        yield return new WaitForSecondsRealtime(0.45f);
        Assert.That(group.alpha, Is.Zero);
        Assert.That(visual.localScale, Is.EqualTo(Vector3.one));
    }

    [UnityTest]
    public IEnumerator Hold_KeepsOneAnimationUntilReleaseAndSupportsAnotherHold()
    {
        sticker.SetContinuousFire(true);
        Assert.That(group.alpha, Is.Zero, "Holding without an actual shot must stay hidden.");
        sticker.PlayShot();
        var firstShake = Read<Tween>(sticker, "shake");
        int children = root.transform.childCount;
        yield return new WaitForSecondsRealtime(0.3f);
        for (int i = 0; i < 30; i++) sticker.PlayShot();
        Assert.That(Read<Tween>(sticker, "shake"), Is.EqualTo(firstShake));
        Assert.That(root.transform.childCount, Is.EqualTo(children));
        Assert.That(group.alpha, Is.EqualTo(1f));
        Assert.That(visual.localScale.x, Is.EqualTo(1f).Within(0.01f));
        sticker.SetContinuousFire(false);
        Assert.That(firstShake.isAlive, Is.False);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(group.alpha, Is.Zero);
        sticker.SetContinuousFire(true);
        sticker.PlayShot();
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That(group.alpha, Is.EqualTo(1f));
    }

    [UnityTest]
    public IEnumerator EarlyRelease_WaitsForPopAndMinimumDisplay()
    {
        sticker.SetContinuousFire(true);
        sticker.PlayShot();
        sticker.SetContinuousFire(false);
        yield return new WaitForSecondsRealtime(0.14f);
        Assert.That(group.alpha, Is.EqualTo(1f).Within(0.01f));
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That(group.alpha, Is.Zero);
    }

    [UnityTest]
    public IEnumerator SemiAutomatic_DoesNotStayVisibleWhenHeld()
    {
        Set(weapon, "firingMode", WeaponData.FireMode.SemiAutomatic);
        sticker.SetWeapon(weapon);
        sticker.SetContinuousFire(true);
        sticker.PlayShot();
        yield return new WaitForSecondsRealtime(0.45f);
        Assert.That(group.alpha, Is.Zero);
    }

    [UnityTest]
    public IEnumerator DisableAndDestroy_StopHandlesAndPreventLateCallbacks()
    {
        sticker.SetContinuousFire(true);
        sticker.PlayShot();
        var pop = Read<Sequence>(sticker, "animation");
        var shake = Read<Tween>(sticker, "shake");
        root.SetActive(false);
        Assert.That(pop.isAlive || shake.isAlive, Is.False);
        root.SetActive(true);
        yield return new WaitForSecondsRealtime(0.4f);
        Assert.That(group.alpha, Is.Zero);
        Assert.That(visual.anchoredPosition, Is.EqualTo(new Vector2(40f, 20f)));
        sticker.SetContinuousFire(true);
        sticker.PlayShot();
        pop = Read<Sequence>(sticker, "animation");
        shake = Read<Tween>(sticker, "shake");
        Object.Destroy(root);
        yield return null;
        Assert.That(pop.isAlive || shake.isAlive, Is.False);
    }

    [UnityTest]
    public IEnumerator WeaponSwitchAndCancel_StopHeldAndExitAnimations()
    {
        sticker.SetContinuousFire(true);
        sticker.PlayShot();
        sticker.SetWeapon(null);
        Assert.That(group.alpha, Is.Zero);
        sticker.PlayShot();
        yield return new WaitForSecondsRealtime(0.15f);
        Assert.That(group.alpha, Is.Zero);
        sticker.SetWeapon(weapon);
        sticker.PlayShot();
        yield return new WaitForSecondsRealtime(0.25f);
        var exit = Read<Sequence>(sticker, "animation");
        sticker.Cancel();
        Assert.That(exit.isAlive, Is.False);
        yield return new WaitForSecondsRealtime(0.2f);
        Assert.That(group.alpha, Is.Zero);
    }

    [UnityTest]
    public IEnumerator SpriteOnlyAndCombinedContent_RenderWithoutInventedText()
    {
        var texture = new Texture2D(2, 2);
        var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), Vector2.one * 0.5f);
        try
        {
            Set(weapon, "stickerSprite", sprite);
            Set(weapon, "stickerText", string.Empty);
            sticker.SetWeapon(weapon);
            sticker.PlayShot();
            yield return new WaitForSecondsRealtime(0.14f);
            Assert.That(group.alpha, Is.EqualTo(1f).Within(0.01f));
            Assert.That(icon.enabled, Is.True);
            Assert.That(icon.sprite, Is.SameAs(sprite));
            Assert.That(label.enabled, Is.False);
            Set(weapon, "stickerText", "POW!");
            sticker.SetWeapon(weapon);
            sticker.PlayShot();
            yield return new WaitForSecondsRealtime(0.14f);
            Assert.That(icon.enabled && label.enabled, Is.True);
            Assert.That(label.text, Is.EqualTo("POW!"));
            sticker.Cancel();
        }
        finally
        {
            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
    }

    [UnityTest]
    public IEnumerator Pause_UsesUnscaledTimeAndEmptyContentStaysHidden()
    {
        Time.timeScale = 0f;
        sticker.PlayShot();
        yield return new WaitForSecondsRealtime(0.14f);
        Assert.That(group.alpha, Is.EqualTo(1f).Within(0.01f));
        Assert.That(icon.enabled, Is.False);
        Assert.That(label.text, Is.EqualTo("BANG!"));
        yield return new WaitForSecondsRealtime(0.3f);
        Assert.That(group.alpha, Is.Zero);
        Set(weapon, "stickerText", string.Empty);
        sticker.SetWeapon(weapon);
        sticker.PlayShot();
        Assert.That(group.alpha, Is.Zero);
        Assert.That(Read<Sequence>(sticker, "animation").isAlive, Is.False);
    }
}
