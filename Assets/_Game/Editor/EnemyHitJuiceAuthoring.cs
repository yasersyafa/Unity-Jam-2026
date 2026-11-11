using System.Linq;
using GabrielBigardi.SpriteAnimator;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds the enemy hit juice assets and wires them into every enemy prefab: the "VFX" sorting layer, the hit / death
/// sprite-animation prefabs, the BOOM! pop-up prefab, and the white-flash sprite material. Safe to run again.
/// </summary>
public static class EnemyHitJuiceAuthoring
{
    private const string VfxLayer = "VFX";
    private const string ShaderName = "Game/Sprite Flash";
    private const string FlashMaterialPath = "Assets/_Game/Prefabs/VFX/EnemySpriteFlash.mat";
    private const string SpriteMaterialGuid = "9dfc825aed78fcd4ba02077103263b40"; // URP Sprite-Unlit-Default
    private const string HitAnimationPath = "Assets/_Game/Animations/EnemyHitParticle.asset";
    private const string DeathAnimationPath = "Assets/_Game/Animations/EnemyDeathParticle.asset";
    private const string BoomSpritePath = "Assets/_Game/Arts/VFX/BoomDeathVFX.png";
    private const string HitPrefabPath = "Assets/_Game/Prefabs/VFX/EnemyHitParticle.prefab";
    private const string DeathPrefabPath = "Assets/_Game/Prefabs/VFX/EnemyDeathParticle.prefab";
    private const string BoomPrefabPath = "Assets/_Game/Prefabs/VFX/BoomDeathVFX.prefab";
    private const string EnemyFolder = "Assets/_Game/Prefabs/Enemies";
    // Sorting order inside the VFX layer: hit < death < boom.
    private const int HitOrder = 0, DeathOrder = 1, BoomOrder = 3;
    // World size of each effect's longest side; tweak the prefab's "Sprite" child scale afterwards if needed.
    private const float HitSize = 1.2f, DeathSize = 2.6f, BoomSize = 2.2f;
    private static readonly Color FlashColor = new(1f, 0.12f, 0.12f, 1f);
    private const float BoomDelay = 0.12f, BoomDuration = 0.7f;

    [MenuItem("Tools/VFX/Build Enemy Hit Juice")]
    public static void Build()
    {
        var shader = Shader.Find(ShaderName);
        if (shader == null) { Debug.LogError($"Shader '{ShaderName}' not found."); return; }
        var hitAnimation = AssetDatabase.LoadAssetAtPath<SpriteAnimationObject>(HitAnimationPath);
        var deathAnimation = AssetDatabase.LoadAssetAtPath<SpriteAnimationObject>(DeathAnimationPath);
        var boomSprite = AssetDatabase.LoadAllAssetsAtPath(BoomSpritePath).OfType<Sprite>().FirstOrDefault();
        if (hitAnimation == null || deathAnimation == null || boomSprite == null)
        {
            Debug.LogError("Missing hit/death sprite animation or BoomDeathVFX sprite.");
            return;
        }
        EnsureSortingLayer();
        var spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(SpriteMaterialGuid));
        var flash = AssetDatabase.LoadAssetAtPath<Material>(FlashMaterialPath);
        if (flash == null) { flash = new Material(shader); AssetDatabase.CreateAsset(flash, FlashMaterialPath); }
        flash.SetColor("_FlashColor", FlashColor);
        EditorUtility.SetDirty(flash);

