using System;
using PrimeTween;
using UnityEngine;

public sealed class BossIntroPresentation : MonoBehaviour
{
    [SerializeField] private CanvasGroup root;
    [SerializeField] private CanvasGroup background;
    [SerializeField] private CanvasGroup dialogue;
    [SerializeField] private CanvasGroup title;
    [SerializeField] private RectTransform portrait;
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private UnityEngine.UI.Image bossImage;
    [SerializeField] private Sprite[] bossFrames;
    [Header("Wheel Animation (12 FPS)")]
    [SerializeField] private Sprite[] wheelFrames;
    [SerializeField, Min(0)] private int wheelRepeatCount = 2;
    [SerializeField] private Sprite wheelFinalSprite;
    private UnityEngine.UI.Image wheelImage;
    private Sprite wheelOriginalSprite;
    [SerializeField] private float duration = 5.02f;
    private Tween animation;
    private Action completed;
    private Vector2 portraitOrigin, titleOrigin;
    private void Awake()
    {
        wheelImage = portrait.GetComponent<UnityEngine.UI.Image>();
        if (wheelImage != null) wheelOriginalSprite = wheelImage.sprite;
        portraitOrigin = portrait.anchoredPosition;
        titleOrigin = titleRect.anchoredPosition;
        CancelAndReset();
    }
    public void Play(Action onCompleted)
    {
        CancelAndReset();
        completed = onCompleted;
        root.alpha = 1;
        root.blocksRaycasts = true;
        Apply(0);
        animation = Tween.Custom(0, duration, duration, Apply, ease: Ease.Linear, useUnscaledTime: true)
            .OnComplete(() => { var callback = completed; CancelAndReset(); callback?.Invoke(); });
    }
    private void Apply(float time)
    {
        if (bossImage != null && bossFrames != null && bossFrames.Length > 0)
            bossImage.sprite = bossFrames[Mathf.FloorToInt(time * 12) % bossFrames.Length];
        ApplyWheel(time);
        float enter = Mathf.SmoothStep(0, 1, Mathf.Clamp01(time / .5f));
        float exit = Mathf.Clamp01((time - 4) / .5f);
        background.alpha = 1 - exit;
        dialogue.alpha = enter * (1 - exit);
        title.alpha = enter * (1 - exit);
        titleRect.anchoredPosition = titleOrigin + Vector2.right * ((1 - enter) * 700 + exit * 180);
        portrait.anchoredPosition = portraitOrigin + Vector2.up * (Mathf.Sin(time * Mathf.PI * 2) * 18);
    }
    private void ApplyWheel(float time)
    {
        if (wheelImage == null) return;
        int count = wheelFrames == null ? 0 : wheelFrames.Length;
        long frame = (long)Mathf.FloorToInt(Mathf.Max(0f, time) * 12f);
        if (count > 0 && frame < (long)count * Mathf.Max(0, wheelRepeatCount))
            wheelImage.sprite = wheelFrames[(int)(frame % count)];
        else
            wheelImage.sprite = wheelFinalSprite != null ? wheelFinalSprite : wheelOriginalSprite;
    }

    public void CancelAndReset()
    {
        animation.Stop();
        completed = null;
        if (wheelImage != null) wheelImage.sprite = wheelOriginalSprite;
        if (root == null) return;
        root.alpha = 0;
        root.blocksRaycasts = false;
        portrait.anchoredPosition = portraitOrigin;
        titleRect.anchoredPosition = titleOrigin;
    }
    private void OnDisable() => CancelAndReset();
}
