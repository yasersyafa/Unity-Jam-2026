using UnityEngine;
using PrimeTween;

[DisallowMultipleComponent]
public sealed class EnemyFeedback : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private GameObject deathVfxPrefab;
    [SerializeField] private Transform vfxAnchor;
    [SerializeField, Min(0.01f)] private float hitVfxLifetime = 2f;
    [SerializeField, Min(0.01f)] private float deathVfxLifetime = 2f;
    [Header("Death Impact")]
    [Tooltip("Near-freeze of game time when the robot explodes (unscaled seconds).")]
    [SerializeField, Min(0f)] private float deathHitStop = 0.06f;
    [SerializeField, Min(0f)] private float deathShakeStrength = 0.1f;
    [SerializeField, Min(0f)] private float deathShakeDuration = 0.22f;
    private Vector3 scale;
    private Quaternion rotation;
    private Color color;
    private Tween animation;
    private bool dead;

    private void Awake()
    {
        if (visual == null) { enabled = false; return; }
        scale = visual.transform.localScale;
        rotation = visual.transform.localRotation;
        color = visual.color;
    }
    private void Start() { if (visual != null && !dead && !animation.isAlive) Idle(); }
    private void Idle()
    {
        animation = Tween.Custom(0f, 1f, 1f, t =>
        {
            if (visual != null) visual.transform.localScale = scale * (1f + Mathf.Sin(t * Mathf.PI * 2f) * 0.03f);
        }, cycles: -1);
    }
    public void PlayHit()
    {
        if (dead || visual == null || !isActiveAndEnabled) return;
        ResetVisual();
        Spawn(hitVfxPrefab, hitVfxLifetime);
        animation = Tween.Custom(0f, 1f, 0.18f, t =>
        {
            float pulse = Mathf.Sin(t * Mathf.PI);
            visual.transform.localScale = Vector3.Scale(scale, new Vector3(1f + pulse * 0.2f, 1f - pulse * 0.15f, 1f));
            visual.transform.localRotation = rotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * 8f);
            visual.color = Color.Lerp(Color.red, color, t);
        }).OnComplete(Idle);
    }
    public float PlayDeath()
    {
        if (dead || visual == null || !isActiveAndEnabled) return 0f;
        dead = true;
        ResetVisual();
        // Death VFX faces away from the camera so debris always flies "backwards" into the scene.
        Camera view = Camera.main;
        Spawn(deathVfxPrefab, deathVfxLifetime, view != null ? Quaternion.LookRotation(view.transform.forward) : (Quaternion?)null);
        HitStop.Play(deathHitStop);
        if (view != null && deathShakeStrength > 0f)
        {
            if (!view.TryGetComponent(out CombatCameraFeedback shake)) shake = view.gameObject.AddComponent<CombatCameraFeedback>();
            shake.Play(deathShakeStrength, deathShakeDuration);
        }
        // The VFX carries the explosion; the sprite only takes an electric tint and pops out.
        // SpriteRenderer color multiplies the sprite, so a cyan tint reads as a short-circuit flash.
        animation = Tween.Custom(0f, 1f, DeathDuration, t =>
        {
            float size = t < 0.4f ? Mathf.Lerp(1f, 1.15f, t / 0.4f) : Mathf.Lerp(1.15f, 0f, (t - 0.4f) / 0.6f);
            visual.transform.localScale = scale * size;
            visual.color = Color.Lerp(ElectricTint, color, t);
        });
        return DeathDuration;
    }
    private const float DeathDuration = 0.12f;
    private static readonly Color ElectricTint = new(0.45f, 0.95f, 1f, 1f);
    private void Spawn(GameObject prefab, float lifetime, Quaternion? rotation = null)
    {
        if (prefab == null) return;
        Transform anchor = vfxAnchor != null ? vfxAnchor : transform;
        VfxPool.Spawn(prefab, anchor.position, rotation ?? anchor.rotation, lifetime);
    }
    private void ResetVisual()
    {
        animation.Stop();
        if (visual == null) return;
        visual.transform.localScale = scale;
        visual.transform.localRotation = rotation;
        visual.color = color;
    }
    private void OnDisable() { ResetVisual(); }
}
