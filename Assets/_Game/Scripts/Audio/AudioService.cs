using UnityEngine;
using UnityEngine.Audio;

public sealed class AudioService : MonoBehaviour
{
    public static AudioService Instance { get; private set; }
    public AudioMixerGroup musicGroup, sfxGroup, uiGroup;
    private readonly AudioSource[] voices = new AudioSource[24];
    private readonly AudioCue[] cues = new AudioCue[24];
    private readonly System.Collections.Generic.Dictionary<AudioCue, float> times = new();
    private readonly System.Collections.Generic.Dictionary<AudioCue, int> last = new();
    private readonly System.Collections.Generic.HashSet<AudioCue> warned = new();
    private AudioSource[] music;
    private int active;
    private float elapsed, duration;
    private float[] starts = new float[2];
    private float target;

    public static AudioService Ensure()
    {
        if (Instance == null)
        {
            var prefab = Resources.Load<AudioService>("GameAudioService");
            if (prefab != null) Instantiate(prefab);
            else new GameObject("Audio Service").AddComponent<AudioService>();
        }
        return Instance;
    }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        music = new[] { AddSource(), AddSource() };
        foreach (var source in music) source.loop = true;
        for (int i = 0; i < voices.Length; i++) voices[i] = AddSource();
    }
    private AudioSource AddSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0;
        return source;
    }
    public void Play(AudioCue cue)
    {
        if (cue == null) return;
        if (cue.clips == null || cue.clips.Length == 0 || System.Array.TrueForAll(cue.clips, clip => clip == null))
        {
            if (warned.Add(cue)) Debug.LogWarning("Audio cue has no clips: " + cue.name, cue);
            return;
        }
        if (times.TryGetValue(cue, out float time) && Time.unscaledTime - time < cue.cooldown) return;
        int count = 0, free = -1;
        for (int i = 0; i < voices.Length; i++)
            if (voices[i].isPlaying) { if (cues[i] == cue) count++; }
            else free = i;
        if (free < 0 || count >= cue.maximumVoices) return;
        int index = Random.Range(0, cue.clips.Length);
        if (cue.clips.Length > 1 && last.TryGetValue(cue, out int previous) && index == previous)
            index = (index + Random.Range(1, cue.clips.Length)) % cue.clips.Length;
        if (cue.clips[index] == null) return;
        last[cue] = index;
        times[cue] = Time.unscaledTime;
        cues[free] = cue;
        var source = voices[free];
        source.outputAudioMixerGroup = cue.ui ? uiGroup : sfxGroup;
        source.clip = cue.clips[index];
        source.volume = cue.volume;
        source.pitch = Mathf.Clamp(Random.Range(cue.pitch.x, cue.pitch.y), 0.1f, 3);
        source.Play();
    }
    public void PlayMusic(AudioClip clip, float volume = 1, float fadeSeconds = 1)
    {
        if (clip != null && music[active].clip == clip && music[active].isPlaying) return;
        active = 1 - active;
        music[active].Stop();
        music[active].clip = clip;
        music[active].volume = 0;
        music[active].outputAudioMixerGroup = musicGroup;
        if (clip != null) music[active].Play();
        BeginFade(clip == null ? 0 : volume, fadeSeconds);
    }
    public void StopMusic(float fadeSeconds = 1) { BeginFade(0, fadeSeconds); }
    private void BeginFade(float volume, float seconds)
    {
        for (int i = 0; i < 2; i++) starts[i] = music[i].volume;
        target = Mathf.Clamp01(volume);
        elapsed = 0;
        duration = Mathf.Max(0.001f, seconds);
    }
    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(duration, 0.001f));
        for (int i = 0; i < 2; i++)
        {
            music[i].volume = Mathf.Lerp(starts[i], i == active ? target : 0, t);
            if (t == 1 && music[i].volume == 0) music[i].Stop();
        }
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
}
