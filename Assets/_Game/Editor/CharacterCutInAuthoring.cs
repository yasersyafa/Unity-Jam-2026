using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class CharacterCutInAuthoring
{
    [MenuItem("Tools/Comic UI/Configure Character Cut In")]
    public static void Configure()
    {
        const string path = "Assets/_Game/Prefabs/UI/CharacterCutIn.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            var root = new GameObject("CharacterCutIn", typeof(RectTransform), typeof(CharacterCutIn));
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMax = Vector2.one; rootRect.anchorMin = Vector2.zero; rootRect.sizeDelta = Vector2.zero;
            var animated = Child(root.transform, "AnimatedRoot");
            var group = animated.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0; group.blocksRaycasts = group.interactable = false;
            var panel = Image(animated, "Panel", new Color(0.07f, 0.025f, 0.12f));
            panel.rectTransform.localRotation = Quaternion.Euler(0, 0, -4);
            var border = panel.gameObject.AddComponent<Outline>(); border.effectColor = Color.black; border.effectDistance = new Vector2(6, -6);
            var texture = Image(animated, "Halftone", new Color(1, 0.85f, 0.1f, 0.18f));
            texture.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Arts/Environment/HalftoneGradientTexture.png");
            var slash = Image(animated, "SpeedLines", Color.yellow);
            slash.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Arts/Environment/YellowRadialSpeedBurst.png");
            slash.color = new Color(1, 0.85f, 0.1f, 0.2f);
            var portrait = Image(animated, "Portrait", Color.white); portrait.preserveAspect = true;
            var label = Child(animated, "Quote").gameObject.AddComponent<TextMeshProUGUI>();
            label.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("e9cfdf41757b64a84a74061e3794fdfb"));
            label.fontStyle = FontStyles.Bold | FontStyles.Italic; label.enableAutoSizing = true;
            label.fontSizeMin = 16; label.fontSizeMax = 48; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
            var audio = root.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.spatialBlend = 0;
            var settings = new SerializedObject(root.GetComponent<CharacterCutIn>());
            Set(settings, "animatedRoot", animated); Set(settings, "visibility", group); Set(settings, "portrait", portrait);
            Set(settings, "accent", slash); Set(settings, "quote", label); Set(settings, "audioSource", audio);
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path); Object.DestroyImmediate(root);
        }
        const string rigPath = "Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab";
        var rig = PrefabUtility.LoadPrefabContents(rigPath);
        try
        {
            var canvas = rig.GetComponentInChildren<Canvas>(true);
            if (canvas == null) throw new System.InvalidOperationException("GameplayRig requires a Canvas.");
            var view = rig.GetComponentInChildren<CharacterCutIn>(true);
            if (view == null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), canvas.transform);
                view = instance.GetComponent<CharacterCutIn>();
            }
            var bridge = rig.GetComponent<WeaponCutInTrigger>() ?? rig.AddComponent<WeaponCutInTrigger>();
            var settings = new SerializedObject(bridge);
            Set(settings, "shooting", rig.GetComponentInChildren<ShootingController>(true)); Set(settings, "cutIn", view);
            settings.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(rig, rigPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(rig); }
    }
    private static void Set(SerializedObject target, string field, Object value) { target.FindProperty(field).objectReferenceValue = value; }
    private static RectTransform Child(Transform parent, string name)
    {
        var child = new GameObject(name, typeof(RectTransform)); child.layer = 5; child.transform.SetParent(parent, false);
        var rect = (RectTransform)child.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
        return rect;
    }
    private static Image Image(Transform parent, string name, Color color)
    {
        var image = Child(parent, name).gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
    }
}
