using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DMC-style combo rank HUD anchored on the right side of the screen.
/// Renders the current rank sprite filling bottom-to-top with StyleMeter.Gauge01,
/// pulses white blinks, and applies continuous micro-twitches and hit punches.
/// </summary>
[DisallowMultipleComponent]
public sealed class StyleMeterUI : MonoBehaviour
{
    [Serializable]
    public struct RankVisual
    {
        public Sprite sprite;
        public Color accentColor;
        [Range(0f, 1f)] public float uvBottom;
        [Range(0f, 1f)] public float uvTop;
    }

    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int FillBoundsId = Shader.PropertyToID("_FillBounds");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int EdgeGlowColorId = Shader.PropertyToID("_EdgeGlowColor");

    [Header("Data Sources")]
    [SerializeField] private StyleMeter style;
    [SerializeField] private SlotGameplayFlow flow;
    [SerializeField] private PlayerHealth player;

    [Header("UI References")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private RectTransform panelRoot;
    [SerializeField] private RectTransform rankVisual;
    [SerializeField] private Image rankImage;
    [SerializeField] private Material rankMaterial;
    [SerializeField] private bool showTagline;
    [SerializeField] private TextMeshProUGUI taglineLabel;
    [SerializeField] private TextMeshProUGUI hitCountLabel;

    [Header("Rank Sprites (Lowest to Highest)")]
    [SerializeField] private RankVisual[] rankVisuals = Array.Empty<RankVisual>();

    [Header("Fill Shader")]
    [SerializeField, Range(0.001f, 0.1f)] private float fillSoftness = 0.015f;
    [SerializeField, Range(0.005f, 0.12f)] private float fillEdgeWidth = 0.04f;
    [SerializeField, Min(0.5f)] private float fillFollowSpeed = 5.5f;

    [Header("White Blink")]
    [SerializeField, Min(0.1f)] private float blinkFrequency = 2.4f;
    [SerializeField, Min(0f)] private float rankBlinkSpeedBoost = 1.6f;
    [SerializeField, Min(1f)] private float blinkSharpness = 3.2f;
    [SerializeField, Range(0f, 1f)] private float minIdleFlash = 0.35f;
    [SerializeField, Range(0f, 1f)] private float maxIdleFlash = 0.65f;
    [SerializeField, Range(0f, 1f)] private float hitFlash = 0.72f;
    [SerializeField, Range(0f, 1f)] private float rankUpFlash = 0.95f;
    [SerializeField, Min(0.5f)] private float flashDecaySpeed = 4.5f;

    [Header("Twitch & Punch")]
    [SerializeField, Min(0f)] private float idleScalePulse = 0.055f;
    [SerializeField, Min(0f)] private float hitScalePunch = 0.18f;
    [SerializeField, Min(0f)] private float rankUpScalePunch = 0.38f;
    [SerializeField, Min(0.5f)] private float punchDecaySpeed = 2.8f;
    [SerializeField, Min(0f)] private float idleJitterPixels = 2.5f;
    [SerializeField, Min(0f)] private float hitJitterPixels = 7.5f;
    [SerializeField, Min(0f)] private float idleTiltDegrees = 2f;
    [SerializeField, Min(0f)] private float hitTiltDegrees = 5.5f;
    [SerializeField, Min(0.5f)] private float twitchDecaySpeed = 4.2f;
    [SerializeField, Min(0f)] private float rankSlideOffset = 46f;
    [SerializeField, Min(1f)] private float slideReturnSpeed = 220f;
    [SerializeField, Min(1f)] private float fadeSpeed = 10f;

    private Material materialInstance;
    private Vector2 basePanelPosition;
    private Vector2 baseRankPosition;
    private Vector3 baseRankScale = Vector3.one;
    private bool transformsCaptured;
    private bool initialized;

    private int lastRankIndex = -1;
    private int lastHitCount = -1;
    private int displayedHitCount = -1;
    private string lastTagline;
    private bool wasVisible;

    private float displayedFill;
    private float flashBurst;
    private float punchScale;
    private float twitchEnergy;
    private float blinkTimer;
    private float twitchTimer;
    private Vector2 slideOffset;
    private float visibleAlpha;

    public float DisplayedFill => displayedFill;
    public float FlashAmount { get; private set; }
    public int CurrentRankIndex => lastRankIndex;
    public bool IsVisible => visibleAlpha > 0.01f;

    public void Configure(
        StyleMeter styleSource,
        CanvasGroup canvasGroup,
        RectTransform rootRect,
        RectTransform rankRect,
        Image image,
        Material shaderMaterial,
        TextMeshProUGUI tagline,
        TextMeshProUGUI hits,
        RankVisual[] visuals)
    {
        Unsubscribe();
        style = styleSource;
        group = canvasGroup;
        panelRoot = rootRect;
        rankVisual = rankRect;
        rankImage = image;
        rankMaterial = shaderMaterial;
        taglineLabel = tagline;
        hitCountLabel = hits;
        rankVisuals = visuals ?? Array.Empty<RankVisual>();

        transformsCaptured = false;
        CaptureBaseTransforms();
        RecreateMaterial();
        ResetVisualState();
        if (isActiveAndEnabled) Subscribe();
        SyncFromStyle(instant: true);
    }

    private void Awake()
    {
        if (style == null) style = GetComponentInParent<StyleMeter>() ?? FindAnyObjectByType<StyleMeter>();
        if (flow == null) flow = FindAnyObjectByType<SlotGameplayFlow>();
        if (player == null) player = FindAnyObjectByType<PlayerHealth>();
        if (group == null) group = GetComponent<CanvasGroup>();
        if (panelRoot == null) panelRoot = transform as RectTransform;

        CaptureBaseTransforms();
        EnsureMaterial();
        ResetVisualState();
    }

    private void OnEnable()
    {
        if (style == null) style = GetComponentInParent<StyleMeter>() ?? FindAnyObjectByType<StyleMeter>();
        if (flow == null) flow = FindAnyObjectByType<SlotGameplayFlow>();
        if (player == null) player = FindAnyObjectByType<PlayerHealth>();

        CaptureBaseTransforms();
        EnsureMaterial();
        Subscribe();
        SyncFromStyle(instant: true);
    }

    private void OnDisable()
    {
        Unsubscribe();
        ResetVisualState();
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
            materialInstance = null;
        }
    }

