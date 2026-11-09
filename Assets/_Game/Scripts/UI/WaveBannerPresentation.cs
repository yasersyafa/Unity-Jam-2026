using PrimeTween;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class WaveBannerPresentation : MonoBehaviour
{
    [SerializeField] private EnemySpawner spawner;
    [SerializeField] private SlotGameplayFlow flow;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private CombatHudAnimationProfile profile;
    [SerializeField] private RectTransform visual;
    [SerializeField] private CanvasGroup group;
    [SerializeField] private TextMeshProUGUI label;
    private Tween animation;
    private Vector2 position;
    private string pending;
    private string showing;

    private void Awake()
    {
        if (visual != null) position = visual.anchoredPosition;
        if (group != null) group.alpha = 0f;
        if (flow == null) flow = FindFirstObjectByType<SlotGameplayFlow>();
        if (player == null) player = FindFirstObjectByType<PlayerHealth>();
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
    }

    private void OnEnable()
    {
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.WaveStarted += Wave;
            spawner.BossReady += Boss;
        }
    }

    private void OnDisable()
    {
        if (spawner != null)
        {
            spawner.WaveStarted -= Wave;
            spawner.BossReady -= Boss;
        }
        animation.Stop();
        pending = showing = null;
        if (group != null) group.alpha = 0f;
        if (visual != null) visual.anchoredPosition = position;
    }

    [ContextMenu("Preview Wave (Play Mode)")]
    private void Preview() { if (Application.isPlaying && isActiveAndEnabled) Wave(1); }
    private void Wave(int number) { pending = $"WAVE {number}"; }
    private void Boss() { pending = "BOSS!"; }

    private void Update()
    {
        if (player == null) player = FindFirstObjectByType<PlayerHealth>();
        if (flow == null) flow = FindFirstObjectByType<SlotGameplayFlow>();
        if (player != null && player.IsDead) { animation.Stop(); if (group != null) group.alpha = 0f; pending = showing = null; return; }
        if (flow != null && flow.IsOpen)
        {
            if (animation.isAlive) { animation.Stop(); if (pending == null) pending = showing; if (group != null) group.alpha = 0f; }
            return;
        }
        if (pending == null || profile == null || label == null || group == null || visual == null) return;
        animation.Stop();
        showing = pending;
        pending = null;
        label.text = showing;
        float total = profile.BannerEnter + profile.BannerHold + profile.BannerExit;
        animation = Tween.Custom(0f, total, total, elapsed =>
        {
            float enter = Mathf.Clamp01(elapsed / profile.BannerEnter);
            float exit = Mathf.Clamp01((elapsed - profile.BannerEnter - profile.BannerHold) / profile.BannerExit);
            group.alpha = enter * (1f - exit);
            visual.anchoredPosition = position + Vector2.up * (profile.AnimateBanner ? (1f - enter + exit) * 30f : 0f);
        }, ease: Ease.Linear, useUnscaledTime: true).OnComplete(() => { if (group != null) group.alpha = 0f; showing = null; if (visual != null) visual.anchoredPosition = position; });
    }
}
