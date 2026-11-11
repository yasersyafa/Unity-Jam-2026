using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>Owns Canvas-space shot effects. It never applies damage or mutates weapon assets.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(100)] // Position streams after WeaponFeedback applies this frame's recoil and sway.
public sealed class WeaponShotPresentation : MonoBehaviour
{
    private const int MaximumEffects = 128;

    private sealed class Effect
    {
        public RectTransform Root;
        public WeaponEffectGraphic Body;
        public WeaponEffectGraphic Trail;
        public WeaponShotVisualProfile Profile;
        public Vector2 Start;
        public Vector2 End;
        public float Elapsed;
        public float Duration;
        public float Angle;
        public bool Projectile;
        public bool Fragment;
        public bool Impact;
    }

    private readonly List<Effect> active = new List<Effect>(MaximumEffects);
    private ObjectPool<Effect> pool;
    private RectTransform effectRoot;
    private RectTransform fallbackMuzzle;
    private RectTransform muzzle;
    private Canvas canvas;
    private WeaponShotVisualProfile profile;
    private Effect stream;
    private bool held;
    private Vector2 aimScreen;

    public int ActiveEffectCount => active.Count + (stream != null ? 1 : 0);
    public bool IsStreaming => stream != null;

    public void Initialize(Canvas hud, RectTransform fallback)
    {
        canvas = hud;
        fallbackMuzzle = fallback;
        effectRoot = (RectTransform)transform;
        pool = new ObjectPool<Effect>(Create, e => e.Root.gameObject.SetActive(true),
            e => e.Root.gameObject.SetActive(false), e => Destroy(e.Root.gameObject),
            collectionCheck: true, defaultCapacity: 16, maxSize: MaximumEffects);
    }

    public void SetWeapon(WeaponShotVisualProfile configuration, RectTransform nozzle)
    {
        ResetEffects();
        profile = configuration;
        muzzle = nozzle;
    }

    public void SetAim(Vector2 screenPoint) { aimScreen = screenPoint; }

    public void SetContinuousFire(bool value)
    {
        held = value;
        if (!held && stream != null)
        {
            pool.Release(stream);
            stream = null;
        }
    }

    public void PlayShot(in ShotVisualContext shot)
    {
        if (profile == null || !isActiveAndEnabled) return;
        Vector2 end = ToLocal(shot.DestinationScreen);
        switch (profile.Mode)
        {
            case WeaponShotVisualProfile.VisualMode.Projectile:
                Launch(MuzzlePosition(), end, shot.HasImpact); break;
            case WeaponShotVisualProfile.VisualMode.Stream:
                if (stream == null && held)
                {
                    stream = Rent();
                    if (stream != null) stream.Body.Configure(profile.Silhouette, profile.Sprite, profile.Color);
                }
                break;
            default:
                if (shot.HasImpact) Impact(profile, end);
                break;
        }
    }

    private Effect Create()
    {
        var root = new GameObject("Shot Effect", typeof(RectTransform));
        root.SetActive(false);
        root.transform.SetParent(effectRoot, false);
        return new Effect { Root = (RectTransform)root.transform, Body = Graphic("Body", root.transform), Trail = Graphic("Trail", root.transform) };
    }

