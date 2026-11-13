using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ComicUiAuthoring
{
    private const string ProfileFolder = "Assets/_Game/ScriptableObjects/UI";
    private static SlotPresentationProfile slotProfile;
    private static CombatHudAnimationProfile hudProfile;
    private static Sprite fillSprite;

    // Run on saved assets so the open scene and its unsaved edits stay untouched.
    [MenuItem("Tools/Comic UI/Configure Saved Prefabs and SlotDemo")]
    public static void Configure()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/Scenes/SlotDemo.unity").isLoaded)
            throw new InvalidOperationException("Close SlotDemo before running saved-asset authoring; unsaved scene changes must be preserved.");
        EnsureFolder(ProfileFolder);
        slotProfile = Profile<SlotPresentationProfile>("SlotPresentation");
        hudProfile = Profile<CombatHudAnimationProfile>("CombatHudAnimation");
        fillSprite = EnsureFillSprite();
        ConfigurePrefab("Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab");
        ConfigurePrefab("Assets/_Game/Prefabs/UI/SlotDemoHUD.prefab");
        var scene = EditorSceneManager.OpenScene("Assets/Scenes/SlotDemo.unity", OpenSceneMode.Additive);
        try
        {
            var components = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<MonoBehaviour>(true)).ToArray();
            var machine = components.OfType<SlotMachineView>().Single();
            var flow = components.OfType<SlotGameplayFlow>().Single();
            var player = components.OfType<PlayerHealth>().Single();
            var spawner = components.OfType<EnemySpawner>().Single();
            GameObject backdrop = Read<GameObject>(machine, "slotCanvas");
            RectTransform panel = backdrop.transform.Find("Machine") as RectTransform;
            if (panel == null) throw new InvalidOperationException("Slot panel Machine is missing.");
            var presentation = Get<SlotPanelPresentation>(backdrop);
            Assign(presentation, "profile", slotProfile);
            Assign(presentation, "panel", panel);
            Assign(presentation, "visibility", Get<CanvasGroup>(backdrop));
            Assign(machine, "panelPresentation", presentation);
            Transform rewardRoot = backdrop.transform.Find("ComicReward");
            if (rewardRoot == null) rewardRoot = Rect("ComicReward", backdrop.transform, new Vector2(0.5f, 0.15f), new Vector2(360, 160));
            var reward = Get<SlotRewardPresentation>(rewardRoot.gameObject);
            var rewardGroup = Get<CanvasGroup>(rewardRoot.gameObject); rewardGroup.alpha = 0f; rewardGroup.blocksRaycasts = false;
            var icon = ChildImage(rewardRoot, "Icon", new Vector2(0.5f, 0.7f), new Vector2(100, 80));
            var label = ChildText(rewardRoot, "Label", new Vector2(0.5f, 0.2f), new Vector2(360, 65), 24);
            Assign(reward, "profile", slotProfile); Assign(reward, "visual", rewardRoot); Assign(reward, "group", rewardGroup);
            Assign(reward, "icon", icon); Assign(reward, "label", label); Assign(machine, "rewardPresentation", reward);
            Slider duration = Read<Slider>(flow, "ammoSlider");
            var durationFeedback = Get<WeaponAmmoFeedback>(duration.gameObject);
            Assign(durationFeedback, "flow", flow); Assign(durationFeedback, "player", player); Assign(durationFeedback, "profile", hudProfile);
            Assign(durationFeedback, "accent", Accent(duration.transform, "CriticalDurationAccent"));
            Transform canvas = duration.GetComponentInParent<Canvas>().transform;
            Transform bannerRoot = canvas.Find("WaveBanner");
            if (bannerRoot == null) bannerRoot = Rect("WaveBanner", canvas, new Vector2(0.5f, 0.88f), new Vector2(360, 60));
            var banner = Get<WaveBannerPresentation>(bannerRoot.gameObject);
            var bannerGroup = Get<CanvasGroup>(bannerRoot.gameObject); bannerGroup.alpha = 0f; bannerGroup.blocksRaycasts = false;
            Assign(banner, "spawner", spawner); Assign(banner, "flow", flow); Assign(banner, "player", player);
            Assign(banner, "profile", hudProfile); Assign(banner, "visual", bannerRoot); Assign(banner, "group", bannerGroup);
            Assign(banner, "label", ChildText(bannerRoot, "Label", new Vector2(0.5f, 0.5f), new Vector2(360, 60), 32));
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        finally { EditorSceneManager.CloseScene(scene, true); }
        AssetDatabase.SaveAssets();
        Debug.Log("Comic UI authoring completed.");
    }
    private static void ConfigurePrefab(string path)
    {
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (var button in root.GetComponentsInChildren<Button>(true))
                if (button.name == "Spin" || button.name == "Skip") SetupButton(button);
            foreach (var health in root.GetComponentsInChildren<PlayerHealthUI>(true))
            {
                var fillImage = Read<Image>(health, "healthFill");
                if (fillImage == null) continue;
                Transform bar = health.transform;
                Transform visual = bar.Find("HealthVisual");
                if (visual == null)
                {
                    var children = bar.Cast<Transform>().ToArray();
                    visual = Rect("HealthVisual", bar, new Vector2(0.5f, 0.5f), Vector2.zero);
                    Stretch((RectTransform)visual);
                    foreach (var child in children) child.SetParent(visual, false);
                }
                RectTransform fill = fillImage.rectTransform;
                var trail = ChildImage(fill.parent, "DamageTrail", new Vector2(0.5f, 0.5f), Vector2.zero);
                Stretch(trail.rectTransform); trail.transform.SetAsFirstSibling();
                Sprite sourceSprite = fill.GetComponent<Image>().sprite;
                trail.sprite = sourceSprite != null ? sourceSprite : fillSprite;
                trail.type = Image.Type.Filled; trail.fillMethod = Image.FillMethod.Horizontal; trail.fillOrigin = 0;
                trail.color = hudProfile.TrailColor; trail.fillAmount = 1f;
                Assign(health, "profile", hudProfile); Assign(health, "impactRoot", visual); Assign(health, "damageTrail", trail);
                Assign(health, "criticalAccent", Accent(visual, "CriticalHealthAccent"));
                var damage = root.GetComponentInChildren<PlayerDamageFeedback>(true);
                if (damage != null) Assign(health, "damagePreset", Read<CombatFeedbackPreset>(damage, "preset"));
            }
            foreach (var weapon in root.GetComponentsInChildren<WeaponFeedback>(true))
            {
                var image = Read<Image>(weapon, "weaponImage");
                Transform equip = image.transform.parent.Find("WeaponEquipVisual");
                if (equip == null)
                {
                    equip = Rect("WeaponEquipVisual", image.transform.parent, new Vector2(0.5f, 0.5f), Vector2.zero);
                    Stretch((RectTransform)equip); image.transform.SetParent(equip, false);
                }
                Assign(weapon, "animationProfile", hudProfile); Assign(weapon, "equipRoot", equip);
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void SetupButton(Button button)
    {
        Transform visual = button.transform.Find("ComicButtonVisual");
        if (visual == null)
        {
            var children = button.transform.Cast<Transform>().ToArray();
            visual = Rect("ComicButtonVisual", button.transform, new Vector2(0.5f, 0.5f), Vector2.zero);
            Stretch((RectTransform)visual);
            var source = button.GetComponent<Image>();
            if (source != null)
            {
                var image = visual.gameObject.AddComponent<Image>(); EditorUtility.CopySerialized(source, image);
                image.raycastTarget = false; Color color = source.color; color.a = 0f; source.color = color; button.targetGraphic = image;
            }
            foreach (var child in children) child.SetParent(visual, false);
        }
        var feedback = Get<ComicButtonFeedback>(button.gameObject);
        Assign(feedback, "profile", slotProfile); Assign(feedback, "visual", visual);
    }
    private static Image Accent(Transform parent, string name)
    {
        var image = ChildImage(parent, name, new Vector2(0.5f, 1f), new Vector2(0, 4));
        image.rectTransform.anchorMin = new Vector2(0f, 1f); image.rectTransform.anchorMax = Vector2.one;
        image.color = new Color(1f, 0.3f, 0.15f, 0.35f); return image;
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = anchor; rect.sizeDelta = size; return rect;
    }
    private static void Stretch(RectTransform rect) { rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero; }
    private static Image ChildImage(Transform parent, string name, Vector2 anchor, Vector2 size)
    {
        var child = parent.Find(name);
        if (child == null) child = Rect(name, parent, anchor, size);
        var image = Get<Image>(child.gameObject); image.raycastTarget = false; return image;
    }
    private static TextMeshProUGUI ChildText(Transform parent, string name, Vector2 anchor, Vector2 size, int fontSize)
    {
        var child = parent.Find(name);
        if (child == null) child = Rect(name, parent, anchor, size);
        var text = Get<TextMeshProUGUI>(child.gameObject);
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
    }
    private static Sprite EnsureFillSprite()
    {
        const string path = "Assets/_Game/Arts/UI/ComicHudFill.png";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null) return sprite;
        EnsureFolder("Assets/_Game/Arts/UI");
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
        texture.Apply();
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    private static T Get<T>(GameObject target) where T : Component
    {
        var component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }
    private static T Read<T>(UnityEngine.Object target, string field) where T : UnityEngine.Object => (T)new SerializedObject(target).FindProperty(field).objectReferenceValue;
    private static void Assign(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
    }
    private static T Profile<T>(string name) where T : ScriptableObject
    {
        string path = ProfileFolder + "/" + name + ".asset";
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) { asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); }
        return asset;
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0, slash)); AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
