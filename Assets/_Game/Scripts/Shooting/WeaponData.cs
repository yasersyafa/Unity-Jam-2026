using UnityEngine;

[CreateAssetMenu(menuName = "Game/Weapon Data")]
public sealed class WeaponData : ScriptableObject
{
    [Header("Presentation")]
    [Tooltip("Sprite shown in the first-person weapon overlay.")]
    [SerializeField] private Sprite sprite;
    [Header("Shooting")]
    [Tooltip("Damage applied to the nearest target per shot.")]
    [SerializeField, Min(0f)] private float damage = 10f;
    [Tooltip("Maximum shots per second while holding Attack.")]
    [SerializeField, Min(0.1f)] private float fireRate = 6f;
    [Header("Recoil")]
    [Tooltip("Horizontal randomness and upward kick in reference-screen pixels per shot.")]
    [SerializeField] private Vector2 kick = new Vector2(8f, 24f);
    [Tooltip("Maximum accumulated recoil in reference-screen pixels.")]
    [SerializeField, Min(0f)] private float maximumRecoil = 140f;
    [Tooltip("Recoil recovery speed in reference-screen pixels per second.")]
    [SerializeField, Min(0f)] private float recovery = 180f;
    public Sprite Sprite => sprite;
    public float Damage => damage;
    public float ShotInterval => 1f / Mathf.Max(0.1f, fireRate);
    public Vector2 Kick => kick;
    public float MaximumRecoil => maximumRecoil;
    public float Recovery => recovery;
}
