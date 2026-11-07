using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(SlotWeaponRewards))]
public sealed class SlotWeaponDemo : MonoBehaviour
{
    [SerializeField] private SlotMachineView machine;
    [SerializeField] private Camera gameplayCamera;
    [SerializeField] private WeaponData initialWeapon;
    [SerializeField] private InputActionReference attack;
    private SlotWeaponRewards rewards;
    private ShootingController shooting;
    private GameObject weaponHud;
    private UnityEngine.UI.Button tryButton;
    private UnityEngine.UI.Button openButton;
    private UnityEngine.UI.Image rewardImage;
    private TextMeshProUGUI rewardText;
    private Sprite targetSprite;
    private Texture2D targetTexture;

    private void Start()
    {
        rewards = GetComponent<SlotWeaponRewards>();
        if (machine == null || gameplayCamera == null || initialWeapon == null || attack == null)
        {
            Debug.LogError("SlotWeaponDemo requires machine, camera, initial weapon and Attack references.", this);
            enabled = false;
            return;
        }
        gameplayCamera.orthographic = true;
        gameplayCamera.orthographicSize = 5;
        var canvasObject = new GameObject("WeaponDemoCanvas", typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        weaponHud = new GameObject("WeaponHUD", typeof(RectTransform));
        weaponHud.transform.SetParent(canvas.transform, false);
        var crosshair = Image("Crosshair", weaponHud.transform, new Vector2(18, 18), Vector2.zero, Color.white);
        var image = Image("Weapon", weaponHud.transform, new Vector2(230, 190), new Vector2(0, -240), Color.white);
        image.preserveAspect = true;
        var controllerObject = new GameObject("DemoShooting");
        controllerObject.transform.SetParent(transform, false);
        controllerObject.SetActive(false);
        var feedback = controllerObject.AddComponent<WeaponFeedback>();
        feedback.Configure(canvas, crosshair.rectTransform, image, gameplayCamera.transform);
        shooting = controllerObject.AddComponent<ShootingController>();
        shooting.Configure(gameplayCamera, initialWeapon, feedback, attack);
        controllerObject.SetActive(true);
        rewards.SetShootingController(shooting);

        rewardText = Label("Reward", canvas.transform, new Vector2(900, 58), new Vector2(0, 270), 21);
        rewardImage = Image("RewardSprite", canvas.transform, new Vector2(90, 70), new Vector2(-480, 270), Color.white);
        rewardImage.preserveAspect = true;
        rewardImage.enabled = false;
        tryButton = Button("Coba Weapon", canvas.transform, new Vector2(470, -230), () => SetPanel(false));
        openButton = Button("Buka Slot", canvas.transform, new Vector2(470, 300), () => SetPanel(true));
        CreateTargets();
        SetPanel(true);
    }

    private void Update()
    {
        if (shooting == null) return;
        var weapon = rewards.CurrentWeapon;
        string text = weapon != null ? $"{weapon.name} | {weapon.Tier} | Durasi: {weapon.UsageDuration:0.##} detik" : "Belum mendapat weapon";
        if (!string.IsNullOrEmpty(rewards.MissingTiers)) text += "\nIsi WeaponData: " + rewards.MissingTiers;
        if (rewardText.text != text) rewardText.text = text;
        rewardImage.sprite = weapon != null ? weapon.Sprite : null;
        rewardImage.enabled = rewardImage.sprite != null;
        tryButton.interactable = weapon != null && !machine.IsSpinning;
    }

    private void SetPanel(bool visible)
    {
        if (!visible && (rewards.CurrentWeapon == null || machine.IsSpinning)) return;
        machine.SetPanelVisible(visible);
        shooting.SetShootingAllowed(!visible);
        weaponHud.SetActive(!visible);
        tryButton.gameObject.SetActive(visible);
        openButton.gameObject.SetActive(!visible);
    }

    private void CreateTargets()
    {
        targetTexture = new Texture2D(1, 1);
        targetTexture.SetPixel(0, 0, Color.white);
        targetTexture.Apply();
        targetSprite = Sprite.Create(targetTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        for (int i = 0; i < 3; i++)
        {
            var target = new GameObject("PracticeTarget" + (i + 1), typeof(SpriteRenderer), typeof(BoxCollider2D));
            target.transform.SetParent(transform, false);
            target.transform.position = gameplayCamera.ViewportToWorldPoint(new Vector3(0.25f + 0.25f * i, 0.6f, 10f));
            target.GetComponent<SpriteRenderer>().sprite = targetSprite;
            target.GetComponent<BoxCollider2D>().size = Vector2.one;
            target.AddComponent<ShootingDummy>();
        }
    }

    private static UnityEngine.UI.Image Image(string name, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform)).AddComponent<UnityEngine.UI.Image>();
        image.transform.SetParent(parent, false);
        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = position;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI Label(string name, Transform parent, Vector2 size, Vector2 position, float fontSize)
    {
        var label = new GameObject(name, typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent, false);
        label.rectTransform.sizeDelta = size;
        label.rectTransform.anchoredPosition = position;
        label.font = TMP_Settings.defaultFontAsset;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static UnityEngine.UI.Button Button(string title, Transform parent, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var image = Image(title, parent, new Vector2(230, 52), position, new Color(0.25f, 0.4f, 0.65f));
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(action);
        Label("Label", image.transform, new Vector2(230, 52), Vector2.zero, 24).text = title;
        return button;
    }

    private void OnDisable() { if (shooting != null) shooting.SetShootingAllowed(false); }
    private void OnDestroy()
    {
        if (targetSprite != null) Destroy(targetSprite);
        if (targetTexture != null) Destroy(targetTexture);
    }
}
