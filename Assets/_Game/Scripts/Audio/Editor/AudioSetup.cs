using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class AudioSetup
{
    private const string Root = "Assets/_Game/ScriptableObjects/Audio/";
    private static AudioCue Cue(string name, bool ui = false)
    {
        string path = Root + name + ".asset";
        var cue = AssetDatabase.LoadAssetAtPath<AudioCue>(path);
        if (cue != null) return cue;
        cue = ScriptableObject.CreateInstance<AudioCue>();
        cue.ui = ui;
        AssetDatabase.CreateAsset(cue, path);
        return cue;
    }
    private static void Set(Object target, string field, Object value)
    {
        var serialized = new SerializedObject(target);
        serialized.FindProperty(field).objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(target);
    }
    private static T Add<T>(GameObject go) where T : Component
    {
        return go.GetComponent<T>() ?? go.AddComponent<T>();
    }
    private static void Wire(GameObject root)
    {
        foreach (var source in root.GetComponentsInChildren<ShootingController>(true))
        {
            var audio = Add<CombatAudio>(source.gameObject);
            Set(audio, "shooting", source);
            Set(audio, "shot", Cue("WeaponShot"));
        }
        foreach (var enemy in root.GetComponentsInChildren<Enemy>(true))
        {
            var audio = Add<CombatAudio>(enemy.gameObject);
            Set(audio, "enemy", enemy);
            Set(audio, "damaged", Cue("EnemyHit"));
            Set(audio, "died", Cue("EnemyDeath"));
        }
        foreach (var player in root.GetComponentsInChildren<PlayerHealth>(true))
        {
            var audio = Add<CombatAudio>(player.gameObject);
            Set(audio, "player", player);
            Set(audio, "damaged", Cue("PlayerHit"));
            Set(audio, "died", Cue("PlayerDeath"));
            var feedback = player.GetComponent<PlayerDamageFeedback>();
            if (feedback != null) Set(feedback, "damageSound", Cue("PlayerHit"));
        }
        foreach (var reel in root.GetComponentsInChildren<SlotReelView>(true))
        {
            var audio = Add<SlotAudio>(reel.gameObject);
            Set(audio, "reel", reel);
            Set(audio, "stopped", Cue("ReelStop", true));
        }
        foreach (var rewards in root.GetComponentsInChildren<SlotWeaponRewards>(true))
        {
            var audio = Add<SlotAudio>(rewards.gameObject);
            Set(audio, "rewards", rewards);
            Set(audio, "awarded", Cue("SlotReward", true));
        }
        foreach (var button in root.GetComponentsInChildren<Button>(true))
        {
            var audio = Add<AudioTrigger>(button.gameObject);
            Set(audio, "cue", Cue("ButtonClick", true));
            bool exists = Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                .Any(i => button.onClick.GetPersistentTarget(i) == audio && button.onClick.GetPersistentMethodName(i) == "Play");
            if (!exists) UnityEventTools.AddPersistentListener(button.onClick, audio.Play);
            EditorUtility.SetDirty(button);
        }
    }
    [MenuItem("Game/Audio/Install Audio Wiring")]
    public static void Install()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Exit Play Mode before audio setup.");
        for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)
                throw new System.InvalidOperationException("Save open scenes before audio setup.");
        var setup = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = PrefabUtility.LoadPrefabContents(path);
                try { Wire(root); PrefabUtility.SaveAsPrefabAsset(root, path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (string guid in AssetDatabase.FindAssets("t:WeaponData"))
            {
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid));
                if (weapon.ShotSound == null) Set(weapon, "shotSound", Cue("WeaponShot"));
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                var scene = EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));
                foreach (var root in scene.GetRootGameObjects()) Wire(root);
                if (!Object.FindAnyObjectByType<SceneMusic>()) new GameObject("Scene Music").AddComponent<SceneMusic>();
                EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }

    [MenuItem("Game/Audio/Wire Slot Stop Chord")]
    public static void WireSlotStopChord()
    {
        const string chordPath = Root + "SlotChord.asset";
        var chord = AssetDatabase.LoadAssetAtPath<SlotChordSettings>(chordPath);
        if (chord == null)
        {
            chord = ScriptableObject.CreateInstance<SlotChordSettings>();
            chord.Configure(SlotNoteRoot.C, SlotChordQuality.Major);
            AssetDatabase.CreateAsset(chord, chordPath);
            EditorUtility.SetDirty(chord);
        }

        var slot1Clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Game/Audio/SFX/Slot/Slot1.wav");
        if (slot1Clip == null)
            throw new System.InvalidOperationException("Slot1.wav not found at Assets/_Game/Audio/SFX/Slot/Slot1.wav.");

        string[] cuePaths =
        {
            Root + "SlotStop1.asset",
            Root + "SlotStop2.asset",
            Root + "SlotStop3.asset"
        };
        foreach (string cuePath in cuePaths)
        {
            var cue = AssetDatabase.LoadAssetAtPath<AudioCue>(cuePath);
            if (cue == null) throw new System.InvalidOperationException($"Missing cue asset: {cuePath}");
            cue.clips = new[] { slot1Clip };
            cue.pitch = Vector2.one;
            cue.ui = true;
            EditorUtility.SetDirty(cue);
        }

        const string rigPath = "Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab";
        var rigRoot = PrefabUtility.LoadPrefabContents(rigPath);
        try
        {
            string[] reelNames = { "Reel1", "Reel2", "Reel3" };
            var slotAudios = rigRoot.GetComponentsInChildren<SlotAudio>(true);
            for (int i = 0; i < reelNames.Length; i++)
            {
                var slotAudio = slotAudios.FirstOrDefault(component => component.gameObject.name == reelNames[i]);
                if (slotAudio == null)
                    throw new System.InvalidOperationException($"SlotAudio not found on {reelNames[i]} in {rigPath}.");

                var serialized = new SerializedObject(slotAudio);
                serialized.FindProperty("stopChord").objectReferenceValue = chord;
                serialized.FindProperty("chordNoteIndex").intValue = i;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(slotAudio);
            }
            PrefabUtility.SaveAsPrefabAsset(rigRoot, rigPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(rigRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
