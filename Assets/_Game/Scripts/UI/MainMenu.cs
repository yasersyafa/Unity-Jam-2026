using UnityEngine;

/// <summary>Main menu button actions, wired through Button.onClick.</summary>
public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "GameScene";
    [SerializeField] private string introScene = "IntroScene";

    public string StartScene => IntroCutsceneController.Completed ? gameScene : introScene;

    public void StartGame() => SceneTransition.Load(StartScene);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
