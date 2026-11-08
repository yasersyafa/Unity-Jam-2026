using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public sealed class ComicButtonFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler, ISubmitHandler
{
    [SerializeField] private SlotPresentationProfile profile;
    [Tooltip("Dedicated visual child, leaving the button hit area fixed.")]
    [SerializeField] private RectTransform visual;
    private Button button;
    private Vector3 scale;
    private Tween animation;
    private bool hovered;
    private bool selected;
    private bool wasInteractable;
    private void Awake() { button = GetComponent<Button>(); scale = visual.localScale; }
    private void Animate(float multiplier)
    {
        animation.Stop();
        if (profile == null || !profile.AnimateButtons || !button.IsInteractable()) { visual.localScale = scale; return; }
        Vector3 targetScale = scale * multiplier;
        if ((visual.localScale - targetScale).sqrMagnitude <= 0.000001f)
        {
            visual.localScale = targetScale;
            return;
        }
        animation = Tween.Scale(visual, targetScale, profile.ButtonDuration, useUnscaledTime: true);
    }
    private float Rest => hovered || selected ? profile != null ? profile.HoverScale : 1f : 1f;
    public void OnPointerEnter(PointerEventData data) { hovered = true; Animate(Rest); }
    public void OnPointerExit(PointerEventData data) { hovered = false; Animate(Rest); }
    public void OnPointerDown(PointerEventData data) { if (data.button == PointerEventData.InputButton.Left) Animate(profile != null ? profile.PressedScale : 1f); }
    public void OnPointerUp(PointerEventData data) { Animate(Rest); }
    public void OnSelect(BaseEventData data) { selected = true; Animate(Rest); }
    public void OnDeselect(BaseEventData data) { selected = false; Animate(Rest); }
    public void OnSubmit(BaseEventData data)
    {
        if (!button.IsInteractable() || profile == null || !profile.AnimateButtons) return;
        Animate(profile.PressedScale);
        if (animation.isAlive) animation.OnComplete(() => Animate(Rest));
        else Animate(Rest);
    }
    private void Update()
    {
        bool active = button.IsInteractable();
        if (active != wasInteractable) { wasInteractable = active; Animate(Rest); }
    }
    private void OnDisable() { animation.Stop(); hovered = selected = false; if (visual != null) visual.localScale = scale; }
}
