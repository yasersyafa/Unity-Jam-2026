using PrimeTween;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SlotPanelPresentation : MonoBehaviour
{
    [SerializeField] private SlotPresentationProfile profile;
    [Tooltip("Panel visual only. Keep the fullscreen backdrop outside this transform.")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private CanvasGroup visibility;
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
        animation.Stop();
        gameObject.SetActive(true);
        visibility.interactable = false;
        visibility.blocksRaycasts = true;
        if (profile == null || !profile.AnimatePanel) { Finish(visible); return; }
        animation = Tween.Custom(0f, 1f, visible ? profile.EnterDuration : profile.ExitDuration, t =>
        {
            float pop = 1f + 2.70158f * Mathf.Pow(t - 1f, 3) + 1.70158f * Mathf.Pow(t - 1f, 2);
            panel.localScale = scale * (visible ? Mathf.LerpUnclamped(profile.EnterScale, 1f, pop) : Mathf.Lerp(1f, 0.9f, t));
            panel.anchoredPosition = position + Vector2.down * (visible ? profile.EnterOffset * (1f - pop) : 30f * t);
            panel.localRotation = rotation * Quaternion.Euler(0f, 0f, visible ? profile.EnterAngle * (1f - pop) : 0f);
            visibility.alpha = visible ? t : 1f - t;
        }, useUnscaledTime: true).OnComplete(() => Finish(visible));
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
        panel.anchoredPosition = position;
        panel.localScale = scale;
        panel.localRotation = rotation;
    }
    private void OnDisable() { animation.Stop(); if (initialized) ResetPanel(); }
    [ContextMenu("Preview Open (Play Mode)")]
    private void Preview() { if (Application.isPlaying) Show(true); }
}
