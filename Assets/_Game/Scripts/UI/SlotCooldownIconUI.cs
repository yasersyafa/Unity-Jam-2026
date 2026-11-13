using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom HUD slot-machine icon that fills upward during SlotGameplayFlow cooldown
/// and rocks left-right while blinking white once ready to open.
/// </summary>
[DisallowMultipleComponent]
public sealed class SlotCooldownIconUI : MonoBehaviour
{
    private static readonly int FillAmountId = Shader.PropertyToID("_FillAmount");
    private static readonly int FillBoundsId = Shader.PropertyToID("_FillBounds");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");

    [Header("Data Sources")]
    [SerializeField] private SlotGameplayFlow flow;
    [SerializeField] private PlayerHealth player;

    [Header("UI References")]
    [SerializeField] private RectTransform visualRoot;
    [SerializeField] private Image iconImage;
    [SerializeField] private Material iconMaterial;

    [Header("Fill Shader")]
    [SerializeField, Range(0.001f, 0.1f)] private float fillSoftness = 0.02f;
    [SerializeField, Range(0.005f, 0.12f)] private float fillEdgeWidth = 0.045f;

    [Header("Ready Animation")]
    [SerializeField, Min(0.1f)] private float rockFrequency = 2.4f;
    [SerializeField, Min(0f)] private float rockAngle = 11f;
    [SerializeField, Min(0.1f)] private float blinkFrequency = 2.6f;
    [SerializeField, Min(1f)] private float blinkSharpness = 2.4f;
    [SerializeField, Range(0f, 1f)] private float readyFlashPeak = 0.75f;
    [SerializeField, Min(0f)] private float readyScalePulse = 0.08f;
    [SerializeField, Min(0f)] private float readyFinishPunch = 0.22f;
    [SerializeField, Min(0.5f)] private float punchDecaySpeed = 4f;

    private Material materialInstance;
    private Vector3 baseScale = Vector3.one;
    private Quaternion baseRotation = Quaternion.identity;
    private bool transformsCaptured;
    private bool hasUpdated;
    private bool wasReady;

    private float readyTimer;
    private float flashBurst;
    private float punchScale;

    public float DisplayedFill { get; private set; }
    public float FlashAmount { get; private set; }
    public bool IsReady { get; private set; }

    public void Configure(
        SlotGameplayFlow gameplayFlow,
        PlayerHealth playerHealth,
        RectTransform rootRect,
        Image image,
        Material shaderMaterial)
    {
        flow = gameplayFlow;
        player = playerHealth;
        visualRoot = rootRect;
        iconImage = image;
        iconMaterial = shaderMaterial;

        transformsCaptured = false;
        CaptureBaseTransforms();
        RecreateMaterial();
        ResetState();
    }

    private void Awake()
    {
        if (flow == null) flow = FindAnyObjectByType<SlotGameplayFlow>();
        if (player == null) player = GetComponentInParent<PlayerHealth>() ?? FindAnyObjectByType<PlayerHealth>();
        if (visualRoot == null) visualRoot = transform as RectTransform;
        if (iconImage == null) iconImage = GetComponent<Image>();

        CaptureBaseTransforms();
        EnsureMaterial();
        ResetState();
    }

    private void OnEnable()
    {
        if (flow == null) flow = FindAnyObjectByType<SlotGameplayFlow>();
        if (player == null) player = GetComponentInParent<PlayerHealth>() ?? FindAnyObjectByType<PlayerHealth>();
        CaptureBaseTransforms();
        EnsureMaterial();
    }

    private void OnDisable()
    {
        ResetState();
    }

    private void OnDestroy()
    {
        if (materialInstance != null)
        {
            Destroy(materialInstance);
            materialInstance = null;
        }
    }

