using UnityEngine;
using PrimeTween;

[DisallowMultipleComponent]
public sealed class WeaponFeedback : MonoBehaviour
{
    [Header("Presentation References")]
    [Tooltip("Screen Space Overlay canvas containing the weapon and crosshair.")]
    [SerializeField] private Canvas canvas;
    [Tooltip("Crosshair RectTransform, anchored to the canvas center.")]
    [SerializeField] private RectTransform crosshair;
    [Tooltip("Weapon image with its own centered RectTransform.")]
    [SerializeField] private UnityEngine.UI.Image weaponImage;
    [Tooltip("Camera transform receiving a temporary visual offset.")]
    [SerializeField] private Transform cameraTransform;

    [Header("Feedback")]
    [Tooltip("Maximum camera translation in world units per shot.")]
    [SerializeField, Min(0f)] private float cameraKick = 0.035f;

    [Header("Equip Animation")]
    [SerializeField] private CombatHudAnimationProfile animationProfile;
    [Tooltip("Dedicated parent of the weapon image. Recoil remains on the image child.")]
    [SerializeField] private RectTransform equipRoot;
    [SerializeField] private WeaponStickerPresentation sticker;

    [Header("Aim & Sway Presentation")]
    [Tooltip("Base position offset for the weapon (negative X moves weapon left).")]
    [SerializeField] private Vector2 weaponOffset = new Vector2(-50f, 0f);

    [Tooltip("Maximum horizontal sway offset in pixels when crosshair moves (+70% boost).")]
    [SerializeField, Min(0f)] private float maxSwayX = 42.5f;

    [Tooltip("Maximum vertical sway offset in pixels when crosshair moves.")]
    [SerializeField, Min(0f)] private float maxSwayY = 18f;

    [Tooltip("Speed of positional sway interpolation.")]
    [SerializeField, Min(0.1f)] private float swaySpeed = 10f;

    [Tooltip("Multiplier for how much the muzzle rotates towards the crosshair.")]
    [SerializeField, Range(0.1f, 2f)] private float aimRotationWeight = 1.0f;

    [Tooltip("Maximum rotation angle in degrees from base resting orientation.")]
    [SerializeField, Range(5f, 60f)] private float maxAimAngle = 40f;

    [Tooltip("Speed of aim rotation smoothing.")]
    [SerializeField, Min(0.1f)] private float aimRotationSpeed = 16f;

    private Tween equipTween;
    private Tween hitmarkerTween;
    private UnityEngine.UI.Image crosshairImage;
    private Vector3 crosshairScale;
    private Vector2 equipPosition;
    private Vector3 equipScale;
    private WeaponVisual visualInstance;
    private WeaponShotPresentation shotPresentation;
    private WeaponData weapon;
    private Vector2 recoil;
    private Vector3 appliedShake;
    private CombatCameraFeedback combatCamera;
    private float weaponKick;
    private Vector2 rawOrigin;
    private Vector2 weaponOrigin;
    private Quaternion weaponRotation;
    private bool feedbackInitialized;

    private Vector2 currentSway;
    private float currentAimAngle;

    public void Configure(Canvas hud, RectTransform aim, UnityEngine.UI.Image image, Transform camera)
    {
        canvas = hud;
        crosshair = aim;
        weaponImage = image;
        cameraTransform = camera;
    }

    private void Awake() { InitializeFeedback(); }

    private void InitializeFeedback()
    {
        if (feedbackInitialized) return;
        feedbackInitialized = true;
        combatCamera = cameraTransform.GetComponent<CombatCameraFeedback>() ?? cameraTransform.gameObject.AddComponent<CombatCameraFeedback>();
        rawOrigin = weaponImage.rectTransform.anchoredPosition;
        weaponOrigin = rawOrigin + weaponOffset;
        weaponRotation = weaponImage.rectTransform.localRotation;
        crosshairImage = crosshair.GetComponent<UnityEngine.UI.Image>();
        crosshairScale = crosshair.localScale;
        if (equipRoot != null) { equipPosition = equipRoot.anchoredPosition; equipScale = equipRoot.localScale; }
    }

