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
    private int ammo;
    private bool ammoInitialized; // OnEnable refills only the initial weapon, not on every re-enable

    public event Action ShotFired;
    public event Action<WeaponData> WeaponEquipped;
    /// <summary>Raised after a shot spends ammo and when a weapon refills it.</summary>
    public event Action AmmoChanged;

    public WeaponData CurrentWeapon => weapon;
    /// <summary>Remaining shots for the current weapon; ignore when the weapon has unlimited ammo.</summary>
    public int Ammo => ammo;
    /// <summary>False once a limited weapon runs dry: the player can still aim and take damage but not attack.</summary>
    public bool HasAmmo => weapon == null || weapon.UnlimitedAmmo || ammo > 0;
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
        if (!ammoInitialized) Refill();
        previousCursorVisible = Cursor.visible;
        ownsCursor = true;
        cursorMode = false;
        hasAim = false;
        UpdateCursorVisibility();
    }

    private void Update()
    {
        bool overUI = UnityEngine.EventSystems.EventSystem.current != null
            && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        bool escape = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        if (feedback != null) feedback.SetContinuousFire(shootingAllowed && !cursorMode
            && Mouse.current != null && Application.isFocused && Time.timeScale > 0f
            && !escape && !overUI && fire != null && weapon != null && HasAmmo
            && weapon.FiringMode == WeaponData.FireMode.Automatic && fire.IsPressed());
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
        if (!wantsToFire || !HasAmmo || Time.timeAsDouble < nextShot)
            return;
        nextShot = Time.timeAsDouble + weapon.ShotInterval;
        if (!weapon.UnlimitedAmmo)
        {
            ammo--;
            AmmoChanged?.Invoke();
        }
        ShotFired?.Invoke();
        // Camera shake is removed before Update; hit selection precedes this shot's recoil.
        // The weapon's damage type decides which targets this shot reaches (single, piercing, explosion, cone...).
        bool hit = weapon.DamageType.Apply(new ShotContext(gameplayCamera, aim, weapon.Damage, hitMask));
        feedback.PlayShot(hit);
    }

    public void EquipWeapon(WeaponData data)
    {
        if (data == null) return;
        weapon = data;
        if (feedback != null) feedback.SetWeapon(data);
        Refill();
        WeaponEquipped?.Invoke(data);
    }

    private void Refill()
    {
        ammoInitialized = true;
        ammo = weapon != null ? weapon.MaxAmmo : 0;
        AmmoChanged?.Invoke();
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
