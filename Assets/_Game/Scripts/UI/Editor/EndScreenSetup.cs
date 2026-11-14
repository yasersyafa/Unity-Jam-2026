using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class EndScreenSetup
{
    private const string PrefabPath = "Assets/_Game/Resources/EndScreenPresentation.prefab";
    private const string WinPanelPath = "Assets/_Game/Arts/UI/WinPanel.png";
    private const string LosePanelPath = "Assets/_Game/Arts/UI/LosePanel.png";
    private const string VictoryPath = "Assets/_Game/Arts/UI/Victory.png";
    private const string LosePath = "Assets/_Game/Arts/UI/Lose.png";
    private const string RetryButtonPath = "Assets/_Game/Arts/UI/RetryButton.png";
    private const string MainMenuButtonPath = "Assets/_Game/Arts/UI/MainMenuButton.png";
    private const string Damaged1ClipPath = "Assets/_Game/Audio/SFX/Player/Damaged 1.wav";
    private const string Damaged2ClipPath = "Assets/_Game/Audio/SFX/Player/Damaged 2.wav";
    private const string GameWinClipPath = "Assets/_Game/Audio/SFX/Player/Game Win.mp3";
    private const string GameLoseClipPath = "Assets/_Game/Audio/SFX/Player/Game Lose.mp3";
    private const string PlayerHitCuePath = "Assets/_Game/ScriptableObjects/Audio/PlayerHit.asset";
    private const string GameWinCuePath = "Assets/_Game/ScriptableObjects/Audio/GameWin.asset";
    private const string GameLoseCuePath = "Assets/_Game/ScriptableObjects/Audio/GameLose.asset";
    private const string GameplayRigPath = "Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab";

    [MenuItem("Game/UI/Build End Screen Presentation Prefab")]
    public static void BuildEndScreenPrefab()
    {
        var winPanel = LoadSprite(WinPanelPath);
        var losePanel = LoadSprite(LosePanelPath);
        var victory = LoadSprite(VictoryPath);
        var lose = LoadSprite(LosePath);
        var retrySprite = LoadSprite(RetryButtonPath);
        var mainMenuSprite = LoadSprite(MainMenuButtonPath);

        var damaged1 = LoadClip(Damaged1ClipPath);
        var damaged2 = LoadClip(Damaged2ClipPath);
        var winClip = LoadClip(GameWinClipPath);
        var loseClip = LoadClip(GameLoseClipPath);

        var playerHitCue = EnsureCue(PlayerHitCuePath, new[] { damaged1, damaged2 }, 0.85f, new Vector2(0.96f, 1.04f), 0.04f, 4, ui: false);
        var gameWinCue = EnsureCue(GameWinCuePath, new[] { winClip }, 0.9f, Vector2.one, 0.1f, 2, ui: true);
        var gameLoseCue = EnsureCue(GameLoseCuePath, new[] { loseClip }, 0.9f, Vector2.one, 0.1f, 2, ui: true);
        WirePlayerHitOnRig(playerHitCue);

        var rootObj = new GameObject(
            "EndScreenPresentation",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup),
            typeof(EndScreenPresentation));

        try
        {
            var canvas = rootObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 550;

            var scaler = rootObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var rootGroup = rootObj.GetComponent<CanvasGroup>();
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;

            // 1. Lightning bolt panel (slides in fast from the right)
            var panelObj = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panelObj.transform.SetParent(rootObj.transform, false);
            var panelRect = panelObj.GetComponent<RectTransform>();
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = new Vector2(0f, 65f);
            panelRect.sizeDelta = new Vector2(1960f, 585f);
            var panelImage = panelObj.GetComponent<Image>();
            panelImage.sprite = winPanel;
            panelImage.preserveAspect = false;
            panelImage.raycastTarget = false;

            // 2. Center Title ("Victory" / "Lose" - fast fade-in + kedat-kedut idle animation)
            var titleObj = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            titleObj.transform.SetParent(rootObj.transform, false);
            var titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 65f);
            titleRect.sizeDelta = new Vector2(1248f, 384f);
            var titleImage = titleObj.GetComponent<Image>();
            titleImage.sprite = victory;
            titleImage.preserveAspect = true;
            titleImage.raycastTarget = false;
            var titleGroup = titleObj.GetComponent<CanvasGroup>();
            titleGroup.alpha = 0f;
            titleGroup.blocksRaycasts = false;

            // 3. Buttons container ("Retry" tilted +10 deg on hover, "Main Menu" tilted -10 deg on hover)
            var buttonsObj = new GameObject("Buttons", typeof(RectTransform), typeof(CanvasGroup));
            buttonsObj.transform.SetParent(rootObj.transform, false);
            var buttonsRect = buttonsObj.GetComponent<RectTransform>();
            buttonsRect.anchorMin = Vector2.zero;
            buttonsRect.anchorMax = Vector2.one;
            buttonsRect.offsetMin = buttonsRect.offsetMax = Vector2.zero;
            var buttonsGroup = buttonsObj.GetComponent<CanvasGroup>();
            buttonsGroup.alpha = 0f;

            var retryBtn = CreateComicButton(
                "RetryButton",
                buttonsObj.transform,
                retrySprite,
                new Vector2(-367f, -301f),
                new Vector2(434f, 175f),
                hoverTilt: 10f);

            var mainMenuBtn = CreateComicButton(
                "MainMenuButton",
                buttonsObj.transform,
                mainMenuSprite,
                new Vector2(279f, -288f),
                new Vector2(566f, 201f),
                hoverTilt: -10f);

            var presentation = rootObj.GetComponent<EndScreenPresentation>();
            var serialized = new SerializedObject(presentation);
            serialized.FindProperty("root").objectReferenceValue = rootGroup;
            serialized.FindProperty("panelRect").objectReferenceValue = panelRect;
            serialized.FindProperty("panelImage").objectReferenceValue = panelImage;
            serialized.FindProperty("titleRect").objectReferenceValue = titleRect;
            serialized.FindProperty("titleImage").objectReferenceValue = titleImage;
            serialized.FindProperty("titleGroup").objectReferenceValue = titleGroup;
            serialized.FindProperty("buttonsGroup").objectReferenceValue = buttonsGroup;
            serialized.FindProperty("retryButton").objectReferenceValue = retryBtn;
            serialized.FindProperty("mainMenuButton").objectReferenceValue = mainMenuBtn;
            serialized.FindProperty("winPanelSprite").objectReferenceValue = winPanel;
            serialized.FindProperty("LosePanelSprite")?.SetValueIfNotNull(losePanel);
            serialized.FindProperty("losePanelSprite").objectReferenceValue = losePanel;
            serialized.FindProperty("victoryTitleSprite").objectReferenceValue = victory;
            serialized.FindProperty("loseTitleSprite").objectReferenceValue = lose;
            serialized.FindProperty("victoryTitleSize").vector2Value = new Vector2(1248f, 384f);
            serialized.FindProperty("loseTitleSize").vector2Value = new Vector2(743f, 310f);
            serialized.FindProperty("winSound").objectReferenceValue = gameWinCue;
            serialized.FindProperty("loseSound").objectReferenceValue = gameLoseCue;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(rootObj, PrefabPath);
        }
        finally
        {
            Object.DestroyImmediate(rootObj);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static Button CreateComicButton(
        string name,
        Transform parent,
        Sprite sprite,
        Vector2 anchoredPos,
        Vector2 size,
        float hoverTilt)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(ComicButton));
        obj.transform.SetParent(parent, false);

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var img = obj.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.raycastTarget = true;

        var btn = obj.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = img;

        var comic = obj.GetComponent<ComicButton>();
        var serializedComic = new SerializedObject(comic);
        serializedComic.FindProperty("image").objectReferenceValue = img;
        serializedComic.FindProperty("normalSprite").objectReferenceValue = sprite;
        serializedComic.FindProperty("hoverSprite").objectReferenceValue = sprite;
        serializedComic.FindProperty("hoverScale").floatValue = 1.08f;
        serializedComic.FindProperty("hoverTilt").floatValue = hoverTilt;
        serializedComic.FindProperty("punchDuration").floatValue = 0.16f;
        serializedComic.FindProperty("pressedScale").floatValue = 0.92f;
        serializedComic.FindProperty("pressDuration").floatValue = 0.06f;
        serializedComic.FindProperty("pulseAmount").floatValue = 0.03f;
        serializedComic.FindProperty("beatsPerMinute").floatValue = 120f;
        serializedComic.ApplyModifiedPropertiesWithoutUndo();

        return btn;
    }

    private static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault()
                     ?? AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            throw new System.InvalidOperationException($"Missing UI sprite at {path}.");
        return sprite;
    }

    private static void SetValueIfNotNull(this SerializedProperty prop, Object value)
    {
        if (prop != null) prop.objectReferenceValue = value;
    }

    private static AudioClip LoadClip(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
            throw new System.InvalidOperationException($"Missing AudioClip at {path}.");
        return clip;
    }

    private static AudioCue EnsureCue(
        string assetPath,
        AudioClip[] clips,
        float volume,
        Vector2 pitch,
        float cooldown,
        int maximumVoices,
        bool ui)
    {
        var cue = AssetDatabase.LoadAssetAtPath<AudioCue>(assetPath);
        if (cue == null)
        {
            cue = ScriptableObject.CreateInstance<AudioCue>();
            AssetDatabase.CreateAsset(cue, assetPath);
        }
        cue.clips = clips;
        cue.volume = volume;
        cue.pitch = pitch;
        cue.cooldown = cooldown;
        cue.maximumVoices = maximumVoices;
        cue.ui = ui;
        cue.loop = false;
        cue.loopStartSeconds = 0f;
        EditorUtility.SetDirty(cue);
        return cue;
    }

    private static void WirePlayerHitOnRig(AudioCue playerHitCue)
    {
        var rigRoot = PrefabUtility.LoadPrefabContents(GameplayRigPath);
        try
        {
            bool changed = false;
            foreach (var player in rigRoot.GetComponentsInChildren<PlayerHealth>(true))
            {
                var combatAudio = player.GetComponent<CombatAudio>();
                if (combatAudio != null)
                {
                    var s = new SerializedObject(combatAudio);
                    s.FindProperty("damaged").objectReferenceValue = playerHitCue;
                    s.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(combatAudio);
                    changed = true;
                }
                var feedback = player.GetComponent<PlayerDamageFeedback>();
                if (feedback != null)
                {
                    var s = new SerializedObject(feedback);
                    s.FindProperty("damageSound").objectReferenceValue = playerHitCue;
                    s.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(feedback);
                    changed = true;
                }
            }
            if (changed)
                PrefabUtility.SaveAsPrefabAsset(rigRoot, GameplayRigPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(rigRoot);
        }
    }
}