    private void Subscribe()
    {
        if (style != null) style.Changed += OnStyleChanged;
    }

    private void Unsubscribe()
    {
        if (style != null) style.Changed -= OnStyleChanged;
    }

    private void CaptureBaseTransforms()
    {
        if (transformsCaptured) return;
        if (panelRoot != null) basePanelPosition = panelRoot.anchoredPosition;
        if (rankVisual != null)
        {
            baseRankPosition = rankVisual.anchoredPosition;
            baseRankScale = rankVisual.localScale;
        }
        transformsCaptured = true;
    }

    private void RecreateMaterial()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
            materialInstance = null;
        }
        EnsureMaterial();
    }

    private void EnsureMaterial()
    {
        if (rankImage == null || materialInstance != null) return;
        Shader shader = rankMaterial != null ? rankMaterial.shader : Shader.Find("Game/UI Combo Rank");
        if (shader == null) return;

        materialInstance = rankMaterial != null
            ? new Material(rankMaterial)
            : new Material(shader);
        materialInstance.name = $"ComboRankFill ({name})";
        materialInstance.hideFlags = HideFlags.DontSave;
        rankImage.material = materialInstance;
    }

    private void OnStyleChanged() => SyncFromStyle(instant: false);

    private void Update()
    {
        if (style == null) style = FindAnyObjectByType<StyleMeter>();
        bool shouldShow = ShouldShow();
        float dt = Time.unscaledDeltaTime;

        float targetAlpha = shouldShow ? 1f : 0f;
        visibleAlpha = Mathf.MoveTowards(visibleAlpha, targetAlpha, dt * fadeSpeed);
        ApplyCanvasGroup(visibleAlpha);

        if (!shouldShow && visibleAlpha <= 0.001f)
        {
            if (wasVisible)
            {
                wasVisible = false;
                displayedFill = 0f;
                FlashAmount = 0f;
                ApplyShaderProperties(0f, 0f);
            }
            return;
        }

        wasVisible = true;
        float targetFill = style != null && style.HasRank ? style.Gauge01 : 0f;
        displayedFill = Mathf.MoveTowards(displayedFill, targetFill, dt * fillFollowSpeed);

        float rankT = RankRatio(lastRankIndex);
        blinkTimer += dt * (blinkFrequency + rankT * rankBlinkSpeedBoost);
        twitchTimer += dt;

        float rawWave = 0.5f + 0.5f * Mathf.Sin(blinkTimer * Mathf.PI * 2f);
        float blinkPulse = Mathf.Pow(rawWave, blinkSharpness) * Mathf.Lerp(minIdleFlash, maxIdleFlash, rankT);
        flashBurst = Mathf.MoveTowards(flashBurst, 0f, dt * flashDecaySpeed);
        FlashAmount = Mathf.Clamp01(Mathf.Max(flashBurst, blinkPulse));

        ApplyShaderProperties(displayedFill, FlashAmount);
        ApplyMotion(dt, rawWave, rankT);
    }

    private bool ShouldShow()
    {
        if (style == null || !style.HasRank) return false;
        if (player != null && player.IsDead) return false;
        if (flow != null && flow.IsOpen) return false;
        return true;
    }

    private void SyncFromStyle(bool instant)
    {
        if (style == null || !style.HasRank)
        {
            lastRankIndex = -1;
            lastHitCount = 0;
            if (instant) ResetVisualState();
            return;
        }

        int rank = style.RankIndex;
        int hits = style.HitCount;
        bool promoted = rank > lastRankIndex;
        bool demoted = initialized && rank < lastRankIndex;
        bool hitLanded = initialized && hits > lastHitCount;

        if (rank != lastRankIndex || !initialized)
        {
            ApplyRankLook(rank);
            if (promoted && !instant)
            {
                displayedFill = 0f;
                flashBurst = rankUpFlash;
                punchScale = rankUpScalePunch;
                twitchEnergy = 1f;
                slideOffset = new Vector2(rankSlideOffset, 0f);
            }
            else if (demoted && !instant)
            {
                displayedFill = style.Gauge01;
                slideOffset = new Vector2(0f, -12f);
                twitchEnergy = 0.5f;
            }
            else
            {
                displayedFill = style.Gauge01;
            }
        }
        else if (hitLanded && !instant)
        {
            flashBurst = Mathf.Max(flashBurst, hitFlash);
            punchScale = Mathf.Max(punchScale, hitScalePunch);
            twitchEnergy = 1f;
        }

        UpdateLabels(style.CurrentRank.text, hits);

        lastRankIndex = rank;
        lastHitCount = hits;
        initialized = true;

        if (instant && ShouldShow())
        {
            visibleAlpha = 1f;
            wasVisible = true;
            displayedFill = style.Gauge01;
            ApplyCanvasGroup(1f);
            ApplyShaderProperties(displayedFill, 0f);
        }
    }

    private void ApplyRankLook(int rank)
    {
        if (rank < 0 || rank >= rankVisuals.Length) return;

        RankVisual visual = rankVisuals[rank];
        if (rankImage != null && visual.sprite != null)
        {
            rankImage.sprite = visual.sprite;
            rankImage.color = Color.white;
        }

        if (taglineLabel != null && visual.accentColor.a > 0.01f)
            taglineLabel.color = Color.Lerp(Color.white, visual.accentColor, 0.35f);

        if (materialInstance == null) return;

        Vector4 bounds = ComputeBounds(visual);
        materialInstance.SetVector(FillBoundsId, bounds);
        if (visual.accentColor.a > 0.01f)
            materialInstance.SetColor(EdgeGlowColorId, Color.Lerp(visual.accentColor, Color.white, 0.55f));
    }

    private Vector4 ComputeBounds(RankVisual visual)
    {
        float vMin = 0f;
        float vMax = 1f;
        if (visual.sprite != null && visual.sprite.texture != null && visual.sprite.texture.height > 0)
        {
            float texHeight = visual.sprite.texture.height;
            vMin = visual.sprite.rect.y / texHeight;
            vMax = visual.sprite.rect.yMax / texHeight;
        }

        float bottom = visual.uvTop > visual.uvBottom ? visual.uvBottom : 0.24f;
        float top = visual.uvTop > visual.uvBottom ? visual.uvTop : 0.78f;
        return new Vector4(Mathf.Lerp(vMin, vMax, bottom), Mathf.Lerp(vMin, vMax, top), fillSoftness, fillEdgeWidth);
    }

    private void UpdateLabels(string tagline, int hits)
    {
        if (taglineLabel != null)
        {
            if (taglineLabel.gameObject.activeSelf != showTagline)
                taglineLabel.gameObject.SetActive(showTagline);
            if (showTagline && !string.Equals(lastTagline, tagline, StringComparison.Ordinal))
            {
                lastTagline = tagline;
                taglineLabel.text = tagline ?? string.Empty;
            }
        }

        if (hitCountLabel != null && displayedHitCount != hits)
        {
            displayedHitCount = hits;
            hitCountLabel.SetText("{0} HITS", hits);
        }
    }

    private void ApplyMotion(float dt, float rawWave, float rankT)
    {
        punchScale = Mathf.MoveTowards(punchScale, 0f, dt * punchDecaySpeed);
        twitchEnergy = Mathf.MoveTowards(twitchEnergy, 0f, dt * twitchDecaySpeed);
        slideOffset = Vector2.MoveTowards(slideOffset, Vector2.zero, dt * slideReturnSpeed);

        if (panelRoot != null)
            panelRoot.anchoredPosition = basePanelPosition + slideOffset;

        if (rankVisual == null) return;

        float pulseEnvelope = Mathf.Pow(rawWave, 2.5f);
        float idlePulse = pulseEnvelope * Mathf.Lerp(idleScalePulse, idleScalePulse * 1.55f, rankT);
        rankVisual.localScale = baseRankScale * (1f + idlePulse + punchScale);

        float jitterMag = idleJitterPixels * (0.35f + 0.65f * rankT) * pulseEnvelope + hitJitterPixels * twitchEnergy;
        float jitterX = Mathf.Sin(twitchTimer * 47f) * jitterMag;
        float jitterY = Mathf.Cos(twitchTimer * 61f) * (jitterMag * 0.75f);
        rankVisual.anchoredPosition = baseRankPosition + new Vector2(jitterX, jitterY);

        float tiltMag = idleTiltDegrees * (0.4f + 0.6f * rankT) * pulseEnvelope + hitTiltDegrees * twitchEnergy;
        float tilt = Mathf.Sin(twitchTimer * 39f) * tiltMag;
        rankVisual.localEulerAngles = new Vector3(0f, 0f, tilt);
    }

    private float RankRatio(int rank)
    {
        if (rank <= 0 || rankVisuals.Length <= 1) return 0f;
        return Mathf.Clamp01((float)rank / (rankVisuals.Length - 1));
    }

    private void ApplyShaderProperties(float fill, float flash)
    {
        if (materialInstance == null) return;
        materialInstance.SetFloat(FillAmountId, Mathf.Clamp01(fill));
        materialInstance.SetFloat(FlashAmountId, Mathf.Clamp01(flash));
    }

    private void ApplyCanvasGroup(float alpha)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    private void ResetVisualState()
    {
        displayedFill = 0f;
        flashBurst = 0f;
        FlashAmount = 0f;
        punchScale = 0f;
        twitchEnergy = 0f;
        slideOffset = Vector2.zero;
        visibleAlpha = 0f;
        wasVisible = false;
        ApplyCanvasGroup(0f);
        ApplyShaderProperties(0f, 0f);
        if (transformsCaptured)
        {
            if (panelRoot != null) panelRoot.anchoredPosition = basePanelPosition;
            if (rankVisual != null)
            {
                rankVisual.anchoredPosition = baseRankPosition;
                rankVisual.localScale = baseRankScale;
                rankVisual.localEulerAngles = Vector3.zero;
            }
        }
    }
}
