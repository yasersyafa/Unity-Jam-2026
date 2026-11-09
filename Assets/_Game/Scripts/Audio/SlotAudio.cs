using UnityEngine;

public sealed class SlotAudio : MonoBehaviour
{
    [SerializeField] private SlotReelView reel;
    [SerializeField] private SlotWeaponRewards rewards;
    [SerializeField] private AudioCue stopped, awarded;
    private void OnEnable()
    {
        if (reel != null) reel.Stopped += Stop;
        if (rewards != null) rewards.WeaponAwarded += Award;
    }
    private void OnDisable()
    {
        if (reel != null) reel.Stopped -= Stop;
        if (rewards != null) rewards.WeaponAwarded -= Award;
    }
    private void Stop(SlotReelView value) { AudioService.Ensure().Play(stopped); }
    private void Award(WeaponData value) { AudioService.Ensure().Play(awarded); }
}