        var hit = BuildAnimated("EnemyHitParticle", HitPrefabPath, hitAnimation, HitOrder, HitSize, spriteMaterial);
        var death = BuildAnimated("EnemyDeathParticle", DeathPrefabPath, deathAnimation, DeathOrder, DeathSize, spriteMaterial);
        var boom = BuildBoom(boomSprite, spriteMaterial);
        int assigned = AssignToEnemies(flash, hit, Duration(hitAnimation), death, Duration(deathAnimation), boom);
        AssetDatabase.SaveAssets();
        Debug.Log($"Enemy hit juice built; assigned to {assigned} enemy prefabs.", boom);
    }

    private static void EnsureSortingLayer()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("m_SortingLayers");
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == VfxLayer) return;
        // Appended last, so it draws above Enemy.
        layers.InsertArrayElementAtIndex(layers.arraySize);
        var layer = layers.GetArrayElementAtIndex(layers.arraySize - 1);
        layer.FindPropertyRelative("name").stringValue = VfxLayer;
        layer.FindPropertyRelative("uniqueID").uintValue = (uint)Random.Range(1, int.MaxValue);
        layer.FindPropertyRelative("locked").intValue = 0;
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject BuildAnimated(string name, string path, SpriteAnimationObject animation, int order, float size, Material material)
    {
        Sprite first = animation.SpriteAnimations[0].Frames[0];
        var root = new GameObject(name);
        try
        {
            var renderer = Child(root, first, order, size, material);
            var animator = renderer.gameObject.AddComponent<SpriteAnimator>();
            var settings = new SerializedObject(animator);
            settings.FindProperty("_playAutomatically").boolValue = true; // restarts every time VfxPool reactivates it
            settings.FindProperty("_spriteAnimationObject").objectReferenceValue = animation;
            settings.FindProperty("_spriteRenderer").objectReferenceValue = renderer;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static GameObject BuildBoom(Sprite sprite, Material material)
    {
        var root = new GameObject("BoomDeathVFX");
        try
        {
            // ComicPop animates the child locally; VfxPool positions the root.
            var renderer = Child(root, sprite, BoomOrder, BoomSize, material);
            renderer.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            var pop = root.AddComponent<ComicPop>();
            var settings = new SerializedObject(pop);
            settings.FindProperty("sprite").objectReferenceValue = renderer;
            settings.FindProperty("delay").floatValue = BoomDelay;
            settings.FindProperty("duration").floatValue = BoomDuration;
            settings.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, BoomPrefabPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    private static SpriteRenderer Child(GameObject root, Sprite sprite, int order, float size, Material material)
    {
        var child = new GameObject("Sprite");
        child.transform.SetParent(root.transform, false);
        Vector3 bounds = sprite.bounds.size;
        child.transform.localScale = Vector3.one * (size / Mathf.Max(0.01f, Mathf.Max(bounds.x, bounds.y)));
        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        if (material != null) renderer.sharedMaterial = material;
        renderer.sortingLayerName = VfxLayer;
        renderer.sortingOrder = order;
        return renderer;
    }

    private static float Duration(SpriteAnimationObject animation)
    {
        var clip = animation.SpriteAnimations[0];
        return clip.FPS > 0 ? (float)clip.Frames.Count / clip.FPS : 0.3f;
    }

    private static int AssignToEnemies(Material flash, GameObject hit, float hitLifetime, GameObject death, float deathLifetime, GameObject boom)
    {
        int count = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EnemyFolder }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var feedback = root.GetComponentInChildren<EnemyFeedback>(true);
                if (feedback == null) continue;
                var serialized = new SerializedObject(feedback);
                var visual = serialized.FindProperty("visual").objectReferenceValue as SpriteRenderer;
                var extras = serialized.FindProperty("flashExtras");
                extras.arraySize = 0;
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    renderer.sharedMaterial = flash;
                    if (renderer == visual) continue;
                    extras.InsertArrayElementAtIndex(extras.arraySize);
                    extras.GetArrayElementAtIndex(extras.arraySize - 1).objectReferenceValue = renderer;
                }
                serialized.FindProperty("hitVfxPrefab").objectReferenceValue = hit;
                serialized.FindProperty("deathVfxPrefab").objectReferenceValue = death;
                serialized.FindProperty("boomPrefab").objectReferenceValue = boom;
                serialized.FindProperty("hitVfxLifetime").floatValue = hitLifetime;
                serialized.FindProperty("deathVfxLifetime").floatValue = deathLifetime;
                serialized.FindProperty("boomLifetime").floatValue = BoomDelay + BoomDuration + 0.05f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
                count++;
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        return count;
    }
}
