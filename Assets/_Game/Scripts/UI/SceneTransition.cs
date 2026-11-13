using System.Collections;
using PrimeTween;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Persistent halftone-wipe scene loader. Call <see cref="Load"/> for every scene change.</summary>
public sealed class SceneTransition : MonoBehaviour
{
    private const string MaterialPath = "SceneTransitionHalftone";
    private static readonly int ProgressId = Shader.PropertyToID("_Progress");
    private static readonly int AngleId = Shader.PropertyToID("_Angle");

    [SerializeField, Min(0.01f)] private float coverDuration = 0.45f;
    [SerializeField, Min(0.01f)] private float revealDuration = 0.45f;
    [SerializeField] private float sweepAngle = 30f;

    private static SceneTransition instance;
    private Image overlay;
    private Material material;
    private bool busy;

    public static bool IsTransitioning => instance != null && instance.busy;

    public static void Load(string sceneName)
    {
        if (instance == null && !Create())
        {
            SceneManager.LoadScene(sceneName);
            return;
        }
        if (instance.busy) return;
        instance.StartCoroutine(instance.Run(sceneName));
    }

    private static bool Create()
    {
        var source = Resources.Load<Material>(MaterialPath);
        if (source == null)
        {
            Debug.LogWarning($"SceneTransition: material Resources/{MaterialPath} not found, loading without transition.");
            return false;
        }

        var root = new GameObject("Scene Transition", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        DontDestroyOnLoad(root);
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        var image = new GameObject("Halftone Wipe", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        var rect = image.rectTransform;
        rect.SetParent(root.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;

        instance = root.AddComponent<SceneTransition>();
        instance.overlay = image;
        instance.material = new Material(source);
        image.material = instance.material;
        instance.SetCovered(false);
        return true;
    }

    private IEnumerator Run(string sceneName)
    {
        busy = true;
        overlay.enabled = true;
        overlay.raycastTarget = true; // blocks clicks while the screen changes

        material.SetFloat(AngleId, sweepAngle);
        yield return Tween.Custom(material, 0f, 1f, coverDuration, (m, value) => m.SetFloat(ProgressId, value),
            Ease.InQuad, useUnscaledTime: true).ToYieldInstruction();

        var load = SceneManager.LoadSceneAsync(sceneName);
        if (load == null)
        {
            Debug.LogError($"SceneTransition: scene '{sceneName}' is not in Build Settings.");
        }
        else
        {
            while (!load.isDone) yield return null;
        }
        yield return null; // let the new scene render one frame behind the cover

        // Reverse the sweep so the dots clear from the side they started covering.
        material.SetFloat(AngleId, sweepAngle + 180f);
        yield return Tween.Custom(material, 1f, 0f, revealDuration, (m, value) => m.SetFloat(ProgressId, value),
            Ease.OutQuad, useUnscaledTime: true).ToYieldInstruction();

        SetCovered(false);
        busy = false;
    }

    private void SetCovered(bool covered)
    {
        material.SetFloat(ProgressId, covered ? 1f : 0f);
        overlay.raycastTarget = covered;
        overlay.enabled = covered;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (material != null) Destroy(material);
    }
}
