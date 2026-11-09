using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AudioCue))]
public sealed class AudioCueEditor : Editor
{
    private GameObject preview;
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        if (GUILayout.Button("Preview"))
        {
            Stop();
            var cue = (AudioCue)target;
            if (cue.clips == null || cue.clips.Length == 0 || cue.clips[0] == null) return;
            preview = EditorUtility.CreateGameObjectWithHideFlags("Audio Preview", HideFlags.HideAndDontSave, typeof(AudioSource));
            var source = preview.GetComponent<AudioSource>();
            source.clip = cue.clips[0];
            source.volume = cue.volume;
            source.Play();
        }
        if (GUILayout.Button("Stop")) Stop();
    }
    private void OnEnable() { EditorApplication.playModeStateChanged += Changed; EditorApplication.update += Tick; }
    private void OnDisable() { EditorApplication.playModeStateChanged -= Changed; EditorApplication.update -= Tick; Stop(); }
    private void Tick() { if (preview != null && !preview.GetComponent<AudioSource>().isPlaying) Stop(); }
    private void Changed(PlayModeStateChange state) { Stop(); }
    private void Stop() { if (preview != null) DestroyImmediate(preview); }
}