    public void SetWeapon(WeaponData data)
    {
        ResetFeedback();
        if (visualInstance != null)
        {
            visualInstance.gameObject.SetActive(false);
            Destroy(visualInstance.gameObject);
            visualInstance = null;
        }
        weapon = data;
        if (sticker != null) sticker.SetWeapon(data);
        weaponImage.sprite = data.Sprite;
        weaponImage.enabled = data.VisualPrefab == null;
        if (data.VisualPrefab != null)
        {
            visualInstance = Instantiate(data.VisualPrefab, weaponImage.transform, false);
            var visualRect = (RectTransform)visualInstance.transform;
            visualRect.anchorMin = Vector2.zero;
            visualRect.anchorMax = Vector2.one;
            visualRect.offsetMin = visualRect.offsetMax = Vector2.zero;
            visualInstance.Initialize(data.Sprite);
        }
        if (shotPresentation == null && data.ShotVisualProfile != null)
        {
            var effects = new GameObject("Weapon Shot Effects", typeof(RectTransform));
            effects.transform.SetParent(canvas.transform, false);
            // Draw right above the weapon HUD, not as the last child, so later overlays (slot machine) stay on top.
            Transform hudRoot = weaponImage.transform;
            while (hudRoot.parent != null && hudRoot.parent != canvas.transform) hudRoot = hudRoot.parent;
            if (hudRoot.parent == canvas.transform) effects.transform.SetSiblingIndex(hudRoot.GetSiblingIndex() + 1);
            var rect = (RectTransform)effects.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            shotPresentation = effects.AddComponent<WeaponShotPresentation>();
            shotPresentation.Initialize(canvas, weaponImage.rectTransform);
        }
        if (shotPresentation != null)
            shotPresentation.SetWeapon(data.ShotVisualProfile, visualInstance != null ? visualInstance.MuzzleAnchor : null);
        if (sticker != null) sticker.transform.SetAsLastSibling();
        PlayEquip();
    }

    public void PlayEquip()
    {
        InitializeFeedback();
        equipTween.Stop();
        if (equipRoot != null && animationProfile != null && animationProfile.AnimateEquip && isActiveAndEnabled)
        {
            equipTween = Tween.Custom(0f, 1f, animationProfile.EquipDuration, t =>
            {
                equipRoot.anchoredPosition = equipPosition + Vector2.down * (animationProfile.EquipOffset * (1f - t));
                equipRoot.localScale = equipScale * Mathf.Lerp(0.9f, 1f, t);
            }, ease: Ease.OutBack, useUnscaledTime: true);
        }
    }

    public Vector2 UpdateAim(Vector2 pointer, float deltaTime)
    {
        RemoveShake();
        recoil = (weapon != null && weapon.EnableRecoil)
            ? Vector2.MoveTowards(recoil, Vector2.zero, weapon.Recovery * deltaTime)
            : Vector2.zero;

        float scale = canvas != null ? canvas.scaleFactor : 1f;
        Vector2 aim = pointer + recoil * scale;
        aim.x = Mathf.Clamp(aim.x, 0f, Screen.width);
        aim.y = Mathf.Clamp(aim.y, 0f, Screen.height);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, aim, null, out Vector2 local);
        crosshair.anchoredPosition = local;
        if (shotPresentation != null) shotPresentation.SetAim(aim);

        float dt = deltaTime > 0f ? deltaTime : Time.unscaledDeltaTime;
        if (dt <= 0f) dt = 0.016f;

        // Base resting position with inspector offset
        weaponOrigin = rawOrigin + weaponOffset;

        // Subtle positional sway (+70% horizontal sway distance)
        Vector2 canvasSize = ((RectTransform)canvas.transform).rect.size;
        float halfW = canvasSize.x > 0f ? canvasSize.x * 0.5f : 960f;
        float halfH = canvasSize.y > 0f ? canvasSize.y * 0.5f : 540f;
        float normX = Mathf.Clamp(local.x / halfW, -1f, 1f);
        float normY = Mathf.Clamp(local.y / halfH, -1f, 1f);
        Vector2 targetSway = new Vector2(normX * maxSwayX, normY * maxSwayY);
        currentSway = Vector2.Lerp(currentSway, targetSway, dt * swaySpeed);

        // Muzzle points towards crosshair
        Vector2 weaponAnchor = weaponOrigin + currentSway;
        Vector2 toCrosshair = local - weaponAnchor;
        Vector2 centerDir = -weaponOrigin;
        if (centerDir.sqrMagnitude < 0.001f) centerDir = Vector2.up;

