using PrimeTween;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SlotPanelPresentation : MonoBehaviour
{
    [SerializeField] private SlotPresentationProfile profile;
    [Tooltip("Panel visual only. Keep the fullscreen backdrop outside this transform.")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup visibility;
    [SerializeField] private RectTransform dockTarget;
    private SlotGenieMeshEffect[] genieEffects;
    private float genieAmount;
    private UnityEngine.UI.RectMask2D[] genieMasks;
    private Tween animation;
    private Vector2 position;
    private Vector3 scale;
    private Quaternion rotation;
    private bool initialized;
    public bool IsPlaying => animation.isAlive;
    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        position = panel.anchoredPosition;
        scale = panel.localScale;
        rotation = panel.localRotation;
    }
    public void Show(bool visible)
    {
        Initialize();
        bool interrupted = animation.isAlive;
        animation.Stop();
        gameObject.SetActive(true);
        visibility.interactable = false;
        visibility.blocksRaycasts = true;
        if (profile == null || !profile.AnimatePanel) { Finish(visible); return; }
        if (dockTarget != null)
        {
            if (genieEffects == null)
            {
                var graphics = panel.GetComponentsInChildren<UnityEngine.UI.Graphic>(true);
                genieEffects = new SlotGenieMeshEffect[graphics.Length];
                for (int i = 0; i < graphics.Length; i++)
                    genieEffects[i] = graphics[i].GetComponent<SlotGenieMeshEffect>() ?? graphics[i].gameObject.AddComponent<SlotGenieMeshEffect>();
            }
            if (genieMasks == null)
                genieMasks = System.Array.FindAll(panel.GetComponentsInChildren<UnityEngine.UI.RectMask2D>(true), mask => mask.enabled);
            foreach (var mask in genieMasks) if (mask != null) mask.enabled = false;
            if (!interrupted) genieAmount = visible ? 1f : 0f;
            float start = genieAmount;
            animation = Tween.Custom(0f, 1f, visible ? profile.EnterDuration : profile.ExitDuration, t =>
            {
                genieAmount = Mathf.Lerp(start, visible ? 0f : 1f, t);
                UpdateGenie();
                visibility.alpha = visible ? Mathf.Clamp01(t * 5f) : Mathf.Clamp01((1f - t) * 5f);
            }, useUnscaledTime: true).OnComplete(() => Finish(visible));
            UpdateGenie();
            return;
        }
        animation = Tween.Custom(0f, 1f, visible ? profile.EnterDuration : profile.ExitDuration, t =>
        {
            float pop = 1f + 2.70158f * Mathf.Pow(t - 1f, 3) + 1.70158f * Mathf.Pow(t - 1f, 2);
            panel.localScale = scale * (visible ? Mathf.LerpUnclamped(profile.EnterScale, 1f, pop) : Mathf.Lerp(1f, 0.9f, t));
            panel.anchoredPosition = position + Vector2.down * (visible ? profile.EnterOffset * (1f - pop) : 30f * t);
            panel.localRotation = rotation * Quaternion.Euler(0f, 0f, visible ? profile.EnterAngle * (1f - pop) : 0f);
            visibility.alpha = visible ? t : 1f - t;
        }, useUnscaledTime: true).OnComplete(() => Finish(visible));
    }
    private void UpdateGenie()
    {
        if (dockTarget == null || genieEffects == null) return;
        Canvas source = dockTarget.GetComponentInParent<Canvas>();
        Canvas destination = panel.GetComponentInParent<Canvas>();
        Camera sourceCamera = source != null && source.renderMode != RenderMode.ScreenSpaceOverlay ? source.worldCamera : null;
        Camera destinationCamera = destination != null && destination.renderMode != RenderMode.ScreenSpaceOverlay ? destination.worldCamera : null;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(sourceCamera, dockTarget.TransformPoint(dockTarget.rect.center));
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(panel, screen, destinationCamera, out Vector2 local))
            foreach (var effect in genieEffects)
                if (effect != null) effect.Configure(panel, local, genieAmount, profile.DockScale);
    }
    public void HideImmediate()
    {
        animation.Stop();
        if (initialized) ResetPanel();
        if (visibility != null) { visibility.alpha = 0f; visibility.interactable = false; visibility.blocksRaycasts = false; }
        gameObject.SetActive(false);
    }
    private void Finish(bool visible)
    {
        ResetPanel();
        visibility.alpha = visible ? 1f : 0f;
        visibility.interactable = visible;
        visibility.blocksRaycasts = visible;
        if (!visible) gameObject.SetActive(false);
    }
    private void ResetPanel()
    {
        genieAmount = 0f;
        UpdateGenie();
        if (genieMasks != null) foreach (var mask in genieMasks) if (mask != null) mask.enabled = true;
        panel.anchoredPosition = position;
        panel.localScale = scale;
        panel.localRotation = rotation;
    }
    private void OnDisable() { animation.Stop(); if (initialized) ResetPanel(); }
    [ContextMenu("Preview Open (Play Mode)")]
    private void Preview() { if (Application.isPlaying) Show(true); }
}
