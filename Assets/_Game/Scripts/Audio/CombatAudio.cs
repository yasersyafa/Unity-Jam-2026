using UnityEngine;

public sealed class CombatAudio : MonoBehaviour
{
    [SerializeField] private ShootingController shooting;
    [SerializeField] private Enemy enemy;
    [SerializeField] private PlayerHealth player;
    [SerializeField] private AudioCue shot, damaged, died;
    private AudioCue activeLoopCue;

    private void OnEnable()
    {
        if (shooting != null)
        {
            shooting.ShotFired += Shot;
            shooting.ContinuousFireChanged += ContinuousFire;
            shooting.WeaponEquipped += WeaponChanged;
        }
        if (enemy != null) { enemy.Damaged += EnemyDamage; enemy.Died += Death; }
        if (player != null) { if (player.GetComponent<PlayerDamageFeedback>() == null) player.Damaged += PlayerDamage; player.Died += Death; }
    }
    private void OnDisable()
    {
        if (shooting != null)
        {
            shooting.ShotFired -= Shot;
            shooting.ContinuousFireChanged -= ContinuousFire;
            shooting.WeaponEquipped -= WeaponChanged;
        }
        if (enemy != null) { enemy.Damaged -= EnemyDamage; enemy.Died -= Death; }
        if (player != null) { player.Damaged -= PlayerDamage; player.Died -= Death; }
        StopActiveLoop();
    }
    private AudioCue CurrentShotCue()
        => shooting != null && shooting.CurrentWeapon != null && shooting.CurrentWeapon.ShotSound != null
            ? shooting.CurrentWeapon.ShotSound
            : shot;
    private void Shot()
    {
        var cue = CurrentShotCue();
        if (cue != null && cue.loop) return;
        AudioService.Ensure().Play(cue);
    }
    private void ContinuousFire(bool active)
    {
        if (!active) { StopActiveLoop(); return; }
        var cue = CurrentShotCue();
        if (cue == null || !cue.loop) { StopActiveLoop(); return; }
        activeLoopCue = cue;
        AudioService.Ensure().StartLoop(cue);
    }
    private void WeaponChanged(WeaponData _) { StopActiveLoop(); }
    private void StopActiveLoop()
    {
        if (activeLoopCue == null) return;
        if (AudioService.Instance != null) AudioService.Instance.StopLoop(activeLoopCue);
        activeLoopCue = null;
    }
    private void EnemyDamage()
    {
        if (DamageType.SuppressEnemyHitSound) return;
        AudioService.Ensure().Play(damaged);
    }
    private void Damage() { AudioService.Ensure().Play(damaged); }
    private void PlayerDamage(float amount) { Damage(); }
    private void Death() { AudioService.Ensure().Play(died); }
}
