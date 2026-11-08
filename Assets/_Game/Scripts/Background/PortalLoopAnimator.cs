using System;
using PrimeTween;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PortalLoopAnimator : MonoBehaviour
{
    [Serializable]
    private sealed class PortalLayer
    {
        public SpriteRenderer renderer;
        [Min(0.01f)] public float targetScale = 1f;
        [Range(0f, 1f)] public float targetAlpha = 1f;
        [Min(0f), Tooltip("Seconds for the fast appearance to blend into steady linear growth. Growth continues throughout; zero uses Growth Ease without an appearance. Included in Cycle Duration.")]
        public float appearanceDuration;
        [NonSerialized] public float activeAppearanceDuration;
        [Tooltip("Degrees per second around local Z. Negative rotates clockwise.")]
        public float rotationSpeed = -45f;
        [NonSerialized] public Vector3 initialScale;
        [NonSerialized] public Quaternion initialRotation;
        [NonSerialized] public Color initialColor;
        [NonSerialized] public double angle;
    }

    [SerializeField, Min(0.01f), Tooltip("Seconds per synchronized growth cycle.")]
    private float cycleDuration = 4f;
    [SerializeField] private Ease growthEase = Ease.Linear;
    [SerializeField, Tooltip("Continue during gameplay pause when enabled.")]
    private bool useUnscaledTime;
    [SerializeField] private PortalLayer[] portals;
    private Tween animation;
    private bool captured;
    private float activeDuration;
    private Ease activeEase;
    private bool activeUnscaled;

    private void OnEnable()
    {
        if (portals == null || portals.Length == 0) { Fail(); return; }
        for (int i = 0; i < portals.Length; i++)
        {
            if (portals[i] == null || portals[i].renderer == null) { Fail(); return; }
            for (int j = 0; j < i; j++)
                if (portals[j].renderer == portals[i].renderer) { Fail(); return; }
        }
        foreach (var layer in portals)
        {
            layer.initialScale = layer.renderer.transform.localScale;
            layer.initialRotation = layer.renderer.transform.localRotation;
            layer.initialColor = layer.renderer.color;
            layer.angle = 0d;
        }
        captured = true;
        RestartCycle();
    }
    private void Fail()
    {
        Debug.LogError("PortalLoopAnimator requires unique, assigned portal renderers.", this);
        enabled = false;
    }
    [ContextMenu("Restart Cycle (Play Mode)")]
    public void RestartCycle()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || !captured) return;
        animation.Stop();
        activeDuration = Safe(cycleDuration, 4f, 0.01f, float.MaxValue);
        activeEase = growthEase;
        activeUnscaled = useUnscaledTime;
        foreach (var layer in portals)
            layer.activeAppearanceDuration = AppearanceDuration(layer);
        Apply(0f);
        animation = Tween.Custom(0f, 1f, activeDuration, Apply, ease: Ease.Linear,
            cycles: -1, cycleMode: CycleMode.Restart, useUnscaledTime: activeUnscaled);
    }
    private void Apply(float progress)
    {
        foreach (var layer in portals)
        {
            if (layer.renderer == null) continue;
            float elapsed = progress * activeDuration;
            float appearance = layer.activeAppearanceDuration;
            // Appearance overlays the ongoing growth instead of starting a second tween.
            // The cubic correction reaches zero with zero velocity and acceleration.
            float growthProgress = appearance > 0f ? progress : Easing.Evaluate(progress, activeEase);
            Vector3 target = Vector3.one * Safe(layer.targetScale, 1f, 0.01f, float.MaxValue);
            Vector3 scale = Vector3.LerpUnclamped(layer.initialScale, target, growthProgress);
            if (appearance > 0f)
            {
                float remainingAppearance = 1f - Mathf.Clamp01(elapsed / appearance);
                scale -= layer.initialScale * (remainingAppearance * remainingAppearance * remainingAppearance);
            }
            layer.renderer.transform.localScale = scale;
            Color color = layer.initialColor;
            color.a = Mathf.Lerp(layer.initialColor.a, Safe(layer.targetAlpha, 1f, 0f, 1f), growthProgress);
            layer.renderer.color = color;
        }
    }
    private void Update()
    {
        if (!captured) return;
        if (activeDuration != Safe(cycleDuration, 4f, 0.01f, float.MaxValue)
            || activeEase != growthEase || activeUnscaled != useUnscaledTime) RestartCycle();
        foreach (var layer in portals)
            if (layer.activeAppearanceDuration != AppearanceDuration(layer)) { RestartCycle(); break; }
        double delta = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        foreach (var layer in portals)
        {
            if (layer.renderer == null) continue;
            layer.angle = (layer.angle + Safe(layer.rotationSpeed, -45f, -36000f, 36000f) * delta) % 360d;
            layer.renderer.transform.localRotation = layer.initialRotation * Quaternion.Euler(0f, 0f, (float)layer.angle);
        }
    }
    private void OnDisable()
    {
        animation.Stop();
        if (!captured) return;
        foreach (var layer in portals)
        {
            if (layer.renderer == null) continue;
            layer.renderer.transform.localScale = layer.initialScale;
            layer.renderer.transform.localRotation = layer.initialRotation;
            layer.renderer.color = layer.initialColor;
        }
        captured = false;
    }
    private float AppearanceDuration(PortalLayer layer)
    {
        return Safe(layer.appearanceDuration, 0f, 0f, Mathf.Max(0f, activeDuration - 0.01f));
    }
    private void OnValidate() { cycleDuration = Safe(cycleDuration, 4f, 0.01f, float.MaxValue); }
    private static float Safe(float value, float fallback, float min, float max)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
    }
}
