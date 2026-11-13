using UnityEngine;
using PrimeTween;

[DisallowMultipleComponent]
public sealed class EnemyFeedback : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    [Tooltip("Other sprites (e.g. wings) that flash with the visual. They need the Game/Sprite Flash material.")]
    [SerializeField] private SpriteRenderer[] flashExtras = System.Array.Empty<SpriteRenderer>();
    [SerializeField] private GameObject hitVfxPrefab;
    [SerializeField] private GameObject deathVfxPrefab;
    [Tooltip("Comic BOOM! pop-up spawned on death. Its own ComicPop delay keeps it just behind the death smoke.")]
    [SerializeField] private GameObject boomPrefab;
    [SerializeField] private Transform vfxAnchor;
    [Tooltip("Seconds before the pooled instance is recycled; match the sprite animation length.")]
    [SerializeField, Min(0.01f)] private float hitVfxLifetime = 0.3f;
    [SerializeField, Min(0.01f)] private float deathVfxLifetime = 0.35f;
    [Tooltip("Must cover the BOOM pop-up delay plus its duration.")]
    [SerializeField, Min(0.01f)] private float boomLifetime = 0.85f;
    [Header("VFX Size")]
    [Tooltip("Size multiplier for the hit animation (1 = prefab size).")]
    [SerializeField, Min(0.01f)] private float hitVfxScale = 1f;
    [Tooltip("Size multiplier for the death smoke animation (1 = prefab size).")]
    [SerializeField, Min(0.01f)] private float deathVfxScale = 1f;
    [Tooltip("Size multiplier for the BOOM! pop-up (1 = prefab size).")]
    [SerializeField, Min(0.01f)] private float boomScale = 1f;
    [Header("Hit Impact")]
    [Tooltip("Short near-freeze on a non-lethal hit (unscaled seconds).")]
    [SerializeField, Min(0f)] private float hitStop = 0.025f;
    [Tooltip("Minimum unscaled seconds between hit-stops, shared by all enemies, so automatic fire never keeps time frozen.")]
    [SerializeField, Min(0f)] private float hitStopCooldown = 0.12f;
    [SerializeField, Min(0f)] private float hitShakeStrength = 0.035f;
    [SerializeField, Min(0f)] private float hitShakeDuration = 0.1f;
    [Header("Death Impact")]
    [Tooltip("Near-freeze of game time when the robot explodes (unscaled seconds).")]
    [SerializeField, Min(0f)] private float deathHitStop = 0.06f;
    [SerializeField, Min(0f)] private float deathShakeStrength = 0.1f;
    [SerializeField, Min(0f)] private float deathShakeDuration = 0.22f;
    private static readonly int FlashId = Shader.PropertyToID("_FlashAmount");
    private static float nextHitStopTime;
    // Unscaled time restarts each Play session; clear the shared cooldown even without a domain reload.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { nextHitStopTime = 0f; }
    private MaterialPropertyBlock block;
    private Vector3 scale;
    private Quaternion rotation;
    private Color color;
    private Tween animation;
    private bool dead;
    private bool wasHit;

    private void Awake()
    {
        if (visual == null) { enabled = false; return; }
        scale = visual.transform.localScale;
        rotation = visual.transform.localRotation;
        color = visual.color;
        block = new MaterialPropertyBlock();
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
        wasHit = true;
        Spawn(hitVfxPrefab, hitVfxLifetime, hitVfxScale, CameraFacing());
        if (Time.unscaledTime >= nextHitStopTime)
        {
            nextHitStopTime = Time.unscaledTime + hitStopCooldown;
            HitStop.Play(hitStop);
        }
        Shake(hitShakeStrength, hitShakeDuration);
        animation = Tween.Custom(0f, 1f, 0.18f, t =>
        {
            float pulse = Mathf.Sin(t * Mathf.PI);
            visual.transform.localScale = Vector3.Scale(scale, new Vector3(1f + pulse * 0.2f, 1f - pulse * 0.15f, 1f));
            visual.transform.localRotation = rotation * Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI * 4f) * (1f - t) * 8f);
            // Comic impact frame: solid white, then fade back to the sprite.
            SetFlash(t < 0.3f ? 1f : 1f - (t - 0.3f) / 0.7f);
        }).OnComplete(Idle);
    }
    public float PlayDeath()
    {
        if (dead || visual == null || !isActiveAndEnabled) return 0f;
        dead = true;
        ResetVisual();
        Quaternion? facing = CameraFacing();
        // A one-hit kill never played the hit animation, so show it together with the death ones.
        if (!wasHit) Spawn(hitVfxPrefab, hitVfxLifetime, hitVfxScale, facing);
        Spawn(deathVfxPrefab, deathVfxLifetime, deathVfxScale, facing);
        Spawn(boomPrefab, boomLifetime, boomScale, facing);
        HitStop.Play(deathHitStop);
        Shake(deathShakeStrength, deathShakeDuration);
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
    // Sprite VFX share the camera's rotation so they always read flat on screen.
    private static Quaternion? CameraFacing()
    {
        Camera view = Camera.main;
        return view != null ? view.transform.rotation : null;
    }
    private static void Shake(float strength, float duration)
    {
        Camera view = Camera.main;
        if (view == null || strength <= 0f) return;
        if (!view.TryGetComponent(out CombatCameraFeedback shake)) shake = view.gameObject.AddComponent<CombatCameraFeedback>();
        shake.Play(strength, duration);
    }
    private void Spawn(GameObject prefab, float lifetime, float scale, Quaternion? rotation = null)
    {
        if (prefab == null) return;
        Transform anchor = vfxAnchor != null ? vfxAnchor : transform;
        VfxPool.Spawn(prefab, anchor.position, rotation ?? anchor.rotation, lifetime, scale);
    }
    private void SetFlash(float amount)
    {
        SetFlash(visual, amount);
        foreach (var extra in flashExtras) if (extra != null) SetFlash(extra, amount);
    }
    private void SetFlash(SpriteRenderer target, float amount)
    {
        target.GetPropertyBlock(block);
        block.SetFloat(FlashId, amount);
        target.SetPropertyBlock(block);
    }
    private void ResetVisual()
    {
        animation.Stop();
        if (visual == null) return;
        visual.transform.localScale = scale;
        visual.transform.localRotation = rotation;
        visual.color = color;
        if (block != null) SetFlash(0f);
    }
    private void OnDisable() { ResetVisual(); }
}
