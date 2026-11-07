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
        Spawn(deathVfxPrefab, deathVfxLifetime);
        animation = Tween.Custom(0f, 1f, 0.35f, t =>
        {
            float size = t < 0.25f ? Mathf.Lerp(1f, 1.2f, t / 0.25f) : Mathf.Lerp(1.2f, 0f, (t - 0.25f) / 0.75f);
            visual.transform.localScale = scale * size;
            Color faded = color;
            faded.a *= 1f - t;
            visual.color = faded;
        });
        return 0.35f;
    }
    private void Spawn(GameObject prefab, float lifetime)
    {
        if (prefab == null) return;
        Transform anchor = vfxAnchor != null ? vfxAnchor : transform;
        Destroy(Instantiate(prefab, anchor.position, anchor.rotation), Mathf.Max(0.01f, lifetime));
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
