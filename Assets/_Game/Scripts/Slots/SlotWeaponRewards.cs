using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class WeaponRewardPool
{
    private readonly List<WeaponData>[] pools = new List<WeaponData>[5];
    private readonly System.Random random;
    public string MissingTiers { get; }
    public bool IsComplete => MissingTiers.Length == 0;

    public WeaponRewardPool(IEnumerable<WeaponData> weapons, System.Random random = null)
    {
        this.random = random ?? new System.Random();
        for (int i = 0; i < pools.Length; i++) pools[i] = new List<WeaponData>();
        var seen = new HashSet<WeaponData>();
        if (weapons != null)
            foreach (var weapon in weapons)
                if (weapon != null && seen.Add(weapon) && (int)weapon.Tier >= 0 && (int)weapon.Tier < pools.Length)
                    pools[(int)weapon.Tier].Add(weapon);
        var missing = new List<string>();
        for (int i = 0; i < pools.Length; i++)
            if (pools[i].Count == 0) missing.Add(((SlotTier)i).ToString());
        MissingTiers = string.Join(", ", missing);
    }

    public WeaponData Pick(SlotTier tier)
    {
        if (!IsComplete) throw new InvalidOperationException("Weapon pool is incomplete: " + MissingTiers);
        var pool = pools[(int)tier];
        return pool[random.Next(pool.Count)];
    }
}

[DisallowMultipleComponent]
public sealed class SlotWeaponRewards : MonoBehaviour
{
    [SerializeField] private SlotMachineView machine;
    [Tooltip("Add manually created WeaponData assets. All five tiers need at least one unique weapon.")]
    [SerializeField] private WeaponData[] weapons = Array.Empty<WeaponData>();
    private ShootingController shooting;
    private WeaponRewardPool pool;
    private bool poolDirty = true;
    private int rewardedSpin;
    public WeaponData CurrentWeapon { get; private set; }
    public string MissingTiers => pool != null ? pool.MissingTiers : "Common, Uncommon, Rare, Epic, Jackpot";
    public event Action<WeaponData> WeaponAwarded;

    public void SetShootingController(ShootingController controller) { shooting = controller; }

    private void OnEnable()
    {
        if (machine == null) { Debug.LogError("SlotWeaponRewards requires a slot machine.", this); enabled = false; return; }
        machine.SpinCompleted += Award;
        RefreshPool();
    }

    private void OnValidate() { poolDirty = true; }
    private void Update() { if (poolDirty && !machine.IsSpinning) RefreshPool(); }

    private void RefreshPool()
    {
        pool = new WeaponRewardPool(weapons);
        poolDirty = false;
        machine.SetSpinAllowed(pool.IsComplete);
    }

    private void Award(SlotResult result)
    {
        if (result.SpinNumber <= rewardedSpin) return;
        rewardedSpin = result.SpinNumber;
        CurrentWeapon = pool.Pick(result.Tier);
        if (shooting != null) shooting.EquipWeapon(CurrentWeapon);
        WeaponAwarded?.Invoke(CurrentWeapon);
    }

    private void OnDisable()
    {
        if (machine == null) return;
        machine.SpinCompleted -= Award;
        machine.SetSpinAllowed(false);
    }
}
