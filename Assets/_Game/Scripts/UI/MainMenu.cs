using UnityEngine;

/// <summary>Main menu button actions, wired through Button.onClick.</summary>
public sealed class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "GameScene";

    public void StartGame() => SceneTransition.Load(gameScene);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