    private void Update()
    {
        if (flow == null) flow = FindAnyObjectByType<SlotGameplayFlow>();

        bool hidden = flow == null || flow.IsOpen || (player != null && player.IsDead);
        if (iconImage != null && iconImage.enabled == hidden)
            iconImage.enabled = !hidden;

        if (hidden)
        {
            ResetMotion();
            hasUpdated = false;
            wasReady = false;
            IsReady = false;
            return;
        }

        float dt = Time.unscaledDeltaTime;
        float cooldown = flow.SlotCooldown;
        float remaining = Mathf.Max(0f, flow.RemainingCooldown);
        float fill = cooldown > 0f ? 1f - Mathf.Clamp01(remaining / cooldown) : 1f;
        bool ready = remaining <= 0f && flow.ManualOpenAllowed;

        DisplayedFill = fill;
        IsReady = ready;

        if (ready)
        {
            if (hasUpdated && !wasReady)
            {
                readyTimer = 0f;
                punchScale = readyFinishPunch;
                flashBurst = readyFlashPeak;
            }
            UpdateReadyAnimation(dt);
        }
        else
        {
            ResetMotion();
        }

        ApplyShaderProperties(fill, FlashAmount);
        hasUpdated = true;
        wasReady = ready;
    }

    private void UpdateReadyAnimation(float dt)
    {
        readyTimer += dt;

        float blinkWave = 0.5f + 0.5f * Mathf.Sin(readyTimer * Mathf.PI * 2f * blinkFrequency);
        float idleFlash = Mathf.Pow(blinkWave, blinkSharpness) * readyFlashPeak;
        flashBurst = Mathf.MoveTowards(flashBurst, 0f, dt * punchDecaySpeed);
        FlashAmount = Mathf.Clamp01(Mathf.Max(flashBurst, idleFlash));

        punchScale = Mathf.MoveTowards(punchScale, 0f, dt * punchDecaySpeed);
        float scale = 1f + Mathf.Pow(blinkWave, 2f) * readyScalePulse + punchScale;
        float rock = Mathf.Sin(readyTimer * Mathf.PI * 2f * rockFrequency) * rockAngle;

        if (visualRoot != null)
        {
            visualRoot.localScale = baseScale * scale;
            visualRoot.localRotation = baseRotation * Quaternion.Euler(0f, 0f, rock);
        }
    }

    private void CaptureBaseTransforms()
    {
        if (transformsCaptured || visualRoot == null) return;
        baseScale = visualRoot.localScale;
        baseRotation = visualRoot.localRotation;
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
        if (iconImage == null || materialInstance != null) return;
        Shader shader = iconMaterial != null ? iconMaterial.shader : Shader.Find("Game/UI Combo Rank");
        if (shader == null) return;

        materialInstance = iconMaterial != null
            ? new Material(iconMaterial)
            : new Material(shader);
        materialInstance.name = $"SlotMachineIconFill ({name})";
        materialInstance.hideFlags = HideFlags.DontSave;
        iconImage.material = materialInstance;
        ApplyBounds();
    }

    private void ApplyBounds()
    {
        if (materialInstance == null) return;
        float vMin = 0f;
        float vMax = 1f;
        Sprite sprite = iconImage != null ? iconImage.sprite : null;
        if (sprite != null && sprite.texture != null && sprite.texture.height > 0)
        {
            float texHeight = sprite.texture.height;
            vMin = sprite.rect.y / texHeight;
            vMax = sprite.rect.yMax / texHeight;
        }
        materialInstance.SetVector(FillBoundsId, new Vector4(vMin, vMax, fillSoftness, fillEdgeWidth));
    }

    private void ApplyShaderProperties(float fill, float flash)
    {
        if (materialInstance == null) return;
        materialInstance.SetFloat(FillAmountId, Mathf.Clamp01(fill));
        materialInstance.SetFloat(FlashAmountId, Mathf.Clamp01(flash));
    }

    private void ResetMotion()
    {
        readyTimer = 0f;
        flashBurst = 0f;
        punchScale = 0f;
        FlashAmount = 0f;
        if (transformsCaptured && visualRoot != null)
        {
            visualRoot.localScale = baseScale;
            visualRoot.localRotation = baseRotation;
        }
    }

    private void ResetState()
    {
        DisplayedFill = 0f;
        IsReady = false;
        hasUpdated = false;
        wasReady = false;
        ResetMotion();
        ApplyShaderProperties(0f, 0f);
    }
}
