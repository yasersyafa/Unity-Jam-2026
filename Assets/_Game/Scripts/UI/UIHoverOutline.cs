using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Yellow comic outline around a UI graphic's alpha shape while it is hovered or selected. Uses the
/// "Game/UI Outline" shader through a per-object material instance, so hovering one object never lights up another.
/// Simple (4-vertex) images are enlarged by <see cref="padding"/> so the outline fits; sliced, tiled and filled
/// images need transparent padding inside the sprite instead. Not for TextMeshPro, which has its own outline.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Graphic))]
public sealed class UIHoverOutline : BaseMeshEffect, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    private static readonly int AmountId = Shader.PropertyToID("_OutlineAmount");
    private static readonly int WidthId = Shader.PropertyToID("_OutlineWidth");
    private static readonly int ColorId = Shader.PropertyToID("_OutlineColor");

    [Tooltip("Material using the Game/UI Outline shader. Filled in automatically when the component is added.")]
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Color color = new(1f, 0.86f, 0.12f, 1f);
    [Tooltip("Outline thickness in sprite texels.")]
    [SerializeField, Min(0f)] private float width = 4f;
    [Tooltip("UI units a simple quad grows on each side so the outline is not cut off. Keep it above width x pulse scale.")]
    [SerializeField, Min(0f)] private float padding = 8f;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.1f;
    [Tooltip("Peak thickness multiplier of the pulse while hovered.")]
    [SerializeField, Min(1f)] private float pulseScale = 1.35f;
    [SerializeField, Min(0.05f)] private float pulseDuration = 0.35f;

    private Material instance;
    private Tween fade;
    private Tween pulse;
    private float amount;
    private bool hovered;
    private bool selected;

    // The graphic needs Raycast Target enabled to receive pointer enter/exit.
    public void OnPointerEnter(PointerEventData eventData) { hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData eventData) { hovered = false; Refresh(); }
    public void OnSelect(BaseEventData eventData) { selected = true; Refresh(); }
    public void OnDeselect(BaseEventData eventData) { selected = false; Refresh(); }

    protected override void Awake()
    {
        base.Awake();
        if (!Application.isPlaying) return; // Edit mode keeps the graphic's own material and mesh untouched.
        if (outlineMaterial == null)
        {
            Debug.LogWarning("UIHoverOutline needs a material using the Game/UI Outline shader.", this);
            return;
        }
        instance = new Material(outlineMaterial) { name = $"{outlineMaterial.name} ({name})", hideFlags = HideFlags.DontSave };
        instance.SetColor(ColorId, color);
        instance.SetFloat(WidthId, width);
        instance.SetFloat(AmountId, 0f);
        graphic.material = instance;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (instance == null) return;
        // The sprite's UV rect travels in TEXCOORD1, which canvases strip unless enabled.
        var canvas = graphic.canvas;
        if (canvas != null)
        {
            canvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            if (canvas.rootCanvas != null) canvas.rootCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        }
    }

    protected override void OnDisable()
    {
        hovered = selected = false;
        fade.Stop();
        StopPulse();
        SetAmount(0f);
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        fade.Stop();
        pulse.Stop();
        if (instance != null)
        {
            if (graphic != null && graphic.material == instance) graphic.material = null;
            Destroy(instance);
            instance = null;
        }
        base.OnDestroy();
    }

    private void Refresh()
    {
        if (instance == null) return;
        bool on = hovered || selected;
        fade.Stop();
        fade = Tween.Custom(this, amount, on ? 1f : 0f, fadeDuration, (target, value) => target.SetAmount(value),
            Ease.OutQuad, useUnscaledTime: true);
        if (!on) { StopPulse(); return; }
        if (!pulse.isAlive && pulseScale > 1f)
            pulse = Tween.Custom(this, width, width * pulseScale, pulseDuration,
                (target, value) => target.instance.SetFloat(WidthId, value),
                Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
    }

    private void StopPulse()
    {
        pulse.Stop();
        if (instance != null) instance.SetFloat(WidthId, width);
    }

    private void SetAmount(float value)
    {
        amount = value;
        if (instance != null) instance.SetFloat(AmountId, value);
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive() || instance == null) return;
        int count = vh.currentVertCount;
        if (count == 0) return;

        var vertex = new UIVertex();
        Vector2 minUv = Vector2.positiveInfinity, maxUv = Vector2.negativeInfinity;
        Vector2 minPos = Vector2.positiveInfinity, maxPos = Vector2.negativeInfinity;
        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            minUv = Vector2.Min(minUv, vertex.uv0);
            maxUv = Vector2.Max(maxUv, vertex.uv0);
            minPos = Vector2.Min(minPos, vertex.position);
            maxPos = Vector2.Max(maxPos, vertex.position);
        }

        var uvRect = new Vector4(minUv.x, minUv.y, maxUv.x, maxUv.y);
        Vector2 size = maxPos - minPos;
        bool grow = count == 4 && padding > 0f && size.x > 0f && size.y > 0f;
        Vector2 center = (minPos + maxPos) * 0.5f;
        Vector2 uvPerUnit = grow ? new Vector2((maxUv.x - minUv.x) / size.x, (maxUv.y - minUv.y) / size.y) : Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            if (grow)
            {
                Vector2 position = vertex.position;
                var outward = new Vector2(Mathf.Sign(position.x - center.x), Mathf.Sign(position.y - center.y)) * padding;
                vertex.position = new Vector3(position.x + outward.x, position.y + outward.y, vertex.position.z);
                Vector4 uv = vertex.uv0;
                vertex.uv0 = new Vector4(uv.x + outward.x * uvPerUnit.x, uv.y + outward.y * uvPerUnit.y, uv.z, uv.w);
            }
            vertex.uv1 = uvRect;
            vh.SetUIVertex(vertex, i);
        }
    }
}
