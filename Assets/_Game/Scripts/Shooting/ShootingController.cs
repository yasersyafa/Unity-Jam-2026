using System;
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
    private bool cursorMode;
    private Vector2 lastAim;
    private bool hasAim;
    private bool shootingAllowed = true;
    private int resumeFrame;

    public event Action ShotFired;
    public event Action<WeaponData> WeaponEquipped;

    public WeaponData CurrentWeapon => weapon;
    public double NextShotTime => nextShot;
    public float ShotInterval => weapon != null ? weapon.ShotInterval : 0f;
    public bool IsSemiAutomatic => weapon != null && weapon.FiringMode == WeaponData.FireMode.SemiAutomatic;

    public float FireCooldownProgress
    {
        get
        {
            if (weapon == null || weapon.ShotInterval <= 0f) return 1f;
            double remaining = nextShot - Time.timeAsDouble;
            if (remaining <= 0) return 1f;
            return 1f - Mathf.Clamp01((float)(remaining / weapon.ShotInterval));
        }
    }

    public void Configure(Camera camera, WeaponData initialWeapon, WeaponFeedback presentation, InputActionReference action)
    {
        gameplayCamera = camera;
        weapon = initialWeapon;
        feedback = presentation;
        attack = action;
    }

    public void SetShootingAllowed(bool allowed)
    {
        shootingAllowed = allowed;
        cursorMode = true;
        resumeFrame = Time.frameCount;
        if (feedback != null)
        {
            feedback.ResetFeedback();
            if (allowed) feedback.PlayEquip();
        }
        UpdateCursorVisibility();
    }

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
        cursorMode = false;
        hasAim = false;
        UpdateCursorVisibility();
    }

    private void Update()
    {
        if (!shootingAllowed || Time.frameCount == resumeFrame) { UpdateCursorVisibility(); return; }
        if (Mouse.current == null) return;
        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool suspended = Time.timeScale <= 0f || !Application.isFocused;
        if (escapePressed || suspended)
        {
            if (!cursorMode && hasAim && Application.isFocused) Mouse.current.WarpCursorPosition(lastAim);
            cursorMode = true;
            feedback.ResetFeedback();
            UpdateCursorVisibility();
            return;
        }
        if (cursorMode)
        {
            lastAim = feedback.UpdateAim(Mouse.current.position.ReadValue(), 0f);
            hasAim = true;
            if (!fire.WasPressedThisFrame()) { UpdateCursorVisibility(); return; }
            if (UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
            cursorMode = false;
            hasAim = false;
        }
        UpdateCursorVisibility();
        Vector2 aim = feedback.UpdateAim(Mouse.current.position.ReadValue(), Time.deltaTime);
        lastAim = aim;
        hasAim = true;
        bool wantsToFire = weapon.FiringMode == WeaponData.FireMode.Automatic
            ? fire.IsPressed()
            : fire.WasPressedThisFrame();
        if (UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        if (!wantsToFire || Time.timeAsDouble < nextShot)
            return;
        nextShot = Time.timeAsDouble + weapon.ShotInterval;
        ShotFired?.Invoke();
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
        WeaponEquipped?.Invoke(data);
    }

    private void UpdateCursorVisibility()
    {
        if (ownsCursor)
            Cursor.visible = !shootingAllowed || cursorMode || !Application.isFocused || Time.timeScale <= 0f;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (ownsCursor)
            Cursor.visible = !shootingAllowed || cursorMode || !hasFocus || Time.timeScale <= 0f;
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
