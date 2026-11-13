using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Authors and wires the DMC-style combo rank HUD inside GameplayRig.prefab.
/// </summary>
public static class StyleMeterUiAuthoring
{
    private const string GameplayRigPath = "Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab";
    private const string RankSheetPath = "Assets/_Game/Arts/UI/ComboRankSheets.png";
    private const string BackgroundPath = "Assets/_Game/Arts/UI/ComboBackgroung.png";
    private const string RankMaterialPath = "Assets/_Game/Arts/UI/ComboRankFill.mat";
    private const string FontPath = "Assets/_Game/Fonts/Bangers/Bangers-Regular SDF.asset";

    [MenuItem("Tools/Comic UI/Configure Style Rank HUD")]
    public static void ConfigureSavedRig()
    {
        var root = PrefabUtility.LoadPrefabContents(GameplayRigPath);
        try
        {
            ConfigureRigRoot(root);
            PrefabUtility.SaveAsPrefabAsset(root, GameplayRigPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("Style rank HUD authoring completed.");
    }

    private static void ConfigureRigRoot(GameObject root)
    {
        Transform canvasTransform = FindChildByName(root.transform, "CanvasHUD");
        if (canvasTransform == null)
            throw new InvalidOperationException("CanvasHUD not found inside GameplayRig.");

        Dictionary<string, Sprite> sprites = LoadSpritesByName(RankSheetPath);
        Sprite backgroundSprite = LoadRequired<Sprite>(BackgroundPath);
        Material rankMaterial = LoadRequired<Material>(RankMaterialPath);
        TMP_FontAsset font = LoadRequired<TMP_FontAsset>(FontPath);

        RectTransform hudRoot = EnsureRect("StyleRankHUD", canvasTransform);
        hudRoot.anchorMin = new Vector2(1f, 0.56f);
        hudRoot.anchorMax = new Vector2(1f, 0.56f);
        hudRoot.pivot = new Vector2(1f, 0.5f);
        hudRoot.anchoredPosition = new Vector2(-65f, 20f);
        hudRoot.sizeDelta = new Vector2(360f, 290f);
        hudRoot.localScale = Vector3.one;
        hudRoot.localRotation = Quaternion.identity;

        var group = GetOrAdd<CanvasGroup>(hudRoot.gameObject);
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        // Layer order: ComboBackground -> HitCountText -> RankVisual (draws rank emblem over banner edge).
        RectTransform backgroundRect = EnsureRect("ComboBackground", hudRoot);
        backgroundRect.anchorMin = new Vector2(1f, 0f);
        backgroundRect.anchorMax = new Vector2(1f, 0f);
        backgroundRect.pivot = new Vector2(1f, 0f);
        backgroundRect.anchoredPosition = new Vector2(0f, 28f);
        backgroundRect.sizeDelta = new Vector2(340f, 58f);
        backgroundRect.localScale = Vector3.one;
        backgroundRect.localEulerAngles = new Vector3(0f, 0f, -2.5f);
        backgroundRect.SetSiblingIndex(0);

        var backgroundImage = GetOrAdd<Image>(backgroundRect.gameObject);
        backgroundImage.sprite = backgroundSprite;
        backgroundImage.color = Color.white;
        backgroundImage.type = Image.Type.Simple;
        backgroundImage.preserveAspect = false;
        backgroundImage.raycastTarget = false;

        RectTransform taglineRect = EnsureRect("RankTagline", backgroundRect);
        Stretch(taglineRect, left: 22f, right: 18f, top: 2f, bottom: 0f);
        taglineRect.localScale = Vector3.one;
        taglineRect.localRotation = Quaternion.identity;

        var tagline = GetOrAdd<TextMeshProUGUI>(taglineRect.gameObject);
        tagline.font = font;
        tagline.fontSize = 31f;
        tagline.fontStyle = FontStyles.Italic;
        tagline.alignment = TextAlignmentOptions.MidlineRight;
        tagline.color = Color.white;
        tagline.textWrappingMode = TextWrappingModes.NoWrap;
        tagline.overflowMode = TextOverflowModes.Overflow;
        tagline.raycastTarget = false;
        tagline.text = "COME ON!";
        backgroundRect.gameObject.SetActive(false);
        taglineRect.gameObject.SetActive(false);

        RectTransform hitRect = EnsureRect("HitCountText", hudRoot);
        hitRect.anchorMin = new Vector2(1f, 0f);
        hitRect.anchorMax = new Vector2(1f, 0f);
        hitRect.pivot = new Vector2(0.5f, 0f);
        hitRect.anchoredPosition = new Vector2(-165f, 18f);
        hitRect.sizeDelta = new Vector2(240f, 38f);
        hitRect.localScale = Vector3.one;
        hitRect.localRotation = Quaternion.identity;
        hitRect.SetSiblingIndex(1);

        var hitText = GetOrAdd<TextMeshProUGUI>(hitRect.gameObject);
        hitText.font = font;
        hitText.fontSize = 28f;
        hitText.fontStyle = FontStyles.Italic;
        hitText.alignment = TextAlignmentOptions.Center;
        hitText.color = new Color(1f, 0.9f, 0.45f, 1f);
        hitText.textWrappingMode = TextWrappingModes.NoWrap;
        hitText.raycastTarget = false;
        hitText.text = "0 HITS";

        RectTransform rankVisualRect = EnsureRect("RankVisual", hudRoot);
        rankVisualRect.anchorMin = new Vector2(1f, 1f);
        rankVisualRect.anchorMax = new Vector2(1f, 1f);
        rankVisualRect.pivot = new Vector2(0.5f, 0.5f);
        rankVisualRect.anchoredPosition = new Vector2(-165f, -108f);
        rankVisualRect.sizeDelta = new Vector2(246f, 210f);
        rankVisualRect.localScale = Vector3.one;
        rankVisualRect.localRotation = Quaternion.identity;
        rankVisualRect.SetSiblingIndex(2);

        RectTransform rankImageRect = EnsureRect("RankImage", rankVisualRect);
        Stretch(rankImageRect, 0f, 0f, 0f, 0f);
        rankImageRect.localScale = Vector3.one;
        rankImageRect.localRotation = Quaternion.identity;

        var rankImage = GetOrAdd<Image>(rankImageRect.gameObject);
        rankImage.sprite = RequireSprite(sprites, "ComboRankSheets_1");
        rankImage.material = rankMaterial;
        rankImage.color = Color.white;
        rankImage.type = Image.Type.Simple;
        rankImage.preserveAspect = true;
        rankImage.raycastTarget = false;

        var rankVisuals = new[]
        {
            Visual(sprites, "ComboRankSheets_1", new Color(0.35f, 0.90f, 1.00f, 1f), 0.2465f, 0.7606f), // C
            Visual(sprites, "ComboRankSheets_2", new Color(0.65f, 1.00f, 0.26f, 1f), 0.2394f, 0.7606f), // B
            Visual(sprites, "ComboRankSheets_3", new Color(1.00f, 0.68f, 0.17f, 1f), 0.2465f, 0.7606f), // A
            Visual(sprites, "ComboRankSheets_0", new Color(1.00f, 0.25f, 0.51f, 1f), 0.2465f, 0.7817f), // S
            Visual(sprites, "ComboRankSheets_4", new Color(0.79f, 0.29f, 1.00f, 1f), 0.2887f, 0.7958f), // SS
            Visual(sprites, "ComboRankSheets_5", new Color(1.00f, 0.42f, 0.10f, 1f), 0.2887f, 0.7958f), // SSS
        };

        var ui = GetOrAdd<StyleMeterUI>(hudRoot.gameObject);
        var serialized = new SerializedObject(ui);
        serialized.FindProperty("player").objectReferenceValue = root.GetComponentInChildren<PlayerHealth>(true);
        serialized.FindProperty("group").objectReferenceValue = group;
        serialized.FindProperty("panelRoot").objectReferenceValue = hudRoot;
        serialized.FindProperty("rankVisual").objectReferenceValue = rankVisualRect;
        serialized.FindProperty("rankImage").objectReferenceValue = rankImage;
        serialized.FindProperty("rankMaterial").objectReferenceValue = rankMaterial;
        serialized.FindProperty("showTagline").boolValue = false;
        serialized.FindProperty("taglineLabel").objectReferenceValue = tagline;
        serialized.FindProperty("hitCountLabel").objectReferenceValue = hitText;

        SerializedProperty array = serialized.FindProperty("rankVisuals");
        array.arraySize = rankVisuals.Length;
        for (int i = 0; i < rankVisuals.Length; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);
            element.FindPropertyRelative("sprite").objectReferenceValue = rankVisuals[i].sprite;
            element.FindPropertyRelative("accentColor").colorValue = rankVisuals[i].accentColor;
            element.FindPropertyRelative("uvBottom").floatValue = rankVisuals[i].uvBottom;
            element.FindPropertyRelative("uvTop").floatValue = rankVisuals[i].uvTop;
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static StyleMeterUI.RankVisual Visual(
        Dictionary<string, Sprite> sprites,
        string spriteName,
        Color accent,
        float bottom,
        float top)
    {
        return new StyleMeterUI.RankVisual
        {
            sprite = RequireSprite(sprites, spriteName),
            accentColor = accent,
            uvBottom = bottom,
            uvTop = top,
        };
    }

    private static Dictionary<string, Sprite> LoadSpritesByName(string path)
    {
        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .ToDictionary(sprite => sprite.name, StringComparer.Ordinal);
    }

    private static Sprite RequireSprite(Dictionary<string, Sprite> sprites, string spriteName)
    {
        if (!sprites.TryGetValue(spriteName, out Sprite sprite) || sprite == null)
            throw new InvalidOperationException($"Sprite '{spriteName}' not found in {RankSheetPath}.");
        return sprite;
    }

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
            throw new InvalidOperationException($"Asset '{path}' ({typeof(T).Name}) not found.");
        return asset;
    }

    private static Transform FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform nested = FindChildByName(child, name);
            if (nested != null) return nested;
        }
        return null;
    }

    private static RectTransform EnsureRect(string name, Transform parent)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing as RectTransform ?? existing.gameObject.AddComponent<RectTransform>();

        var obj = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)obj.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static void Stretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private static T GetOrAdd<T>(GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        return component != null ? component : obj.AddComponent<T>();
    }
}
