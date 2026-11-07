using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class SetupSlotDemoHUD
{
    [MenuItem("Tools/Setup Slot Demo HUD")]
    public static void Execute()
    {
        string scenePath = "Assets/Scenes/SlotDemo.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth not found in SlotDemo!");
            return;
        }

        var gameplayFlow = Object.FindFirstObjectByType<SlotGameplayFlow>();
        if (gameplayFlow == null)
        {
            Debug.LogError("SlotGameplayFlow not found in SlotDemo!");
            return;
        }

        // Get default TMP Font asset
        var tmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        // Find or create SlotDemoHUD Canvas
        var hudGo = GameObject.Find("SlotDemoHUD");
        if (hudGo != null)
        {
            Object.DestroyImmediate(hudGo);
        }

        hudGo = new GameObject("SlotDemoHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = hudGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = hudGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        // 1. Build Player Health Bar
        var healthBarGo = new GameObject("PlayerHealthBar", typeof(RectTransform));
        healthBarGo.transform.SetParent(hudGo.transform, false);
        var healthRect = healthBarGo.GetComponent<RectTransform>();
        healthRect.anchorMin = new Vector2(0f, 0f);
        healthRect.anchorMax = new Vector2(0f, 0f);
        healthRect.pivot = new Vector2(0f, 0f);
        healthRect.anchoredPosition = new Vector2(50f, 50f);
        healthRect.sizeDelta = new Vector2(340f, 36f);

        // Background
        var healthBgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        healthBgGo.transform.SetParent(healthBarGo.transform, false);
        var healthBgRect = healthBgGo.GetComponent<RectTransform>();
        healthBgRect.anchorMin = Vector2.zero;
        healthBgRect.anchorMax = Vector2.one;
        healthBgRect.sizeDelta = Vector2.zero;
        var healthBgImg = healthBgGo.GetComponent<Image>();
        healthBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        // Fill Area
        var healthFillArea = new GameObject("Fill Area", typeof(RectTransform));
        healthFillArea.transform.SetParent(healthBarGo.transform, false);
        var healthFillAreaRect = healthFillArea.GetComponent<RectTransform>();
        healthFillAreaRect.anchorMin = Vector2.zero;
        healthFillAreaRect.anchorMax = Vector2.one;
        healthFillAreaRect.offsetMin = new Vector2(3f, 3f);
        healthFillAreaRect.offsetMax = new Vector2(-3f, -3f);

        var healthFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        healthFillGo.transform.SetParent(healthFillArea.transform, false);
        var healthFillRect = healthFillGo.GetComponent<RectTransform>();
        healthFillRect.anchorMin = Vector2.zero;
        healthFillRect.anchorMax = Vector2.one;
        healthFillRect.sizeDelta = Vector2.zero;
        var healthFillImg = healthFillGo.GetComponent<Image>();
        healthFillImg.color = new Color(0.85f, 0.15f, 0.15f, 1f); // Vibrant red

        var healthSlider = healthBarGo.AddComponent<Slider>();
        healthSlider.transition = Selectable.Transition.None;
        healthSlider.direction = Slider.Direction.LeftToRight;
        healthSlider.targetGraphic = healthFillImg;
        healthSlider.fillRect = healthFillRect;
        healthSlider.minValue = 0f;
        healthSlider.maxValue = playerHealth.MaximumHealth;
        healthSlider.value = playerHealth.CurrentHealth;

        // Health Text
        var healthTextGo = new GameObject("HealthText", typeof(RectTransform), typeof(TextMeshProUGUI));
        healthTextGo.transform.SetParent(healthBarGo.transform, false);
        var healthTextRect = healthTextGo.GetComponent<RectTransform>();
        healthTextRect.anchorMin = Vector2.zero;
        healthTextRect.anchorMax = Vector2.one;
        healthTextRect.sizeDelta = Vector2.zero;
        var healthTmp = healthTextGo.GetComponent<TextMeshProUGUI>();
        if (tmpFont != null) healthTmp.font = tmpFont;
        healthTmp.fontSize = 20f;
        healthTmp.fontStyle = FontStyles.Bold;
        healthTmp.alignment = TextAlignmentOptions.Center;
        healthTmp.color = Color.white;
        healthTmp.text = $"HP {Mathf.CeilToInt(playerHealth.CurrentHealth)} / {Mathf.CeilToInt(playerHealth.MaximumHealth)}";

        var healthUI = healthBarGo.AddComponent<PlayerHealthUI>();
        var healthSo = new SerializedObject(healthUI);
        healthSo.FindProperty("playerHealth").objectReferenceValue = playerHealth;
        healthSo.FindProperty("healthSlider").objectReferenceValue = healthSlider;
        healthSo.FindProperty("healthText").objectReferenceValue = healthTmp;
        healthSo.ApplyModifiedProperties();

        // 2. Build Weapon Duration Bar
        var durationBarGo = new GameObject("WeaponDurationBar", typeof(RectTransform));
        durationBarGo.transform.SetParent(hudGo.transform, false);
        var durationRect = durationBarGo.GetComponent<RectTransform>();
        durationRect.anchorMin = new Vector2(0.5f, 0.5f);
        durationRect.anchorMax = new Vector2(0.5f, 0.5f);
        durationRect.pivot = new Vector2(0.5f, 0.5f);
        durationRect.anchoredPosition = new Vector2(0f, 185f);
        durationRect.sizeDelta = new Vector2(460f, 26f);

        // Background
        var durationBgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        durationBgGo.transform.SetParent(durationBarGo.transform, false);
        var durationBgRect = durationBgGo.GetComponent<RectTransform>();
        durationBgRect.anchorMin = Vector2.zero;
        durationBgRect.anchorMax = Vector2.one;
        durationBgRect.sizeDelta = Vector2.zero;
        var durationBgImg = durationBgGo.GetComponent<Image>();
        durationBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        // Fill Area
        var durationFillArea = new GameObject("Fill Area", typeof(RectTransform));
        durationFillArea.transform.SetParent(durationBarGo.transform, false);
        var durationFillAreaRect = durationFillArea.GetComponent<RectTransform>();
        durationFillAreaRect.anchorMin = Vector2.zero;
        durationFillAreaRect.anchorMax = Vector2.one;
        durationFillAreaRect.offsetMin = new Vector2(2f, 2f);
        durationFillAreaRect.offsetMax = new Vector2(-2f, -2f);

        var durationFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        durationFillGo.transform.SetParent(durationFillArea.transform, false);
        var durationFillRect = durationFillGo.GetComponent<RectTransform>();
        durationFillRect.anchorMin = Vector2.zero;
        durationFillRect.anchorMax = Vector2.one;
        durationFillRect.sizeDelta = Vector2.zero;
        var durationFillImg = durationFillGo.GetComponent<Image>();
        durationFillImg.color = new Color(1f, 0.7f, 0.05f, 1f); // Vibrant amber/gold

        var durationSlider = durationBarGo.AddComponent<Slider>();
        durationSlider.transition = Selectable.Transition.None;
        durationSlider.direction = Slider.Direction.LeftToRight;
        durationSlider.targetGraphic = durationFillImg;
        durationSlider.fillRect = durationFillRect;
        durationSlider.minValue = 0f;
        durationSlider.maxValue = 8f;
        durationSlider.value = 8f;

        // Duration Text
        var durationTextGo = new GameObject("DurationText", typeof(RectTransform), typeof(TextMeshProUGUI));
        durationTextGo.transform.SetParent(durationBarGo.transform, false);
        var durationTextRect = durationTextGo.GetComponent<RectTransform>();
        durationTextRect.anchorMin = Vector2.zero;
        durationTextRect.anchorMax = Vector2.one;
        durationTextRect.sizeDelta = Vector2.zero;
        var durationTmp = durationTextGo.GetComponent<TextMeshProUGUI>();
        if (tmpFont != null) durationTmp.font = tmpFont;
        durationTmp.fontSize = 17f;
        durationTmp.fontStyle = FontStyles.Bold;
        durationTmp.alignment = TextAlignmentOptions.Center;
        durationTmp.color = Color.white;
        durationTmp.text = "Weapon: 8.0s / 8.0s";

        // Save as Prefab
        if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs/UI"))
        {
            AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "UI");
        }
        PrefabUtility.SaveAsPrefabAsset(hudGo, "Assets/_Game/Prefabs/UI/SlotDemoHUD.prefab");

        // 3. Connect to SlotGameplayFlow
        var flowSo = new SerializedObject(gameplayFlow);
        var sliderProp = flowSo.FindProperty("weaponDurationSlider");
        var textProp = flowSo.FindProperty("weaponDurationText");
        if (sliderProp != null) sliderProp.objectReferenceValue = durationSlider;
        if (textProp != null) textProp.objectReferenceValue = durationTmp;
        flowSo.ApplyModifiedProperties();

        EditorUtility.SetDirty(gameplayFlow);
        EditorUtility.SetDirty(healthUI);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Slot Demo HUD setup and Prefab creation completed successfully.");
    }
}
