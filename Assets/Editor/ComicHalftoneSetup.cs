using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Creates the Stage 2 material and applies it only to the selected halftone layer.</summary>
public static class ComicHalftoneSetup
{
    private const string MaterialPath = "Assets/Materials/Background/M_ComicHalftone.mat";

    [MenuItem("Tools/Comic Background/Create Halftone Material")]
    public static void CreateMaterial()
    {
        Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/Comic/ComicHalftone.shader");
        if (shader == null)
            throw new InvalidOperationException("Comic halftone shader has not imported yet.");

        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Background");
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader) { name = "M_ComicHalftone" };
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else if (material.shader != shader)
        {
            throw new InvalidOperationException("Existing halftone material uses a different shader; preserve it for manual review.");
        }

        AssetDatabase.SaveAssets();
        // Force a real pass compilation when a graphics device is available.
        bool passReady = material.SetPass(0);
        var errors = ShaderUtil.GetShaderMessages(shader)
            .Where(message => message.severity == UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error).ToArray();
        if (errors.Length > 0)
            throw new InvalidOperationException(string.Join("\n", errors.Select(error => error.message)));
        Debug.Log($"Comic halftone material ready. Pass ready: {passReady}. Shader errors: {errors.Length}.");
    }

    [MenuItem("Tools/Comic Background/Assign Material To Selected Halftone")]
    public static void AssignToSelectedHalftone()
    {
        GameObject selected = Selection.activeGameObject;
        if (EditorApplication.isPlayingOrWillChangePlaymode || selected == null ||
            !selected.scene.IsValid() || !selected.scene.isLoaded || EditorUtility.IsPersistent(selected) ||
            selected.transform.parent == null || selected.transform.parent.name != "BackgroundRoot" ||
            (selected.name != "02_Halftone" && selected.name != "L02_Halftone") ||
            PrefabUtility.IsPartOfAnyPrefab(selected))
        {
            Debug.LogError("Select the scene's non-prefab 02_Halftone or L02_Halftone child of BackgroundRoot, outside Play Mode.");
            return;
        }

        SpriteRenderer renderer = selected.GetComponent<SpriteRenderer>();
        if (renderer == null || renderer.sprite == null)
        {
            Debug.LogError("The selected layer requires a SpriteRenderer with an assigned sprite.");
            return;
        }

        CreateMaterial();
        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        Undo.RecordObject(material, "Configure Halftone Reference Width");
        float referenceWidth = renderer.drawMode == SpriteDrawMode.Simple
            ? renderer.sprite.bounds.size.x : renderer.size.x;
        material.SetFloat(Shader.PropertyToID("_PatternWidth"), Mathf.Max(referenceWidth, 0.0001f));
        EditorUtility.SetDirty(material);
        Undo.RecordObject(renderer, "Assign Comic Halftone Material");
        renderer.sharedMaterial = material;
        EditorSceneManager.MarkSceneDirty(selected.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Halftone assigned. Save the scene when ready. Original sprite and sorting are preserved.", selected);
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }
}
