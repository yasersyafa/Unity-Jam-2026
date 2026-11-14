using PrimeTween;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ScalePulse : MonoBehaviour
{
    [Tooltip("Transform or RectTransform to animate. Defaults to this object's transform.")]
    [SerializeField] private Transform target;
    [Tooltip("Scale multipliers relative to the target's original local scale.")]
    [SerializeField, Min(0f)] private float minScale = 1f;
    [SerializeField, Min(0f)] private float maxScale = 1.1f;
    [Tooltip("Duration in seconds for each direction, not the full cycle.")]
    [SerializeField, Min(0.01f)] private float duration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutSine;
    [SerializeField] private bool useUnscaledTime = true;

    private Transform activeTarget;
    private Vector3 originalScale;
    private Tween scaleTween;

    private void OnEnable()
    {
        StopPulse();
        activeTarget = target != null ? target : transform;
        originalScale = activeTarget.localScale;
        float lowerScale = Mathf.Max(0f, minScale);
        float upperScale = Mathf.Max(lowerScale, maxScale);
        scaleTween = Tween.Scale(activeTarget, originalScale * lowerScale, originalScale * upperScale,
            Mathf.Max(0.01f, duration), ease, cycles: -1, cycleMode: CycleMode.Yoyo,
            useUnscaledTime: useUnscaledTime);
    }

    private void OnDisable()
    {
        StopPulse();
    }

    private void OnDestroy()
    {
        StopPulse();
    }

    private void StopPulse()
    {
        scaleTween.Stop();
        scaleTween = default;
        if (activeTarget != null)
        {
            activeTarget.localScale = originalScale;
        }
        activeTarget = null;
    }
}
