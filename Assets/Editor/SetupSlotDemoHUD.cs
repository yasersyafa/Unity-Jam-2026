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

        var playerHealth = Object.FindAnyObjectByType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("PlayerHealth not found in SlotDemo!");
            return;
        }

        var gameplayFlow = Object.FindAnyObjectByType<SlotGameplayFlow>();
        if (gameplayFlow == null)
        {
            Debug.LogError("SlotGameplayFlow not found in SlotDemo!");
            return;
        }

        var shootingController = Object.FindAnyObjectByType<ShootingController>();

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

        healthFillImg.type = Image.Type.Filled;
        healthFillImg.fillMethod = Image.FillMethod.Horizontal;
        healthFillImg.fillOrigin = 0;
        healthFillImg.fillAmount = 1f;

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
        healthSo.FindProperty("healthFill").objectReferenceValue = healthFillImg;
        healthTextGo.SetActive(false);
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

        // 3. Build HUD FireRate Bar (under Weapon Duration Bar)
        var hudFireRateGo = new GameObject("HUDFireRateBar", typeof(RectTransform), typeof(CanvasGroup));
        hudFireRateGo.transform.SetParent(hudGo.transform, false);
        var hudFireRateRect = hudFireRateGo.GetComponent<RectTransform>();
        hudFireRateRect.anchorMin = new Vector2(0.5f, 0.5f);
        hudFireRateRect.anchorMax = new Vector2(0.5f, 0.5f);
        hudFireRateRect.pivot = new Vector2(0.5f, 0.5f);
        hudFireRateRect.anchoredPosition = new Vector2(0f, 150f);
        hudFireRateRect.sizeDelta = new Vector2(320f, 18f);

        var hudFrBgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
        hudFrBgGo.transform.SetParent(hudFireRateGo.transform, false);
        var hudFrBgRect = hudFrBgGo.GetComponent<RectTransform>();
        hudFrBgRect.anchorMin = Vector2.zero;
        hudFrBgRect.anchorMax = Vector2.one;
        hudFrBgRect.sizeDelta = Vector2.zero;
        var hudFrBgImg = hudFrBgGo.GetComponent<Image>();
        hudFrBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        var hudFrFillArea = new GameObject("Fill Area", typeof(RectTransform));
        hudFrFillArea.transform.SetParent(hudFireRateGo.transform, false);
        var hudFrFillAreaRect = hudFrFillArea.GetComponent<RectTransform>();
        hudFrFillAreaRect.anchorMin = Vector2.zero;
        hudFrFillAreaRect.anchorMax = Vector2.one;
        hudFrFillAreaRect.offsetMin = new Vector2(2f, 2f);
        hudFrFillAreaRect.offsetMax = new Vector2(-2f, -2f);

        var hudFrFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        hudFrFillGo.transform.SetParent(hudFrFillArea.transform, false);
        var hudFrFillRect = hudFrFillGo.GetComponent<RectTransform>();
        hudFrFillRect.anchorMin = Vector2.zero;
        hudFrFillRect.anchorMax = Vector2.one;
        hudFrFillRect.sizeDelta = Vector2.zero;
        var hudFrFillImg = hudFrFillGo.GetComponent<Image>();
        hudFrFillImg.color = new Color(0.1f, 0.95f, 0.45f, 1f);

        var hudFrSlider = hudFireRateGo.AddComponent<Slider>();
        hudFrSlider.transition = Selectable.Transition.None;
        hudFrSlider.direction = Slider.Direction.LeftToRight;
        hudFrSlider.targetGraphic = hudFrFillImg;
        hudFrSlider.fillRect = hudFrFillRect;
        hudFrSlider.minValue = 0f;
        hudFrSlider.maxValue = 1f;
        hudFrSlider.value = 1f;

        var hudFrTextGo = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hudFrTextGo.transform.SetParent(hudFireRateGo.transform, false);
        var hudFrTextRect = hudFrTextGo.GetComponent<RectTransform>();
        hudFrTextRect.anchorMin = Vector2.zero;
        hudFrTextRect.anchorMax = Vector2.one;
        hudFrTextRect.sizeDelta = Vector2.zero;
        var hudFrTmp = hudFrTextGo.GetComponent<TextMeshProUGUI>();
        if (tmpFont != null) hudFrTmp.font = tmpFont;
        hudFrTmp.fontSize = 13f;
        hudFrTmp.fontStyle = FontStyles.Bold;
        hudFrTmp.alignment = TextAlignmentOptions.Center;
        hudFrTmp.color = Color.white;
        hudFrTmp.text = "READY";

        var hudFrUI = hudFireRateGo.AddComponent<WeaponFireRateUI>();
        var hudFrSo = new SerializedObject(hudFrUI);
        hudFrSo.FindProperty("shootingController").objectReferenceValue = shootingController;
        hudFrSo.FindProperty("slider").objectReferenceValue = hudFrSlider;
        hudFrSo.FindProperty("fillImage").objectReferenceValue = hudFrFillImg;
        hudFrSo.FindProperty("canvasGroup").objectReferenceValue = hudFireRateGo.GetComponent<CanvasGroup>();
        hudFrSo.FindProperty("statusText").objectReferenceValue = hudFrTmp;
        hudFrSo.FindProperty("onlySemiAutomatic").boolValue = true;
        hudFrSo.ApplyModifiedProperties();

        // 4. Build Crosshair FireRate Bar (under Crosshair reticle)
        var crosshairGo = GameObject.Find("Crosshair");
        if (crosshairGo != null)
        {
            var oldBar = crosshairGo.transform.Find("CrosshairFireRateBar");
            if (oldBar != null) Object.DestroyImmediate(oldBar.gameObject);

            var chBarGo = new GameObject("CrosshairFireRateBar", typeof(RectTransform), typeof(CanvasGroup));
            chBarGo.transform.SetParent(crosshairGo.transform, false);
            var chBarRect = chBarGo.GetComponent<RectTransform>();
            chBarRect.anchorMin = new Vector2(0.5f, 0.5f);
            chBarRect.anchorMax = new Vector2(0.5f, 0.5f);
            chBarRect.pivot = new Vector2(0.5f, 0.5f);
            chBarRect.anchoredPosition = new Vector2(0f, -28f);
            chBarRect.sizeDelta = new Vector2(46f, 5f);

            var chBgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            chBgGo.transform.SetParent(chBarGo.transform, false);
            var chBgRect = chBgGo.GetComponent<RectTransform>();
            chBgRect.anchorMin = Vector2.zero;
            chBgRect.anchorMax = Vector2.one;
            chBgRect.sizeDelta = Vector2.zero;
            var chBgImg = chBgGo.GetComponent<Image>();
            chBgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            var chFillArea = new GameObject("Fill Area", typeof(RectTransform));
            chFillArea.transform.SetParent(chBarGo.transform, false);
            var chFillAreaRect = chFillArea.GetComponent<RectTransform>();
            chFillAreaRect.anchorMin = Vector2.zero;
            chFillAreaRect.anchorMax = Vector2.one;
            chFillAreaRect.offsetMin = new Vector2(1f, 1f);
            chFillAreaRect.offsetMax = new Vector2(-1f, -1f);

            var chFillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            chFillGo.transform.SetParent(chFillArea.transform, false);
            var chFillRect = chFillGo.GetComponent<RectTransform>();
            chFillRect.anchorMin = Vector2.zero;
            chFillRect.anchorMax = Vector2.one;
            chFillRect.sizeDelta = Vector2.zero;
            var chFillImg = chFillGo.GetComponent<Image>();
            chFillImg.color = new Color(0.1f, 0.95f, 0.45f, 1f);

            var chSlider = chBarGo.AddComponent<Slider>();
            chSlider.transition = Selectable.Transition.None;
            chSlider.direction = Slider.Direction.LeftToRight;
            chSlider.targetGraphic = chFillImg;
            chSlider.fillRect = chFillRect;
            chSlider.minValue = 0f;
            chSlider.maxValue = 1f;
            chSlider.value = 1f;

            var chUI = chBarGo.AddComponent<WeaponFireRateUI>();
            var chSo = new SerializedObject(chUI);
            chSo.FindProperty("shootingController").objectReferenceValue = shootingController;
            chSo.FindProperty("slider").objectReferenceValue = chSlider;
            chSo.FindProperty("fillImage").objectReferenceValue = chFillImg;
            chSo.FindProperty("canvasGroup").objectReferenceValue = chBarGo.GetComponent<CanvasGroup>();
            chSo.FindProperty("onlySemiAutomatic").boolValue = true;
            chSo.ApplyModifiedProperties();
        }

        // Save as Prefab
        if (!AssetDatabase.IsValidFolder("Assets/_Game/Prefabs/UI"))
        {
            AssetDatabase.CreateFolder("Assets/_Game/Prefabs", "UI");
        }
        PrefabUtility.SaveAsPrefabAsset(hudGo, "Assets/_Game/Prefabs/UI/SlotDemoHUD.prefab");

        // 5. Connect to SlotGameplayFlow
        var flowSo = new SerializedObject(gameplayFlow);
        var sliderProp = flowSo.FindProperty("ammoSlider");
        var textProp = flowSo.FindProperty("ammoText");
        if (sliderProp != null) sliderProp.objectReferenceValue = durationSlider;
        if (textProp != null) textProp.objectReferenceValue = durationTmp;
        flowSo.ApplyModifiedProperties();

        // 6. Connect WeaponAmmoFeedback & WaveBannerPresentation if in scene
        foreach (var wdf in Object.FindObjectsByType<WeaponAmmoFeedback>())
        {
            var so = new SerializedObject(wdf);
            var fProp = so.FindProperty("flow");
            var pProp = so.FindProperty("player");
            if (fProp != null && fProp.objectReferenceValue == null) fProp.objectReferenceValue = gameplayFlow;
            if (pProp != null && pProp.objectReferenceValue == null) pProp.objectReferenceValue = playerHealth;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(wdf);
        }

        foreach (var wbp in Object.FindObjectsByType<WaveBannerPresentation>())
        {
            var so = new SerializedObject(wbp);
            var fProp = so.FindProperty("flow");
            var pProp = so.FindProperty("player");
            if (fProp != null && fProp.objectReferenceValue == null) fProp.objectReferenceValue = gameplayFlow;
            if (pProp != null && pProp.objectReferenceValue == null) pProp.objectReferenceValue = playerHealth;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(wbp);
        }

        EditorUtility.SetDirty(gameplayFlow);
        EditorUtility.SetDirty(healthUI);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("Slot Demo HUD setup, FireRate Bar, and Feedback wiring completed successfully.");
    }
}
