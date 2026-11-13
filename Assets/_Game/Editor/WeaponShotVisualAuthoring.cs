using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>One-time defaults from the weapon design sheet; existing profiles and prefabs retain designer edits.</summary>
public static class WeaponShotVisualAuthoring
{
    private const string ProfileFolder = "Assets/_Game/ScriptableObjects/WeaponVisuals";
    private const string PrefabFolder = "Assets/_Game/Prefabs/Weapons";

    [MenuItem("Tools/Weapon Visuals/Create Missing Visuals")]
    public static void CreateMissingVisuals()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before authoring weapon visuals.");
        if (!AssetDatabase.IsValidFolder(ProfileFolder)) AssetDatabase.CreateFolder("Assets/_Game/ScriptableObjects", "WeaponVisuals");
        foreach (string guid in AssetDatabase.FindAssets("t:WeaponData", new[] { "Assets/_Game/ScriptableObjects/Weapons" }))
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(guid));
            string profilePath = ProfileFolder + "/" + weapon.name + "_Visual.asset";
            var profile = AssetDatabase.LoadAssetAtPath<WeaponShotVisualProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<WeaponShotVisualProfile>();
                Defaults(profile, weapon.name);
                AssetDatabase.CreateAsset(profile, profilePath);
            }
            var data = new SerializedObject(weapon);
            if (weapon.ShotVisualProfile == null) data.FindProperty("shotVisualProfile").objectReferenceValue = profile;
            if (weapon.VisualPrefab == null)
                data.FindProperty("visualPrefab").objectReferenceValue = CreateVisualPrefab(weapon, profile);
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        SetM4Muzzle();
        // Damage remains instantaneous; these three effects now belong to the projectile's arrival presentation.
        foreach (string name in new[] { "Durian", "LPG", "Rocket" })
        {
            var damage = AssetDatabase.LoadAssetAtPath<ExplosionDamage>("Assets/_Game/ScriptableObjects/DamageTypes/Explosion_" + name + ".asset");
            if (damage == null) throw new InvalidOperationException("Missing explosion damage: " + name);
            var data = new SerializedObject(damage);
            data.FindProperty("impactVfx").objectReferenceValue = null;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Weapon visuals authored. Existing profile and prefab tuning preserved.");
    }

    private static void Defaults(WeaponShotVisualProfile profile, string weapon)
    {
        var data = new SerializedObject(profile);
        void Mode(WeaponShotVisualProfile.VisualMode mode) => data.FindProperty("mode").enumValueIndex = (int)mode;
        void Shape(WeaponShotVisualProfile.Shape shape) => data.FindProperty("shape").enumValueIndex = (int)shape;
        void Color(Color color) => data.FindProperty("color").colorValue = color;
        void Float(string key, float value) => data.FindProperty(key).floatValue = value;
        void Size(float x, float y) => data.FindProperty("size").vector2Value = new Vector2(x, y);
        void Wind() { data.FindProperty("trailColor").colorValue = new Color(0.8f, 0.95f, 1f, 0.5f); Float("spin", 420f); }
        void Explosion(float size) { Float("impactSize", size); Float("impactDuration", 0.35f); data.FindProperty("impactColor").colorValue = new Color(1f, 0.4f, 0.08f); }
        Mode(WeaponShotVisualProfile.VisualMode.Projectile);
        switch (weapon)
        {
            case "Bambu_Runcing":
                Shape(WeaponShotVisualProfile.Shape.Spear); Color(new Color(0.65f, 0.85f, 0.25f)); Size(150f, 16f); break;
            case "Batu_Biasa":
            case "_StandarWeapon":
            case "Ketapel":
                Shape(WeaponShotVisualProfile.Shape.Rock); Color(new Color(0.6f, 0.65f, 0.72f)); Size(38f, 32f); Wind(); break;
            case "Sandal_Slipper":
                Shape(WeaponShotVisualProfile.Shape.Slipper); Color(new Color(0.25f, 0.75f, 0.95f)); Size(75f, 34f); Wind(); break;
            case "Nerf_Gun":
                Shape(WeaponShotVisualProfile.Shape.Dart); Color(new Color(1f, 0.45f, 0.12f)); Size(65f, 16f); Float("speed", 2400f); break;
            case "Water_Gun":
                Mode(WeaponShotVisualProfile.VisualMode.Stream); Shape(WeaponShotVisualProfile.Shape.Dart);
                Color(new Color(0.15f, 0.7f, 1f, 0.75f)); Float("streamWidth", 30f); Float("streamLength", 1100f); break;
            case "Flamethrower":
                Mode(WeaponShotVisualProfile.VisualMode.Stream); Shape(WeaponShotVisualProfile.Shape.Dart);
                Color(new Color(1f, 0.4f, 0.06f, 0.85f)); Float("streamWidth", 110f); Float("streamLength", 780f); break;
            case "Durian":
                Shape(WeaponShotVisualProfile.Shape.Durian); Color(new Color(0.6f, 0.8f, 0.15f)); Size(65f, 65f);
                data.FindProperty("orientToTravel").boolValue = false;
                data.FindProperty("fragmentCount").intValue = 12;
                data.FindProperty("impactColor").colorValue = new Color(0.8f, 0.9f, 0.25f);
                Float("impactSize", 100f); Float("impactDuration", 0.4f); break;
            case "LPG":
                Shape(WeaponShotVisualProfile.Shape.Cylinder); Color(new Color(0.25f, 0.8f, 0.4f)); Size(48f, 70f);
                data.FindProperty("orientToTravel").boolValue = false; Explosion(160f); break;
            case "Rocket_Launcher":
                Shape(WeaponShotVisualProfile.Shape.Rocket); Color(new Color(0.8f, 0.85f, 0.65f)); Size(95f, 30f);
                data.FindProperty("trailColor").colorValue = new Color(1f, 0.45f, 0.08f, 0.9f);
                Float("trailLength", 110f); Float("trailWidth", 22f); Float("speed", 1400f); Explosion(220f); break;
            case "Glock_Pistol":
            case "Uzi":
            case "M4A1":
            case "AWP":
            case "Machine_Gun_M134-A2":
            case "Jackpot":
                Mode(WeaponShotVisualProfile.VisualMode.Hitscan); break;
            default:
                UnityEngine.Object.DestroyImmediate(profile);
                throw new InvalidOperationException("Weapon has no visual design mapping: " + weapon);
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static WeaponVisual CreateVisualPrefab(WeaponData weapon, WeaponShotVisualProfile profile)
    {
        string path = PrefabFolder + "/" + weapon.name + "_Visual.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing.GetComponent<WeaponVisual>();
        var root = new GameObject(weapon.name + "_Visual", typeof(RectTransform), typeof(WeaponVisual));
        try
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(500f, 340f);
            var body = new GameObject("WeaponSilhouette", typeof(RectTransform), typeof(CanvasRenderer), typeof(WeaponEffectGraphic));
            body.transform.SetParent(root.transform, false);
            ConfigureBody(weapon, profile, body.GetComponent<WeaponEffectGraphic>());
            var muzzle = new GameObject("MuzzleAnchor", typeof(RectTransform));
            muzzle.transform.SetParent(body.transform, false);
            var muzzleRect = (RectTransform)muzzle.transform;
            muzzleRect.anchorMin = muzzleRect.anchorMax = new Vector2(0.5f, 1f);
            muzzleRect.anchoredPosition = Vector2.zero;
            if (body.GetComponent<WeaponEffectGraphic>().rectTransform.localRotation != Quaternion.identity)
                muzzleRect.anchorMin = muzzleRect.anchorMax = new Vector2(1f, 0.6f);
            var data = new SerializedObject(root.GetComponent<WeaponVisual>());
            data.FindProperty("muzzleAnchor").objectReferenceValue = muzzleRect;
            data.ApplyModifiedPropertiesWithoutUndo();
            return PrefabUtility.SaveAsPrefabAsset(root, path).GetComponent<WeaponVisual>();
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    private static void ConfigureBody(WeaponData weapon, WeaponShotVisualProfile profile, WeaponEffectGraphic body)
    {
        bool gun = profile.Mode != WeaponShotVisualProfile.VisualMode.Projectile
            || profile.Silhouette == WeaponShotVisualProfile.Shape.Dart
            || profile.Silhouette == WeaponShotVisualProfile.Shape.Rocket;
        body.rectTransform.sizeDelta = gun ? new Vector2(220f, 110f)
            : profile.Silhouette == WeaponShotVisualProfile.Shape.Spear ? new Vector2(18f, 260f) : new Vector2(100f, 130f);
        body.rectTransform.localRotation = gun ? Quaternion.Euler(0f, 0f, 135f) : Quaternion.identity;
        body.Configure(gun ? WeaponShotVisualProfile.Shape.Gun : profile.Silhouette, weapon.Sprite, profile.Color);
    }

    private static void SetM4Muzzle()
    {
        const string path = PrefabFolder + "/M4_Visual.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var data = new SerializedObject(root.GetComponent<WeaponVisual>());
            if (data.FindProperty("muzzleAnchor").objectReferenceValue != null) return;
            data.FindProperty("muzzleAnchor").objectReferenceValue = root.GetComponentsInChildren<RectTransform>(true).Single(t => t.name == "MuzzleAnchor");
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
}