        float baseAngle = Mathf.Atan2(centerDir.y, centerDir.x) * Mathf.Rad2Deg;
        float aimAngle = Mathf.Atan2(toCrosshair.y, toCrosshair.x) * Mathf.Rad2Deg;
        float deltaAngle = Mathf.DeltaAngle(baseAngle, aimAngle);
        float targetZ = Mathf.Clamp(deltaAngle * aimRotationWeight, -maxAimAngle, maxAimAngle);
        currentAimAngle = Mathf.LerpAngle(currentAimAngle, targetZ, dt * aimRotationSpeed);

        return aim;
    }

    public void SetContinuousFire(bool value)
    {
        if (shotPresentation != null) shotPresentation.SetContinuousFire(value);
        if (visualInstance != null) visualInstance.SetContinuousFire(value);
        if (sticker != null) sticker.SetContinuousFire(value);
    }

    public void PlayShot(bool hit)
    {
        if (visualInstance != null) visualInstance.PlayShot();
        if (sticker != null) sticker.PlayShot();
        if (weapon != null && weapon.EnableRecoil && combatCamera != null) combatCamera.Play(cameraKick, 0.08f);
        if (weapon != null && weapon.EnableRecoil)
        {
            recoil = Vector2.up * Mathf.Min(recoil.y + weapon.UpwardKick, weapon.MaximumRecoil);
            weaponKick = 18f;
        }
        if (hit) PlayHitmarker();
    }

    public void PlayHitmarker()
    {
        hitmarkerTween.Stop();
        hitmarkerTween = Tween.Custom(0f, 1f, 0.15f, t =>
        {
            crosshair.localScale = crosshairScale * (1f + 0.4f * (1f - t));
            if (crosshairImage != null) crosshairImage.color = Color.Lerp(Color.red, Color.white, t);
        }, ease: Ease.OutQuad, useUnscaledTime: true);
    }

    public float GetProjectileTravelDuration(in ShotVisualContext shot) => shotPresentation.GetProjectileTravelDuration(shot);

    public void PlayShot(bool hit, in ShotVisualContext shot, ProjectileShot projectile = null)
    {
        if (shotPresentation != null) shotPresentation.PlayShot(shot, projectile);
        PlayShot(hit);
    }

    private void LateUpdate()
    {
        RemoveShake();
        if (weapon != null && !weapon.EnableRecoil) weaponKick = 0f;
        weaponImage.rectTransform.anchoredPosition = weaponOrigin + currentSway + Vector2.up * weaponKick;
        weaponImage.rectTransform.localRotation = Quaternion.Euler(0f, 0f, currentAimAngle);
        weaponKick = Mathf.MoveTowards(weaponKick, 0f, Time.deltaTime * 144f);
        appliedShake = Vector3.zero;
        cameraTransform.localPosition += appliedShake;
    }

    private void RemoveShake()
    {
        if (cameraTransform != null) cameraTransform.localPosition -= appliedShake;
        appliedShake = Vector3.zero;
    }

    public void ResetFeedback()
    {
        InitializeFeedback();
        if (shotPresentation != null) shotPresentation.SetContinuousFire(false);
        if (visualInstance != null) visualInstance.ResetEffects();
        if (sticker != null) sticker.Cancel();
        equipTween.Stop();
        if (equipRoot != null) { equipRoot.anchoredPosition = equipPosition; equipRoot.localScale = equipScale; }
        hitmarkerTween.Stop();
        if (crosshair != null) crosshair.localScale = crosshairScale;
        if (crosshairImage != null) crosshairImage.color = Color.white;
        weaponOrigin = rawOrigin + weaponOffset;
        if (weaponImage != null)
        {
            weaponImage.rectTransform.anchoredPosition = weaponOrigin;
            weaponImage.rectTransform.localRotation = weaponRotation;
        }
        currentSway = Vector2.zero;
        currentAimAngle = 0f;
        RemoveShake();
        recoil = Vector2.zero;
        weaponKick = 0f;
    }

    private void OnDisable() { ResetFeedback(); }
    private void OnDestroy()
    {
        if (shotPresentation != null) Destroy(shotPresentation.gameObject);
    }
}
