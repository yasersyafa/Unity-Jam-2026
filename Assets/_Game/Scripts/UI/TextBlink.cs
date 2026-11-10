using PrimeTween;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public sealed class TextBlink : MonoBehaviour
{
    [Tooltip("Duration in seconds for each fade direction.")]
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;
    private Tween blinkTween;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        canvasGroup.alpha = 1f;
        blinkTween = Tween.Alpha(canvasGroup, 1f, 0f, Mathf.Max(0.01f, fadeDuration),
            Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
    }

    private void OnDisable()
    {
        blinkTween.Stop();
        blinkTween = default;
        if (canvasGroup != null) canvasGroup.alpha = 1f;
    }
}
