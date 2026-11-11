using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WeaponStickerAuthoring
{
    private const string RigPath = "Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab";
    private const string VisualPath = "Assets/_Game/Prefabs/Gameplay/WeaponEquipVisual.prefab";

    [MenuItem("Tools/Comic UI/Configure Weapon Sticker")]
    public static void Configure()
    {
        var rig = PrefabUtility.LoadPrefabContents(RigPath);
        CombatHudAnimationProfile profile;
        try
        {
            var feedback = rig.GetComponentInChildren<WeaponFeedback>(true);
            profile = new SerializedObject(feedback).FindProperty("animationProfile").objectReferenceValue as CombatHudAnimationProfile;
            if (profile == null) throw new InvalidOperationException("GameplayRig requires its Combat HUD Animation Profile.");
        }
        finally { PrefabUtility.UnloadPrefabContents(rig); }

        var source = PrefabUtility.LoadPrefabContents(VisualPath);
        try
        {
            var weapon = source.GetComponentInChildren<Image>(true);
            for (int i = 0; i < StickerNames.Length; i++)
                ConfigureSticker(weapon, StickerNames[i], StickerPositions[i], StickerAngles[i], profile);
            PrefabUtility.SaveAsPrefabAsset(source, VisualPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(source); }

        rig = PrefabUtility.LoadPrefabContents(RigPath);
        try
        {
            var feedback = rig.GetComponentInChildren<WeaponFeedback>(true);
            var settings = new SerializedObject(feedback);
            var weapon = settings.FindProperty("weaponImage").objectReferenceValue as Image;
            var stickers = settings.FindProperty("stickers");
            stickers.arraySize = StickerNames.Length;
            for (int i = 0; i < StickerNames.Length; i++)
            {
                var child = weapon.transform.Find(StickerNames[i]);
                var presenter = child != null ? child.GetComponent<WeaponStickerPresentation>() : null;
                if (presenter == null) throw new InvalidOperationException($"WeaponEquipVisual sticker '{StickerNames[i]}' was not inherited by GameplayRig.");
                var textSettings = new SerializedObject(presenter.GetComponentInChildren<TextMeshProUGUI>(true));
                var materialProperty = textSettings.FindProperty("m_sharedMaterial");
                if (materialProperty.prefabOverride) PrefabUtility.RevertPropertyOverride(materialProperty, InteractionMode.AutomatedAction);
                stickers.GetArrayElementAtIndex(i).objectReferenceValue = presenter;
            }
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(rig, RigPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(rig); }
    }

    // Shots cycle through these in order. Layout is only applied when a sticker is first created, so positions
    // tuned in WeaponEquipVisual survive re-running this tool.
    private static readonly string[] StickerNames = { "ComicSticker", "ComicSticker 2", "ComicSticker 3" };
    private static readonly Vector2[] StickerPositions = { new Vector2(40f, 20f), new Vector2(180f, 70f), new Vector2(-80f, 90f) };
    private static readonly float[] StickerAngles = { -10f, 8f, -4f };

    private static void ConfigureSticker(Image weapon, string name, Vector2 position, float angle, CombatHudAnimationProfile profile)
    {
        var existing = weapon.transform.Find(name);
        var root = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform), typeof(CanvasGroup));
        var rect = (RectTransform)root.transform;
        rect.SetParent(weapon.transform, false);
        if (existing == null)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(220f, 110f);
            rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
        root.layer = weapon.gameObject.layer;
        var group = root.GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.interactable = group.blocksRaycasts = false;
        var icon = Child<Image>(rect, "Sprite");
        icon.preserveAspect = true;
        icon.raycastTarget = false;
        icon.enabled = false;
        var label = Child<TextMeshProUGUI>(rect, "Text");
        label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("e9cfdf41757b64a84a74061e3794fdfb"));
        label.fontStyle = FontStyles.Bold;
        label.fontSize = 44f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 18f;
        label.fontSizeMax = 44f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1f, 0.9f, 0.2f);
        const string materialPath = "Assets/_Game/Arts/UI/WeaponStickerText.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(label.font.material);
            material.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
            material.EnableKeyword("OUTLINE_ON");
            AssetDatabase.CreateAsset(material, materialPath);
        }
        label.fontSharedMaterial = material;
        label.text = string.Empty;
        label.raycastTarget = false;
        if (!root.TryGetComponent(out WeaponStickerPresentation presenter)) presenter = root.AddComponent<WeaponStickerPresentation>();
        var settings = new SerializedObject(presenter);
        settings.FindProperty("profile").objectReferenceValue = profile;
        settings.FindProperty("icon").objectReferenceValue = icon;
        settings.FindProperty("label").objectReferenceValue = label;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T Child<T>(RectTransform parent, string name) where T : Component
    {
        var existing = parent.Find(name);
        var child = existing != null ? existing.gameObject : new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        child.layer = parent.gameObject.layer;
        return child.TryGetComponent(out T component) ? component : child.AddComponent<T>();
    }
}
