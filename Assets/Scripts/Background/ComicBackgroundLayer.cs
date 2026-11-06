using System;
using UnityEngine;

/// <summary>
/// Fits a Simple SpriteRenderer to an orthographic viewport and optionally animates it.
/// Use camera-aligned sprites with centered pivots and an unscaled parent hierarchy.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class ComicBackgroundLayer : MonoBehaviour
{
    [Header("Camera & Fitting")]
    [SerializeField, Tooltip("Orthographic camera to cover. Falls back to MainCamera when unassigned.")]
    private Camera targetCamera = null;
    [SerializeField, Min(1f), Tooltip("Extra coverage for floating motion and the shrinking half of a pulse.")]
    private float overscan = 1f;

    [Header("Floating Animation")]
    [SerializeField, Tooltip("Animate local position in Play Mode only.")]
    private bool enableFloating = false;
    [SerializeField, Tooltip("Maximum local X/Y displacement from the authored position, in world units with unit parent scale.")]
    private Vector2 floatAmplitude;
    [SerializeField, Min(0f), Tooltip("Complete floating loops per second. Zero leaves position at its base.")]
    private float floatCyclesPerSecond = 0.1f;
    [SerializeField, Tooltip("Floating phase in cycles; 0.25 is a quarter turn.")]
    private float floatPhaseOffset;

    [Header("Pulse Animation")]
    [SerializeField, Tooltip("Animate uniform scale in Play Mode only.")]
    private bool enablePulse = false;
    [SerializeField, Range(0f, 0.95f), Tooltip("Fraction of fitted scale. 0.035 means a +/-3.5% pulse.")]
    private float pulseAmplitude;
    [SerializeField, Min(0f), Tooltip("Complete pulse loops per second. Zero leaves scale at its fitted value.")]
    private float pulseCyclesPerSecond = 1f;
    [SerializeField, Tooltip("Pulse phase in cycles; 0.25 is a quarter turn.")]
    private float pulsePhaseOffset;

    private SpriteRenderer spriteRenderer;
    private Transform layerTransform;
    private Vector3 baseLocalPosition;
    private float fittedScale;
    private bool hasFit;
    private bool playing;
    private bool initialized;
    private bool warnedParentScale;
    private bool warnedPerspective;

    private void OnEnable()
    {
        layerTransform = transform;
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseLocalPosition = layerTransform.localPosition;
        playing = Application.IsPlaying(gameObject);
        initialized = true;
        hasFit = false;
        // Fitting precedes animation, including the first runtime frame.
        RefreshFit();
        ApplyTransform();
    }

    private void OnValidate()
    {
        // OnValidate may run during loading, off the main thread. Only sanitize data here.
        overscan = Sanitize(overscan, 1f, float.MaxValue, 1f);
        floatCyclesPerSecond = Sanitize(floatCyclesPerSecond, 0f, float.MaxValue, 0.1f);
        pulseCyclesPerSecond = Sanitize(pulseCyclesPerSecond, 0f, float.MaxValue, 1f);
        pulseAmplitude = Sanitize(pulseAmplitude, 0f, 0.95f, 0f);
        floatAmplitude.x = Sanitize(floatAmplitude.x, -float.MaxValue, float.MaxValue, 0f);
        floatAmplitude.y = Sanitize(floatAmplitude.y, -float.MaxValue, float.MaxValue, 0f);
        floatPhaseOffset = Sanitize(floatPhaseOffset, -float.MaxValue, float.MaxValue, 0f);
        pulsePhaseOffset = Sanitize(pulsePhaseOffset, -float.MaxValue, float.MaxValue, 0f);
    }

    private void LateUpdate()
    {
        if (!initialized)
            return;

        bool isPlaying = Application.IsPlaying(gameObject);
        if (playing && !isPlaying)
            SetLocalPosition(baseLocalPosition);
        else if (!playing)
            baseLocalPosition = layerTransform.localPosition;

        playing = isPlaying;
        RefreshFit();
        ApplyTransform();
    }

    private void OnDisable()
    {
        if (!initialized)
            return;

        // Restore before domain reload or component disable; never capture an animated pose as the base.
        if (playing)
        {
            SetLocalPosition(baseLocalPosition);
            if (hasFit)
                SetLocalScale(fittedScale);
        }

        initialized = false;
    }

    private void RefreshFit()
    {
        Transform parent = layerTransform.parent;
        bool invalidParentScale = parent != null &&
            (parent.lossyScale - Vector3.one).sqrMagnitude > 0.000001f;
        if (invalidParentScale && !warnedParentScale)
            Debug.LogWarning("ComicBackgroundLayer requires unit scale throughout its parent hierarchy. Viewport fitting may be incorrect.", this);
        warnedParentScale = invalidParentScale;

        Camera cameraToUse = targetCamera != null ? targetCamera : Camera.main;
        bool perspective = cameraToUse != null && !cameraToUse.orthographic;
        if (perspective && !warnedPerspective)
            Debug.LogWarning("ComicBackgroundLayer requires an orthographic camera.", this);
        warnedPerspective = perspective;

        Sprite sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
        if (cameraToUse == null || perspective || sprite == null)
            return;

        Vector3 spriteSize = sprite.bounds.size;
        float visibleHeight = cameraToUse.orthographicSize * 2f;
        float visibleWidth = visibleHeight * cameraToUse.aspect;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f || visibleHeight <= 0f || visibleWidth <= 0f)
            return;

        // The larger ratio covers both dimensions. A single factor preserves artwork proportions.
        float scale = Mathf.Max(visibleWidth / spriteSize.x, visibleHeight / spriteSize.y) * overscan;
        if (float.IsNaN(scale) || float.IsInfinity(scale) || scale <= 0f)
            return;

        // Read current inputs each tick so resized views and reimported sprite bounds are handled.
        // Transform setters below write only on change, avoiding repeated Edit Mode scene mutations.
        fittedScale = scale;
        hasFit = true;
    }

    private void ApplyTransform()
    {
        if (!hasFit)
        {
            if (playing)
                SetLocalPosition(baseLocalPosition);
            return;
        }

        Vector3 position = baseLocalPosition;
        float pulse = 1f;
        if (playing)
        {
            // Absolute double-precision time prevents integrated position/scale drift and long-session jitter.
            // Scaled game time intentionally pauses both animations when timeScale is zero.
            double time = Time.timeAsDouble;
            if (enableFloating && floatCyclesPerSecond > 0f)
            {
                double angle = LoopAngle(time, floatCyclesPerSecond, floatPhaseOffset);
                position.x += floatAmplitude.x * (float)Math.Sin(angle);
                position.y += floatAmplitude.y * (float)Math.Cos(angle);
            }

            if (enablePulse && pulseCyclesPerSecond > 0f)
            {
                double angle = LoopAngle(time, pulseCyclesPerSecond, pulsePhaseOffset);
                pulse += Mathf.Clamp(pulseAmplitude, 0f, 0.95f) * (float)Math.Sin(angle);
            }
        }

        if (playing)
            SetLocalPosition(position);
        SetLocalScale(fittedScale * Mathf.Max(0.05f, pulse));
    }

    private void SetLocalPosition(Vector3 position)
    {
        if (!layerTransform.localPosition.Equals(position))
            layerTransform.localPosition = position;
    }

    private void SetLocalScale(float scale)
    {
        Vector3 uniformScale = Vector3.one * scale;
        if (!layerTransform.localScale.Equals(uniformScale))
            layerTransform.localScale = uniformScale;
    }

    private static double LoopAngle(double time, float cyclesPerSecond, float phaseOffset)
    {
        return (time * cyclesPerSecond + phaseOffset) % 1.0 * (Math.PI * 2.0);
    }

    private static float Sanitize(float value, float minimum, float maximum, float fallback)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, minimum, maximum);
    }
}
