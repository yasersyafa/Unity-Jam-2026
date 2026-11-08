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
    private void Awake() { position = visual.anchoredPosition; group.alpha = 0f; }
    private void OnEnable() { spawner.WaveStarted += Wave; spawner.BossReady += Boss; }
    private void OnDisable()
    {
        spawner.WaveStarted -= Wave; spawner.BossReady -= Boss;
        animation.Stop(); pending = showing = null;
        group.alpha = 0f; visual.anchoredPosition = position;
    }
    [ContextMenu("Preview Wave (Play Mode)")]
    private void Preview() { if (Application.isPlaying && isActiveAndEnabled) Wave(1); }
    private void Wave(int number) { pending = $"WAVE {number}"; }
    private void Boss() { pending = "BOSS!"; }
    private void Update()
    {
        if (player.IsDead) { animation.Stop(); group.alpha = 0f; pending = showing = null; return; }
        if (flow.IsOpen)
        {
            if (animation.isAlive) { animation.Stop(); if (pending == null) pending = showing; group.alpha = 0f; }
            return;
        }
        if (pending == null || profile == null) return;
        animation.Stop(); showing = pending; pending = null; label.text = showing;
        float total = profile.BannerEnter + profile.BannerHold + profile.BannerExit;
        animation = Tween.Custom(0f, total, total, elapsed =>
        {
            float enter = Mathf.Clamp01(elapsed / profile.BannerEnter);
            float exit = Mathf.Clamp01((elapsed - profile.BannerEnter - profile.BannerHold) / profile.BannerExit);
            group.alpha = enter * (1f - exit);
            visual.anchoredPosition = position + Vector2.up * (profile.AnimateBanner ? (1f - enter + exit) * 30f : 0f);
        }, ease: Ease.Linear, useUnscaledTime: true).OnComplete(() => { group.alpha = 0f; showing = null; visual.anchoredPosition = position; });
    }
}