    private static WeaponEffectGraphic Graphic(string objectName, Transform parent)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(WeaponEffectGraphic));
        go.transform.SetParent(parent, false);
        return go.GetComponent<WeaponEffectGraphic>();
    }

    private Effect Rent()
    {
        if (ActiveEffectCount >= MaximumEffects) return null;
        Effect effect = pool.Get();
        effect.Root.anchoredPosition = Vector2.zero;
        effect.Root.localRotation = Quaternion.identity;
        effect.Root.localScale = Vector3.one;
        effect.Body.rectTransform.localRotation = Quaternion.identity;
        effect.Body.rectTransform.anchoredPosition = Vector2.zero;
        effect.Body.rectTransform.localScale = Vector3.one;
        effect.Trail.enabled = false;
        effect.Elapsed = 0f;
        effect.Projectile = false;
        effect.Fragment = false;
        effect.Impact = false;
        return effect;
    }

    private void Launch(Vector2 start, Vector2 end, bool impact)
    {
        Effect effect = Rent();
        if (effect == null) return;
        effect.Profile = profile;
        effect.Start = start;
        effect.End = end;
        effect.Duration = Mathf.Max(0.01f, Vector2.Distance(start, end) / profile.Speed);
        effect.Angle = profile.OrientToTravel ? Angle(end - start) : 0f;
        effect.Projectile = true;
        effect.Impact = impact;
        effect.Root.anchoredPosition = start;
        effect.Body.Configure(profile.Silhouette, profile.Sprite, profile.Color);
        effect.Body.rectTransform.sizeDelta = profile.Size;
        effect.Body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, effect.Angle);
        if (profile.TrailColor.a > 0f && profile.TrailLength > 0f)
        {
            effect.Trail.enabled = true;
            effect.Trail.Configure(WeaponShotVisualProfile.Shape.Streak, null, profile.TrailColor);
            effect.Trail.rectTransform.sizeDelta = new Vector2(profile.TrailLength, profile.TrailWidth);
            effect.Trail.rectTransform.anchoredPosition = -(end - start).normalized * profile.TrailLength * 0.5f;
            effect.Trail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Angle(end - start));
            effect.Trail.transform.SetAsFirstSibling();
        }
        active.Add(effect);
    }

    private void Impact(WeaponShotVisualProfile configuration, Vector2 position)
    {
        Effect effect = Rent();
        if (effect == null) return;
        effect.Profile = configuration;
        effect.Start = effect.End = position;
        effect.Duration = configuration.ImpactDuration;
        effect.Root.anchoredPosition = position;
        effect.Body.Configure(configuration.ImpactShape, configuration.ImpactSprite, configuration.ImpactColor);
        effect.Body.rectTransform.sizeDelta = Vector2.one * configuration.ImpactSize;
        active.Add(effect);
        for (int i = 0; i < configuration.FragmentCount; i++)
        {
            Effect fragment = Rent();
            if (fragment == null) break;
            float angle = 360f * i / configuration.FragmentCount;
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            fragment.Profile = configuration;
            fragment.Start = position;
            fragment.End = position + direction * configuration.FragmentDistance;
            fragment.Duration = configuration.ImpactDuration;
            fragment.Fragment = true;
            fragment.Root.anchoredPosition = position;
            fragment.Body.Configure(WeaponShotVisualProfile.Shape.Spike, null, configuration.Color);
            fragment.Body.rectTransform.sizeDelta = new Vector2(23f, 9f);
            fragment.Body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
            active.Add(fragment);
        }
    }

    private void Update()
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            Effect effect = active[i];
            effect.Elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(effect.Elapsed / effect.Duration);
            effect.Root.anchoredPosition = Vector2.Lerp(effect.Start, effect.End, progress);
            if (effect.Projectile)
                effect.Body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, effect.Angle + effect.Profile.Spin * effect.Elapsed);
            else
            {
                Color tint = effect.Fragment ? effect.Profile.Color : effect.Profile.ImpactColor;
                tint.a *= 1f - progress;
                effect.Body.color = tint;
                if (!effect.Fragment) effect.Body.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.2f, progress);
            }
            if (progress < 1f) continue;
            bool impact = effect.Projectile && effect.Impact;
            WeaponShotVisualProfile configuration = effect.Profile;
            Vector2 end = effect.End;
            active.RemoveAt(i);
            pool.Release(effect);
            if (impact) Impact(configuration, end);
        }
    }

    private void LateUpdate()
    {
        if (stream == null) return;
        Vector2 start = MuzzlePosition();
        Vector2 direction = ToLocal(aimScreen) - start;
        float length = Mathf.Min(direction.magnitude, profile.StreamLength);
        stream.Root.anchoredPosition = start + direction.normalized * length * 0.5f;
        stream.Body.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Angle(direction));
        float pulse = 1f + Mathf.Sin(Time.time * 36f) * 0.08f;
        stream.Body.rectTransform.sizeDelta = new Vector2(length, profile.StreamWidth * pulse);
    }

    private Vector2 MuzzlePosition()
    {
        Vector3 world = muzzle != null ? muzzle.position : fallbackMuzzle.TransformPoint(
            new Vector3(fallbackMuzzle.rect.center.x, fallbackMuzzle.rect.yMax, 0f));
        return effectRoot.InverseTransformPoint(world);
    }

    private Vector2 ToLocal(Vector2 screen)
    {
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRoot, screen, uiCamera, out Vector2 local);
        return local;
    }

    private static float Angle(Vector2 direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

    public void ResetEffects()
    {
        SetContinuousFire(false);
        foreach (Effect effect in active) pool.Release(effect);
        active.Clear();
    }

    private void OnDisable() { ResetEffects(); }
    private void OnDestroy() { pool?.Clear(); }
}
