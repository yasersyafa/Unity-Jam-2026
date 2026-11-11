using System;
using PrimeTween;
using UnityEngine;

/// <summary>
/// Comic bullseye used by the tutorial. Shootable targets are <see cref="IDamageable"/>, so every damage type and the
/// style meter treat them like enemies; aim markers have no collider and are cleared by hovering the crosshair.
/// </summary>
[DisallowMultipleComponent]
public sealed class TutorialTarget : MonoBehaviour, IDamageable
{
    private static Sprite bullseye;

    private SpriteRenderer visual;
    private CircleCollider2D hitbox;
    private float health;
    private Vector3 origin;
    private Vector3 swing;
    private float swingSpeed;
    private float phase;
    private Vector3 baseScale;
    private Sequence animation;

    public bool IsCleared { get; private set; }
    public event Action<TutorialTarget> Cleared;

    public static TutorialTarget Create(Transform parent, Vector3 position, float worldDiameter, int layer,
        bool shootable, float health, Vector3 swing, float swingSpeed, int sortingLayerId)
    {
        var go = new GameObject(shootable ? "Tutorial Target" : "Tutorial Aim Marker");
        go.layer = layer;
        go.transform.SetParent(parent, true);
        go.transform.position = position;
        var target = go.AddComponent<TutorialTarget>();
        target.visual = go.AddComponent<SpriteRenderer>();
        target.visual.sprite = Bullseye;
        target.visual.sortingLayerID = sortingLayerId; // Same layer as enemies, above the background.
        target.visual.sortingOrder = 50;
        if (!shootable) target.visual.color = new Color(1f, 1f, 1f, 0.75f);
        if (shootable)
        {
            target.hitbox = go.AddComponent<CircleCollider2D>();
            target.hitbox.radius = 0.5f;
        }
        float spriteSize = Bullseye.bounds.size.x;
        target.baseScale = Vector3.one * (worldDiameter / spriteSize);
        target.health = Mathf.Max(0.01f, health);
        target.origin = position;
        target.swing = swing;
        target.swingSpeed = swingSpeed;
        target.phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        target.PlaySpawn();
        return target;
    }

    public void TakeDamage(float damage)
    {
        if (IsCleared || hitbox == null) return;
        health -= Mathf.Max(0f, damage);
        if (health <= 0f) Clear();
        else Flash();
    }

    /// <summary>Clears an aim marker when the crosshair passes over it.</summary>
    public void Touch()
    {
        if (!IsCleared) Clear();
    }

    private void Update()
    {
        if (IsCleared || swing == Vector3.zero) return;
        transform.position = origin + swing * Mathf.Sin(Time.time * swingSpeed + phase);
    }

    private void PlaySpawn()
    {
        transform.localScale = baseScale * 0.2f;
        var tilt = new Vector3(0f, 0f, UnityEngine.Random.Range(-25f, 25f));
        animation = Sequence.Create()
            .Chain(Tween.Scale(transform, baseScale, 0.25f, Ease.OutBack))
            .Group(Tween.LocalRotation(transform, tilt, Vector3.zero, 0.25f, Ease.OutBack));
    }

    private void Flash()
    {
        animation.Stop();
        transform.localScale = baseScale * 1.15f;
        animation = Sequence.Create()
            .Chain(Tween.Scale(transform, baseScale, 0.12f, Ease.OutQuad))
            .Group(Tween.Color(visual, new Color(1f, 0.9f, 0.2f), Color.white, 0.12f));
    }

    private void Clear()
    {
        IsCleared = true;
        if (hitbox != null) hitbox.enabled = false;
        Cleared?.Invoke(this);
        animation.Stop();
        animation = Sequence.Create()
            .Chain(Tween.Scale(transform, baseScale * 1.7f, 0.16f, Ease.OutQuad))
            .Group(Tween.Alpha(visual, 0f, 0.16f))
            .ChainCallback(this, target => Destroy(target.gameObject));
    }

    private void OnDestroy() { animation.Stop(); }

    /// <summary>Red and white rings with a thick black ink outline, generated once.</summary>
    private static Sprite Bullseye
    {
        get
        {
            if (bullseye != null) return bullseye;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Tutorial Bullseye", filterMode = FilterMode.Bilinear };
            var pixels = new Color32[size * size];
            var ink = new Color32(20, 16, 16, 255);
            var red = new Color32(232, 48, 40, 255);
            var white = new Color32(255, 248, 230, 255);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - size * 0.5f, dy = y + 0.5f - size * 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) / (size * 0.5f);
                Color32 c = r > 1f ? new Color32(0, 0, 0, 0)
                    : r > 0.88f ? ink
                    : r > 0.66f ? red
                    : r > 0.62f ? ink
                    : r > 0.42f ? white
                    : r > 0.38f ? ink
                    : r > 0.18f ? red
                    : r > 0.14f ? ink
                    : white;
                pixels[y * size + x] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            bullseye = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            bullseye.name = "Tutorial Bullseye";
            return bullseye;
        }
    }
}
