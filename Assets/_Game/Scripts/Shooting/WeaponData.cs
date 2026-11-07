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
    [Tooltip("Usage duration in seconds. Stored for future expiration rules; no countdown is applied yet.")]
    [SerializeField, Min(0f)] private float usageDuration = 30f;
    public SlotTier Tier => tier;
    public float UsageDuration => Mathf.Max(0f, usageDuration);

    [Header("Presentation")]
    [Tooltip("Sprite shown in the first-person weapon overlay.")]
    [SerializeField] private Sprite sprite;
    [Header("Shooting")]
    [Tooltip("Automatic fires while Attack is held. Semi Automatic fires once per press; presses during cooldown are ignored.")]
    [SerializeField] private FireMode firingMode = FireMode.Automatic;
    [Tooltip("Damage applied to the nearest target per shot.")]
    [SerializeField, Min(0f)] private float damage = 10f;
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
    public float ShotInterval => 1f / Mathf.Max(0.1f, fireRate);
    public float UpwardKick => Mathf.Max(0f, upwardKick);
    public float MaximumRecoil => maximumRecoil;
    public float Recovery => recovery;
}
