using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Comic-style button: swaps sprite on hover/press, punches in with a tilt and pulses to a beat while hovered.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ComicButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    [Header("Sprites")]
    [Tooltip("Defaults to the Button's target graphic.")]
    [SerializeField] private Image image;
    [Tooltip("Defaults to the Image's starting sprite.")]
    [SerializeField] private Sprite normalSprite;
    [Tooltip("Also shown while pressed.")]
    [SerializeField] private Sprite hoverSprite;

    [Header("Punch")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float hoverTilt = -4f;
    [SerializeField, Min(0.01f)] private float punchDuration = 0.18f;
    [SerializeField] private float pressedScale = 0.92f;
    [SerializeField, Min(0.01f)] private float pressDuration = 0.06f;

    [Header("Beat Pulse")]
    [SerializeField, Range(0f, 0.2f)] private float pulseAmount = 0.03f;
    [SerializeField, Min(1f)] private float beatsPerMinute = 120f;

    private Button button;
    private Vector3 baseScale;
    private Vector3 baseEuler;
    private Tween scaleTween;
    private Tween rotationTween;
    private bool hovered;
    private bool selected;
    private bool pressed;

    private bool Highlighted => hovered || selected;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (image == null) image = button.targetGraphic as Image;
        if (normalSprite == null && image != null) normalSprite = image.sprite;
        baseScale = transform.localScale;
        baseEuler = transform.localEulerAngles;
    }

    public void OnPointerEnter(PointerEventData data) { hovered = true; Refresh(); }
    public void OnPointerExit(PointerEventData data) { hovered = false; pressed = false; Refresh(); }
    // Clicking also selects the button; only keyboard/gamepad selection should keep it highlighted.
    public void OnSelect(BaseEventData data) { selected = data is not PointerEventData; Refresh(); }
    public void OnDeselect(BaseEventData data) { selected = false; Refresh(); }

    public void OnPointerDown(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        pressed = true;
        Refresh();
    }

    public void OnPointerUp(PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        pressed = false;
        Refresh();
    }

    private void Refresh()
    {
        StopTweens();
        if (!button.IsInteractable())
        {
            ResetVisual();
            return;
        }

        if (pressed)
        {
            SetSprite(hoverSprite);
            scaleTween = ScaleTo(baseScale * pressedScale, pressDuration, Ease.OutQuad);
            rotationTween = RotateTo(Tilted(hoverTilt * 0.5f), pressDuration, Ease.OutQuad);
        }
        else if (Highlighted)
        {
            SetSprite(hoverSprite);
            scaleTween = ScaleTo(baseScale * hoverScale, punchDuration, Ease.OutBack);
            if (scaleTween.isAlive) scaleTween.OnComplete(this, target => target.StartPulse());
            else StartPulse();
            rotationTween = RotateTo(Tilted(hoverTilt), punchDuration, Ease.OutBack);
        }
        else
        {
            SetSprite(normalSprite);
            float duration = punchDuration * 0.8f;
            scaleTween = ScaleTo(baseScale, duration, Ease.OutBack);
            rotationTween = RotateTo(baseEuler, duration, Ease.OutBack);
        }
    }

    // PrimeTween warns when a tween starts at its end value, so snap instead of tweening in place.
    private Tween ScaleTo(Vector3 target, float duration, Ease ease)
    {
        if ((transform.localScale - target).sqrMagnitude <= 0.000001f)
        {
            transform.localScale = target;
            return default;
        }
        return Tween.Scale(transform, target, duration, ease, useUnscaledTime: true);
    }

    private Tween RotateTo(Vector3 targetEuler, float duration, Ease ease)
    {
        if (Quaternion.Angle(transform.localRotation, Quaternion.Euler(targetEuler)) <= 0.01f)
        {
            transform.localEulerAngles = targetEuler;
            return default;
        }
        return Tween.LocalRotation(transform, targetEuler, duration, ease, useUnscaledTime: true);
    }

    private void StartPulse()
    {
        if (pulseAmount <= 0f) return;
        float halfBeat = 30f / beatsPerMinute;
        scaleTween = Tween.Scale(transform, baseScale * (hoverScale * (1f + pulseAmount)), halfBeat, Ease.InOutSine,
            cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
    }

    private void Update()
    {
        // Disabling interactability mid-hover should drop the highlight immediately.
        if (!button.IsInteractable() && (scaleTween.isAlive || transform.localScale != baseScale)) Refresh();
    }

    private Vector3 Tilted(float angle) => new(baseEuler.x, baseEuler.y, baseEuler.z + angle);

    private void SetSprite(Sprite sprite)
    {
        if (image != null && sprite != null) image.sprite = sprite;
    }

    private void StopTweens()
    {
        scaleTween.Stop();
        rotationTween.Stop();
    }

    private void ResetVisual()
    {
        transform.localScale = baseScale;
        transform.localEulerAngles = baseEuler;
        SetSprite(normalSprite);
    }

    private void OnDisable()
    {
        StopTweens();
        hovered = selected = pressed = false;
        ResetVisual();
    }
}
