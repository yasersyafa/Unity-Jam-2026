using UnityEngine;

public sealed class SceneMusic : MonoBehaviour
{
    [SerializeField] private AudioClip clip;
    [SerializeField, Range(0, 1)] private float volume = 1;
    [SerializeField, Min(0)] private float fadeSeconds = 1;
    private void Start() { AudioService.Ensure().PlayMusic(clip, volume, fadeSeconds); }
}
