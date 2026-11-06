using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public sealed class ShootingController : MonoBehaviour
{
    [Header("Required References")]
    [Tooltip("Orthographic gameplay camera used for target selection.")]
    [SerializeField] private Camera gameplayCamera;
    [Tooltip("Initial weapon configuration. Runtime state never modifies this asset.")]
    [SerializeField] private WeaponData weapon;
    [Tooltip("Presentation component responsible for recoil and weapon visuals.")]
    [SerializeField] private WeaponFeedback feedback;
    [Tooltip("Existing Player/Attack action; enabled through a private action clone.")]
    [SerializeField] private InputActionReference attack;
    [Header("Hit Detection")]
    [Tooltip("Only colliders on these layers can block shots or receive damage.")]
    [SerializeField] private LayerMask hitMask = ~0;
    private InputAction fire;
    private double nextShot;
    private bool previousCursorVisible;
    private bool ownsCursor;

    private void OnEnable()
    {
        if (gameplayCamera == null || weapon == null || feedback == null || attack == null)
        {
            Debug.LogError("ShootingController requires camera, weapon, feedback and Attack references.", this);
            enabled = false;
            return;
        }
        fire = attack.action.Clone();
        fire.Enable();
        feedback.SetWeapon(weapon);
        previousCursorVisible = Cursor.visible;
        ownsCursor = true;
        UpdateCursorVisibility();
    }

    private void Update()
    {
        UpdateCursorVisibility();
        if (Time.timeScale <= 0f || !Application.isFocused || Mouse.current == null)
            return;
        Vector2 aim = feedback.UpdateAim(Mouse.current.position.ReadValue(), Time.deltaTime);
        bool wantsToFire = weapon.FiringMode == WeaponData.FireMode.Automatic
            ? fire.IsPressed()
            : fire.WasPressedThisFrame();
        if (!wantsToFire || Time.timeAsDouble < nextShot)
            return;
        nextShot = Time.timeAsDouble + weapon.ShotInterval;
        // Camera shake is removed before Update; hit selection precedes this shot's recoil.
        Ray ray = gameplayCamera.ScreenPointToRay(aim);
        RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, hitMask);
        if (hit.collider != null)
        {
            IDamageable target = hit.collider.GetComponentInParent<IDamageable>();
            target?.TakeDamage(weapon.Damage);
        }
        feedback.PlayShot(hit.collider != null);
    }

    public void EquipWeapon(WeaponData data)
    {
        if (data == null) return;
        weapon = data;
        if (feedback != null) feedback.SetWeapon(data);
    }

    private void UpdateCursorVisibility()
    {
        if (ownsCursor)
            Cursor.visible = !Application.isFocused || Time.timeScale <= 0f;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (ownsCursor)
            Cursor.visible = !hasFocus || Time.timeScale <= 0f;
    }

    private void OnDisable()
    {
        if (ownsCursor)
        {
            Cursor.visible = previousCursorVisible;
            ownsCursor = false;
        }
        fire?.Dispose();
        fire = null;
        if (feedback != null) feedback.ResetFeedback();
    }
}
