using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;

public sealed class WeaponTestController : MonoBehaviour
{
    [SerializeField] private ShootingController shooting;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text information;
    [SerializeField] private Button testButton;
    [SerializeField] private WeaponData[] weapons;
    private readonly List<WeaponData> catalog = new List<WeaponData>();
    private bool open;
    private void Start()
    {
        if (shooting == null || dropdown == null || panel == null || information == null || testButton == null)
        { Debug.LogError("Weapon test requires its UI and shooting references.", this); enabled = false; return; }
        if (weapons != null) foreach (var weapon in weapons) if (weapon != null && !catalog.Contains(weapon)) catalog.Add(weapon);
        catalog.Sort((a, b) => a.Tier != b.Tier ? a.Tier.CompareTo(b.Tier) : string.CompareOrdinal(a.name, b.name));
        dropdown.ClearOptions();
        var names = new List<string>();
        foreach (var weapon in catalog) names.Add($"[{weapon.Tier}] {weapon.name}");
        dropdown.AddOptions(names);
        dropdown.onValueChanged.AddListener(Select);
        testButton.onClick.AddListener(BeginTest);
        testButton.interactable = catalog.Count > 0;
        dropdown.interactable = catalog.Count > 0;
        int initial = Mathf.Max(0, catalog.FindIndex(w => w.name == "Batu_Biasa"));
        dropdown.SetValueWithoutNotify(initial);
        if (catalog.Count > 0) Select(initial); else information.text = "Weapon catalog is empty. Refresh Weapon Catalog in the Editor.";
        SetPanel(true);
    }
    private void Select(int index)
    {
        if (index < 0 || index >= catalog.Count) return;
        var weapon = catalog[index];
        shooting.EquipWeapon(weapon);
        information.text = $"{weapon.name} | {weapon.Tier}\nDamage: {weapon.Damage} | {weapon.FiringMode}\nShots/s: {1f / weapon.ShotInterval:0.##} | Recoil: {(weapon.EnableRecoil ? "On" : "Off")}";
    }
    private void Update()
    {
        if (shooting != null && Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame) SetPanel(!open);
    }
    private void BeginTest() { if (catalog.Count > 0) SetPanel(false); }
    private void SetPanel(bool value) { open = value; panel.SetActive(value); shooting.SetShootingAllowed(!value); }
    private void OnEnable() { if (catalog.Count > 0) { dropdown.onValueChanged.AddListener(Select); testButton.onClick.AddListener(BeginTest); SetPanel(true); } }
    private void OnDisable()
    {
        if (dropdown != null) dropdown.onValueChanged.RemoveListener(Select);
        if (testButton != null) testButton.onClick.RemoveListener(BeginTest);
        if (shooting != null && Application.isPlaying) shooting.SetShootingAllowed(true);
    }
}
