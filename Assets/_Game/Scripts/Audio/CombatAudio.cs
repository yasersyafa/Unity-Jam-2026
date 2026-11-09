using UnityEngine;

public sealed class CombatAudio : MonoBehaviour
{
    [SerializeField] private ShootingController shooting;
    [SerializeField] private Enemy enemy;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private AudioCue shot, damaged, died;
    private void OnEnable()
    {
        if (shooting != null) shooting.ShotFired += Shot;
        if (enemy != null) { enemy.Damaged += Damage; enemy.Died += Death; }
        if (player != null) { if (player.GetComponent<PlayerDamageFeedback>() == null) player.Damaged += PlayerDamage; player.Died += Death; }
    }
    private void OnDisable()
    {
        if (shooting != null) shooting.ShotFired -= Shot;
        if (enemy != null) { enemy.Damaged -= Damage; enemy.Died -= Death; }
        if (player != null) { player.Damaged -= PlayerDamage; player.Died -= Death; }
    }
    private void Shot() { AudioService.Ensure().Play(shooting != null && shooting.CurrentWeapon != null && shooting.CurrentWeapon.ShotSound != null ? shooting.CurrentWeapon.ShotSound : shot); }
    private void Damage() { AudioService.Ensure().Play(damaged); }
    private void PlayerDamage(float amount) { Damage(); }
    private void Death() { AudioService.Ensure().Play(died); }
}
