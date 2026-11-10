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
            var existing = weapon.transform.Find("ComicSticker");
            var root = existing != null ? existing.gameObject : new GameObject("ComicSticker", typeof(RectTransform), typeof(CanvasGroup));
            var rect = (RectTransform)root.transform;
            rect.SetParent(weapon.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(40f, 20f);
            rect.sizeDelta = new Vector2(220f, 110f);
            rect.localRotation = Quaternion.Euler(0f, 0f, -10f);
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
            var presenter = root.GetComponent<WeaponStickerPresentation>() ?? root.AddComponent<WeaponStickerPresentation>();
            var settings = new SerializedObject(presenter);
            settings.FindProperty("profile").objectReferenceValue = profile;
            settings.FindProperty("icon").objectReferenceValue = icon;
            settings.FindProperty("label").objectReferenceValue = label;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(source, VisualPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(source); }

        rig = PrefabUtility.LoadPrefabContents(RigPath);
        try
        {
            var feedback = rig.GetComponentInChildren<WeaponFeedback>(true);
            var settings = new SerializedObject(feedback);
            var weapon = settings.FindProperty("weaponImage").objectReferenceValue as Image;
            var presenter = weapon.GetComponentInChildren<WeaponStickerPresentation>(true);
            if (presenter == null) throw new InvalidOperationException("WeaponEquipVisual sticker was not inherited by GameplayRig.");
            var textSettings = new SerializedObject(presenter.GetComponentInChildren<TextMeshProUGUI>(true));
            var materialProperty = textSettings.FindProperty("m_sharedMaterial");
            if (materialProperty.prefabOverride) PrefabUtility.RevertPropertyOverride(materialProperty, InteractionMode.AutomatedAction);
            settings.FindProperty("sticker").objectReferenceValue = presenter;
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(rig, RigPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(rig); }
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
        return child.GetComponent<T>() ?? child.AddComponent<T>();
    }
}
