using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class SlotCooldownIconPlayModeTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    private GameObject root;
    private SlotGameplayFlow flow;
    private PlayerHealth player;
    private RectTransform iconRect;
    private Image iconImage;
    private SlotCooldownIconUI iconUi;
    private readonly List<Object> createdAssets = new List<Object>();
    private float previousTimeScale;
    private bool previousRunInBackground;

    private static void Set(object target, string field, object value)
    {
        var info = target.GetType().GetField(field, Private);
        Assert.That(info, Is.Not.Null, $"Field not found: {field} on {target.GetType().Name}");
        info.SetValue(target, value);
    }

    private Material CreateFillMaterial()
    {
        Shader shader = Shader.Find("Game/UI Combo Rank");
        Assert.That(shader, Is.Not.Null, "Shader 'Game/UI Combo Rank' must exist.");
        var material = new Material(shader);
        createdAssets.Add(material);
        return material;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousTimeScale = Time.timeScale;
        previousRunInBackground = Application.runInBackground;
        Time.timeScale = 1f;
        Application.runInBackground = true;

        root = new GameObject("SlotCooldownIconTestRoot");
        root.SetActive(false);

        var inactiveFlowHost = new GameObject("FlowHost");
        inactiveFlowHost.transform.SetParent(root.transform, false);
        inactiveFlowHost.SetActive(false);
        flow = inactiveFlowHost.AddComponent<SlotGameplayFlow>();
        Set(flow, "slotCooldown", 8f);
        Set(flow, "remaining", 8f);
        flow.ManualOpenAllowed = true;

        player = root.AddComponent<PlayerHealth>();

        var canvasObject = new GameObject("CanvasHUD", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(root.transform, false);

        var iconObject = new GameObject("SlotDockTarget", typeof(RectTransform), typeof(Image));
        iconObject.transform.SetParent(canvasObject.transform, false);
        iconRect = iconObject.GetComponent<RectTransform>();
        iconImage = iconObject.GetComponent<Image>();

        var tex = new Texture2D(16, 16);
        createdAssets.Add(tex);
        var sprite = Sprite.Create(tex, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f));
        createdAssets.Add(sprite);
        iconImage.sprite = sprite;

        iconUi = iconObject.AddComponent<SlotCooldownIconUI>();
        iconUi.Configure(flow, player, iconRect, iconImage, CreateFillMaterial());

        root.SetActive(true);
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        Time.timeScale = previousTimeScale;
        Application.runInBackground = previousRunInBackground;
        if (root != null) Object.Destroy(root);
        foreach (Object asset in createdAssets)
        {
            if (asset != null) Object.Destroy(asset);
        }
        createdAssets.Clear();
        yield return null;
    }

    [UnityTest]
    public IEnumerator CooldownProgress_FillsBottomToTopWithoutReadyRockOrBlink()
    {
        Set(flow, "remaining", 6f); // 2s elapsed of 8s -> 0.25 fill
        yield return null;

        Assert.That(iconImage.enabled, Is.True);
        Assert.That(iconUi.IsReady, Is.False);
        Assert.That(iconImage.material.GetFloat(FillAmountId), Is.EqualTo(0.25f).Within(0.001f));
        Assert.That(iconImage.material.GetFloat(FlashAmountId), Is.EqualTo(0f).Within(0.001f));
        Assert.That(Quaternion.Angle(iconRect.localRotation, Quaternion.identity), Is.LessThan(0.01f));

        Set(flow, "remaining", 2f); // 6s elapsed of 8s -> 0.75 fill
        yield return null;

        Assert.That(iconUi.IsReady, Is.False);
        Assert.That(iconImage.material.GetFloat(FillAmountId), Is.EqualTo(0.75f).Within(0.001f));
        Assert.That(iconImage.material.GetFloat(FlashAmountId), Is.EqualTo(0f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator CooldownFinished_RocksLeftRightAndBlinksWhite()
    {
        Set(flow, "remaining", 2f);
        yield return null;
        Assert.That(iconUi.IsReady, Is.False);

        Set(flow, "remaining", 0f);
        yield return null;

        Assert.That(iconUi.IsReady, Is.True);
        Assert.That(iconImage.material.GetFloat(FillAmountId), Is.EqualTo(1f).Within(0.001f));

        float minAngle = float.MaxValue;
        float maxAngle = float.MinValue;
        float minFlash = float.MaxValue;
        float maxFlash = float.MinValue;

        float end = Time.realtimeSinceStartup + 0.65f;
        while (Time.realtimeSinceStartup < end)
        {
            float z = iconRect.localEulerAngles.z;
            if (z > 180f) z -= 360f;
            if (z < minAngle) minAngle = z;
            if (z > maxAngle) maxAngle = z;

            float flash = iconImage.material.GetFloat(FlashAmountId);
            if (flash < minFlash) minFlash = flash;
            if (flash > maxFlash) maxFlash = flash;

            yield return null;
        }

        Assert.That(minAngle, Is.LessThan(-3f), "Ready icon should rotate right (negative Z).");
        Assert.That(maxAngle, Is.GreaterThan(3f), "Ready icon should rotate left (positive Z).");
        Assert.That(maxFlash, Is.GreaterThan(0.3f), "Ready icon should flash white.");
        Assert.That(maxFlash - minFlash, Is.GreaterThan(0.2f), "Ready icon white blink should oscillate.");
    }

    [UnityTest]
    public IEnumerator OpenSlot_HidesIconAndResetsMotion()
    {
        Set(flow, "remaining", 0f);
        yield return null;
        yield return new WaitForSecondsRealtime(0.1f);
        Assert.That(iconImage.enabled, Is.True);

        Set(flow, "open", true);
        yield return null;

        Assert.That(iconImage.enabled, Is.False);
        Assert.That(iconUi.IsReady, Is.False);
        Assert.That(Quaternion.Angle(iconRect.localRotation, Quaternion.identity), Is.LessThan(0.01f));
    }
}
