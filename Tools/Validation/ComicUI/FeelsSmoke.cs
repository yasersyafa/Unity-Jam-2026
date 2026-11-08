using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
[InitializeOnLoad]
public static class FeelsSmoke
{
    static FeelsSmoke()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("FeelsSmoke", false))
            {
                SessionState.SetBool("FeelsSmoke", false);
                new GameObject("FeelsSmoke").AddComponent<FeelsSmokeDriver>();
            }
        };
        Application.logMessageReceived += (message, stack, type) =>
        {
            if (stack.Contains("UnityEditor.Search.SearchDatabase"))
            {
                System.IO.File.WriteAllText("/tmp/feels-editor-search-baseline.txt", message + "\n" + stack);
                return;
            }
            if (SessionState.GetBool("FeelsSmokeActive", false) && (type == LogType.Exception || type == LogType.Error || type == LogType.Assert))
            { System.IO.File.WriteAllText("/tmp/feels-exception.txt", message + "\n" + stack); Debug.Log("FEELS SMOKE FAILED: " + message + "\n" + stack); EditorApplication.Exit(1); }
        };
    }
    public static void Run()
    {
        SessionState.SetBool("FeelsSmokeActive", false);
        EditorSettings.enterPlayModeOptionsEnabled = false;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ComicUiAuthoring.Configure();
        EditorSceneManager.OpenScene("Assets/Scenes/SlotDemo.unity");
        SessionState.SetBool("FeelsSmoke", true); SessionState.SetBool("FeelsSmokeActive", true);
        EditorApplication.EnterPlaymode();
    }
}
