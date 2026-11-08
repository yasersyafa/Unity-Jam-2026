using UnityEngine;

[CreateAssetMenu(menuName = "Game/UI/Combat HUD Animation Profile")]
public sealed class CombatHudAnimationProfile : ScriptableObject
{
    [Header("Health")]
    [SerializeField] private bool animateHealth = true;
    [SerializeField, Min(0.01f)] private float fillDuration = 0.12f;
    [SerializeField, Min(0f)] private float trailDelay = 0.15f;
    [SerializeField, Min(0.01f)] private float trailDuration = 0.35f;
    [SerializeField] private Color trailColor = new Color(1f, 0.75f, 0.2f);
    [SerializeField, Min(0f)] private float healthShakePixels = 5f;
    [SerializeField, Min(0.01f)] private float healthImpactDuration = 0.16f;
    [SerializeField, Range(1f, 1.2f)] private float healthTextScale = 1.08f;
    [Header("Critical Accents")]
    [SerializeField] private bool animateCritical = true;
    [SerializeField, Range(0f, 1f)] private float criticalHealthRatio = 0.25f;
    [SerializeField, Range(0f, 1f)] private float criticalWeaponRatio = 0.2f;
    [SerializeField, Min(0.01f)] private float pulsePeriod = 0.7f;
    [Header("Weapon")]
    [SerializeField] private bool animateEquip = true;
    [SerializeField, Min(0.01f)] private float equipDuration = 0.18f;
    [SerializeField, Min(0f)] private float equipOffset = 30f;
    [Header("Wave Banner")]
    [SerializeField] private bool animateBanner = true;
    [SerializeField, Min(0.01f)] private float bannerEnter = 0.18f;
    [SerializeField, Min(0f)] private float bannerHold = 0.6f;
    [SerializeField, Min(0.01f)] private float bannerExit = 0.15f;
    public bool AnimateHealth => animateHealth;
    public float FillDuration => Mathf.Max(0.01f, fillDuration);
    public float TrailDelay => Mathf.Max(0f, trailDelay);
    public float TrailDuration => Mathf.Max(0.01f, trailDuration);
    public Color TrailColor => trailColor;
    public float HealthShakePixels => healthShakePixels;
    public float HealthImpactDuration => Mathf.Max(0.01f, healthImpactDuration);
    public float HealthTextScale => healthTextScale;
    public bool AnimateCritical => animateCritical;
    public float CriticalHealthRatio => criticalHealthRatio;
    public float CriticalWeaponRatio => criticalWeaponRatio;
    public float PulsePeriod => Mathf.Max(0.01f, pulsePeriod);
    public bool AnimateEquip => animateEquip;
    public float EquipDuration => Mathf.Max(0.01f, equipDuration);
    public float EquipOffset => equipOffset;
    public bool AnimateBanner => animateBanner;
    public float BannerEnter => Mathf.Max(0.01f, bannerEnter);
    public float BannerHold => Mathf.Max(0f, bannerHold);
    public float BannerExit => Mathf.Max(0.01f, bannerExit);
}
