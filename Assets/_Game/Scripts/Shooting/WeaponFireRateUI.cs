using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class WeaponFireRateUI : MonoBehaviour
{
    [Header("Controller")]
    [SerializeField] private ShootingController shootingController;

    [Header("UI References")]
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Behavior")]
    [Tooltip("If true, only shown when weapon is SemiAutomatic (single shot).")]
    [SerializeField] private bool onlySemiAutomatic = true;

    [Tooltip("Color of fill bar while cooling down between taps.")]
    [SerializeField] private Color cooldownColor = new Color(1f, 0.72f, 0.05f, 0.95f); // Amber / orange

    [Tooltip("Color of fill bar when ready to fire.")]
    [SerializeField] private Color readyColor = new Color(0.1f, 0.95f, 0.45f, 1f); // Vibrant green

    public void Configure(ShootingController controller, Slider sliderRef, Image fillRef = null, CanvasGroup cg = null, TextMeshProUGUI text = null)
    {
        shootingController = controller;
        slider = sliderRef;
        fillImage = fillRef;
        canvasGroup = cg;
        statusText = text;
        UpdateVisuals();
    }

    private void Awake()
    {
        if (shootingController == null)
            shootingController = GetComponentInParent<ShootingController>() ?? FindFirstObjectByType<ShootingController>();
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        if (shootingController == null)
        {
            shootingController = FindFirstObjectByType<ShootingController>();
            if (shootingController == null)
            {
                SetVisible(false);
                return;
            }
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        var weapon = shootingController.CurrentWeapon;
        if (weapon == null)
        {
            SetVisible(false);
            return;
        }

        bool isSemi = shootingController.IsSemiAutomatic;
        if (onlySemiAutomatic && !isSemi)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        float progress = shootingController.FireCooldownProgress;
        bool isReady = progress >= 0.999f;

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = progress;
        }

        Color targetColor = isReady ? readyColor : cooldownColor;
        if (fillImage != null)
        {
            fillImage.color = targetColor;
        }

        if (statusText != null)
        {
            statusText.text = isReady ? "READY" : "TAP COOLDOWN";
            statusText.color = targetColor;
        }
    }

    private void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.blocksRaycasts = false;
        }
        else if (gameObject.activeSelf != visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
