using UnityEngine;

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
    private WeaponData weapon;
    private Vector2 recoil;
    private Vector3 appliedShake;
    private float pulse;
    private Vector2 weaponOrigin;

    public void Configure(Canvas hud, RectTransform aim, UnityEngine.UI.Image image, Transform camera)
    {
        canvas = hud;
        crosshair = aim;
        weaponImage = image;
        cameraTransform = camera;
    }

    private void Awake() { weaponOrigin = weaponImage.rectTransform.anchoredPosition; }
    public void SetWeapon(WeaponData data)
    {
        ResetFeedback();
        weapon = data;
        weaponImage.sprite = data.Sprite;
    }
    public Vector2 UpdateAim(Vector2 pointer, float deltaTime)
    {
        RemoveShake();
        recoil = weapon.EnableRecoil
            ? Vector2.MoveTowards(recoil, Vector2.zero, weapon.Recovery * deltaTime)
            : Vector2.zero;
        float scale = canvas.scaleFactor;
        Vector2 aim = pointer + recoil * scale;
        aim.x = Mathf.Clamp(aim.x, 0f, Screen.width);
        aim.y = Mathf.Clamp(aim.y, 0f, Screen.height);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, aim, null, out Vector2 local);
        crosshair.anchoredPosition = local;
        return aim;
    }
    public void PlayShot(bool hit)
    {
        if (weapon.EnableRecoil)
        {
            recoil = Vector2.up * Mathf.Min(recoil.y + weapon.UpwardKick, weapon.MaximumRecoil);
            pulse = 1f;
        }
        crosshair.GetComponent<UnityEngine.UI.Image>().color = hit ? Color.red : Color.white;
    }
    private void LateUpdate()
    {
        RemoveShake();
        if (weapon != null && !weapon.EnableRecoil)
            pulse = 0f;
        pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 8f);
        weaponImage.rectTransform.anchoredPosition = weaponOrigin + Vector2.down * (pulse * 25f);
        appliedShake = Vector3.up * (pulse * cameraKick);
        cameraTransform.localPosition += appliedShake;
    }
    private void RemoveShake()
    {
        if (cameraTransform != null) cameraTransform.localPosition -= appliedShake;
        appliedShake = Vector3.zero;
    }
    public void ResetFeedback()
    {
        RemoveShake();
        recoil = Vector2.zero;
        pulse = 0f;
    }
    private void OnDisable() { ResetFeedback(); }
}
