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
}
