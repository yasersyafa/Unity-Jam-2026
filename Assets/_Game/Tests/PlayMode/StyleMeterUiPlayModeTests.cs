using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class StyleMeterUiPlayModeTests
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    private GameObject root;
    private ShootingController shooting;
    private PlayerHealth player;
    private StyleMeter style;
    private StyleMeterUI styleUi;
    private CanvasGroup group;
    private RectTransform rankVisual;
    private Image rankImage;
    private TextMeshProUGUI taglineLabel;
    private TextMeshProUGUI hitCountLabel;
    private readonly List<Object> createdAssets = new List<Object>();
    private Sprite[] rankSprites;
    private float previousTimeScale;
    private bool previousRunInBackground;

    private static void Set(object target, string field, object value)
    {
        var info = target.GetType().GetField(field, Private);
        Assert.That(info, Is.Not.Null, $"Field not found: {field} on {target.GetType().Name}");
        info.SetValue(target, value);
    }

    private void SimulateShot(bool hit)
    {
        MethodInfo method = typeof(ShootingController).GetMethod("ResolveShot", Private);
        Assert.That(method, Is.Not.Null);
        method.Invoke(shooting, new object[] { hit });
    }

    private Material CreateRankShaderMaterial()
    {
        Shader shader = Shader.Find("Game/UI Combo Rank");
        Assert.That(shader, Is.Not.Null, "Shader 'Game/UI Combo Rank' must exist.");
        var mat = new Material(shader);
        createdAssets.Add(mat);
        return mat;
    }

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        previousTimeScale = Time.timeScale;
        previousRunInBackground = Application.runInBackground;
        Time.timeScale = 1f;
        Application.runInBackground = true;

        root = new GameObject("StyleMeterUiTestRoot");
        root.SetActive(false);

        var shootingHolder = new GameObject("InactiveShootingSource");
        shootingHolder.transform.SetParent(root.transform, false);
        shootingHolder.SetActive(false);
        shooting = shootingHolder.AddComponent<ShootingController>();
        player = root.AddComponent<PlayerHealth>();
        style = root.AddComponent<StyleMeter>();
        Set(style, "shooting", shooting);
        Set(style, "player", player);
        Set(style, "decayDelay", 1.5f);
        Set(style, "ranks", new[]
        {
            new StyleMeter.Rank("C", "COME ON!", 1f, 0.3f),
            new StyleMeter.Rank("B", "BRO WHAT?", 4f, 0.5f),
            new StyleMeter.Rank("A", "ALL U GOT?", 5f, 0.8f),
            new StyleMeter.Rank("S", "SMALL COMBO!", 10f, 1.2f),
            new StyleMeter.Rank("SS", "SOME SHOOTING!!", 15f, 1.8f),
            new StyleMeter.Rank("SSS", "SURE SO SATISFYING!!!", 15f, 2.5f),
        });

        var canvasObject = new GameObject("CanvasHUD", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(root.transform, false);

        var hudObject = new GameObject("StyleRankHUD", typeof(RectTransform), typeof(CanvasGroup));
        hudObject.transform.SetParent(canvasObject.transform, false);
        var hudRect = hudObject.GetComponent<RectTransform>();
        group = hudObject.GetComponent<CanvasGroup>();

        rankVisual = new GameObject("RankVisual", typeof(RectTransform)).GetComponent<RectTransform>();
        rankVisual.SetParent(hudRect, false);

        var imageObject = new GameObject("RankImage", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(rankVisual, false);
        rankImage = imageObject.GetComponent<Image>();

        taglineLabel = new GameObject("RankTagline", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        taglineLabel.transform.SetParent(hudRect, false);
        hitCountLabel = new GameObject("HitCountText", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        hitCountLabel.transform.SetParent(hudRect, false);

        rankSprites = new Sprite[6];
        var visuals = new StyleMeterUI.RankVisual[6];
        for (int i = 0; i < 6; i++)
        {
            var tex = new Texture2D(16, 16);
            tex.name = $"RankTex_{i}";
            createdAssets.Add(tex);
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f));
            sprite.name = $"RankSprite_{i}";
            createdAssets.Add(sprite);
            rankSprites[i] = sprite;
            visuals[i] = new StyleMeterUI.RankVisual
            {
                sprite = sprite,
                accentColor = Color.white,
                uvBottom = 0.24f,
                uvTop = 0.78f,
            };
        }

        styleUi = hudObject.AddComponent<StyleMeterUI>();
        styleUi.Configure(
            style,
            group,
            hudRect,
            rankVisual,
            rankImage,
            CreateRankShaderMaterial(),
            taglineLabel,
            hitCountLabel,
            visuals);

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
    public IEnumerator Hits_ShowRankAndAdvanceBottomToTopShaderFillThenPromote()
    {
        Assert.That(style.HasRank, Is.False);
        Assert.That(styleUi.IsVisible, Is.False);
        Assert.That(group.alpha, Is.EqualTo(0f).Within(0.001f));

        // 1st hit enters Rank C (0/4 gauge).
        SimulateShot(true);
        yield return null;

        Assert.That(style.RankIndex, Is.EqualTo(0));
        Assert.That(styleUi.IsVisible, Is.True);
        Assert.That(styleUi.CurrentRankIndex, Is.EqualTo(0));
        Assert.That(rankImage.sprite, Is.SameAs(rankSprites[0]));
        Assert.That(taglineLabel.gameObject.activeSelf, Is.False);
        Assert.That(hitCountLabel.text, Is.EqualTo("1 HITS"));

        // 2nd hit fills Rank C gauge to 0.25 (1 / 4).
        SimulateShot(true);
        float timeout = Time.realtimeSinceStartup + 1f;
        while (rankImage.material.GetFloat(FillAmountId) < 0.23f && Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(style.Gauge01, Is.EqualTo(0.25f).Within(0.001f));
        Assert.That(rankImage.material.GetFloat(FillAmountId), Is.EqualTo(0.25f).Within(0.03f));

        // 3 more hits promote C -> B (5 total hits, 0/5 gauge at B).
        SimulateShot(true);
        SimulateShot(true);
        SimulateShot(true);
        yield return null;

        Assert.That(style.RankIndex, Is.EqualTo(1));
        Assert.That(rankImage.sprite, Is.SameAs(rankSprites[1]));
        Assert.That(taglineLabel.gameObject.activeSelf, Is.False);
        Assert.That(hitCountLabel.text, Is.EqualTo("5 HITS"));
        Assert.That(rankImage.material.GetFloat(FlashAmountId), Is.GreaterThan(0.4f));
    }

    [UnityTest]
    public IEnumerator Miss_ResetsHitCountWhileKeepingRankDowngradeBehavior()
    {
        // Reach Rank B with 6 hits (Rank B, gauge 1/5 = 0.2).
        for (int i = 0; i < 6; i++) SimulateShot(true);
        yield return null;

        Assert.That(style.RankIndex, Is.EqualTo(1));
        Assert.That(style.HitCount, Is.EqualTo(6));
        Assert.That(hitCountLabel.text, Is.EqualTo("6 HITS"));

        // Miss drains gauge (0.35 * 5 = 1.75 -> drops B to C with remainder gauge) and resets hits to 0.
        SimulateShot(false);
        yield return null;

        Assert.That(style.HitCount, Is.EqualTo(0));
        Assert.That(hitCountLabel.text, Is.EqualTo("0 HITS"));
        Assert.That(style.RankIndex, Is.EqualTo(0));
    }

    [UnityTest]
    public IEnumerator ActiveRank_PulsesWhiteBlinkAndTwitchesContinuously()
    {
        // Promote straight to A so blink + twitch are energetic.
        for (int i = 0; i < 10; i++) SimulateShot(true);
        yield return null;

        // Wait briefly for initial hit burst to decay into steady blink/twitch cycle.
        yield return new WaitForSecondsRealtime(0.28f);

        float minFlash = 1f;
        float maxFlash = 0f;
        float minScale = float.MaxValue;
        float maxScale = float.MinValue;
        float maxPosOffset = 0f;

        float end = Time.realtimeSinceStartup + 0.55f;
        while (Time.realtimeSinceStartup < end)
        {
            float flash = rankImage.material.GetFloat(FlashAmountId);
            if (flash < minFlash) minFlash = flash;
            if (flash > maxFlash) maxFlash = flash;

            float s = rankVisual.localScale.x;
            if (s < minScale) minScale = s;
            if (s > maxScale) maxScale = s;

            float pos = rankVisual.anchoredPosition.magnitude;
            if (pos > maxPosOffset) maxPosOffset = pos;

            yield return null;
        }

        Assert.That(maxFlash, Is.GreaterThan(0.25f), "Letter should blink white cleanly.");
        Assert.That(maxFlash - minFlash, Is.GreaterThan(0.15f), "White blink should oscillate rather than stay constant.");
        Assert.That(maxScale - minScale, Is.GreaterThan(0.015f), "Letter should pulse/twitch scale.");
        Assert.That(maxPosOffset, Is.GreaterThan(0.2f), "Letter should apply micro-twitch offset.");
    }

    [UnityTest]
    public IEnumerator DecayToZero_HidesComboHudAgain()
    {
        Set(style, "decayDelay", 0.04f);
        Set(style, "ranks", new[]
        {
            new StyleMeter.Rank("C", "COME ON!", 1f, 30f),
            new StyleMeter.Rank("B", "BRO WHAT?", 1f, 30f),
        });

        SimulateShot(true);
        yield return null;
        Assert.That(styleUi.IsVisible, Is.True);

        float timeout = Time.realtimeSinceStartup + 1.5f;
        while ((style.HasRank || group.alpha > 0.001f) && Time.realtimeSinceStartup < timeout)
            yield return null;

        Assert.That(style.HasRank, Is.False);
        Assert.That(group.alpha, Is.EqualTo(0f).Within(0.001f));
        Assert.That(rankImage.material.GetFloat(FillAmountId), Is.EqualTo(0f).Within(0.001f));
    }
}
