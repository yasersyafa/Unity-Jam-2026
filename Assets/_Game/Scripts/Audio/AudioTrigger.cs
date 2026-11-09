using UnityEngine;

public sealed class AudioTrigger : MonoBehaviour
{
    [SerializeField] private AudioCue cue;
    public void Play() { AudioService.Ensure().Play(cue); }
}
