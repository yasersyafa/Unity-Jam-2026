using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon Data")]
public sealed class WeaponData : ScriptableObject
{
    public enum FireMode
    {
        Automatic = 0,
        SemiAutomatic = 1
    }

    [Header("Slot Reward")]
    [SerializeField] private SlotTier tier = SlotTier.Common;
    public SlotTier Tier => tier;

    [Header("Ammo")]
    [Tooltip("Never runs out. When off, the player cannot attack once ammo reaches zero until a new weapon is spun.")]
    [SerializeField] private bool unlimitedAmmo;
    [Tooltip("Shots available when this weapon is equipped.")]
    [SerializeField, Min(1)] private int maxAmmo = 30;
    public bool UnlimitedAmmo => unlimitedAmmo;
    public int MaxAmmo => Mathf.Max(1, maxAmmo);

    [Header("Presentation")]
    [Tooltip("Sprite shown in the first-person weapon overlay.")]
    [SerializeField] private Sprite sprite;
    [Tooltip("Optional UI visual prefab with its own muzzle and effects. Empty uses the legacy sprite.")]
    [SerializeField] private WeaponVisual visualPrefab;
    public WeaponVisual VisualPrefab => visualPrefab;
    [Tooltip("Projectile mode locks the hit target and delivers damage on arrival. Hitscan and Stream resolve damage immediately.")]
    [SerializeField] private WeaponShotVisualProfile shotVisualProfile;
    public WeaponShotVisualProfile ShotVisualProfile => shotVisualProfile;
    [Header("Comic Sticker")]
    [SerializeField] private Sprite stickerSprite;
    [SerializeField, TextArea] private string stickerText;
    public Sprite StickerSprite => stickerSprite;
    public string StickerText => stickerText;
    [Header("Audio")]
    [SerializeField] private AudioCue shotSound;
    public AudioCue ShotSound => shotSound;

    [Header("Shooting")]
    [Tooltip("Automatic fires while Attack is held. Semi Automatic fires once per press; presses during cooldown are ignored.")]
    [SerializeField] private FireMode firingMode = FireMode.Automatic;
    [Tooltip("Base damage per shot; the damage type decides which targets receive it.")]
    [SerializeField, Min(0f)] private float damage = 10f;
    [Tooltip("How a shot picks its targets (single, piercing, explosion, cone...). Empty = single target.")]
    [SerializeField] private DamageType damageType;
    [Tooltip("Maximum shots per second for either fire mode. Also limits rapid manual clicks.")]
    [SerializeField, Min(0.1f)] private float fireRate = 6f;
    [Header("Recoil")]
    [Tooltip("Enable aim displacement, weapon kick and camera shake for this weapon. Hit feedback remains active when disabled.")]
    [SerializeField] private bool enableRecoil = true;
    [Tooltip("Upward aim displacement in reference-screen pixels per shot. Recoil never moves the aim horizontally.")]
    [SerializeField, Min(0f)] private float upwardKick = 24f;
    [Tooltip("Maximum accumulated recoil in reference-screen pixels.")]
    [SerializeField, Min(0f)] private float maximumRecoil = 140f;
    [Tooltip("Recoil recovery speed in reference-screen pixels per second.")]
    [SerializeField, Min(0f)] private float recovery = 180f;
    public Sprite Sprite => sprite;
    public FireMode FiringMode => firingMode;
    public bool EnableRecoil => enableRecoil;
    public float Damage => damage;
    public DamageType DamageType => damageType != null ? damageType : DamageType.Default;
    public float ShotInterval => 1f / Mathf.Max(0.1f, fireRate);
    public float UpwardKick => Mathf.Max(0f, upwardKick);
    public float MaximumRecoil => maximumRecoil;
    public float Recovery => recovery;
}
