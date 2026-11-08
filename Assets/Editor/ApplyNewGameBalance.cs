using UnityEditor;
using UnityEngine;

public static class ApplyNewGameBalance
{
    [MenuItem("Tools/Apply New Game Balance")]
    public static void Execute()
    {
        // === 1. WEAPONS ===
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Common/Bambu_Runcing.asset", 100f, 1.0f, false, 0f, 0f, 0f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Common/Batu_Biasa.asset", 20f, 3.5f, true, 6f, 30f, 120f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Common/Sandal_Slipper.asset", 15f, 4.5f, true, 6f, 30f, 120f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Common/_StandarWeapon.asset", 20f, 3.5f, true, 6f, 30f, 120f);

        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Uncommon/Nerf_Gun.asset", 15f, 8.0f, true, 6f, 30f, 120f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Uncommon/Ketapel.asset", 80f, 1.5f, true, 6f, 30f, 120f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Uncommon/Water_Gun.asset", 10f, 10.0f, false, 0f, 0f, 0f);

        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Rare/Durian.asset", 130f, 1.0f, false, 0f, 0f, 0f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Rare/LPG.asset", 125f, 1.3f, false, 0f, 0f, 0f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Rare/Glock_Pistol.asset", 60f, 4.5f, true, 18f, 90f, 150f);

        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Epic/Uzi.asset", 15f, 15.0f, true, 36f, 150f, 180f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Epic/M4A1.asset", 35f, 10.0f, true, 18f, 90f, 150f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Epic/Flamethrower.asset", 10f, 20.0f, true, 6f, 30f, 120f);

        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Jackpot/AWP.asset", 2500f, 1.5f, true, 65f, 200f, 200f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Jackpot/Machine_Gun_M134-A2.asset", 100f, 25.0f, true, 65f, 200f, 200f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Jackpot/Rocket_Launcher.asset", 1000f, 1.5f, true, 45f, 160f, 160f);
        ApplyWeapon("Assets/_Game/ScriptableObjects/Weapons/Jackpot/Jackpot.asset", 1000f, 1.5f, true, 45f, 160f, 160f);

        // === 2. ENEMIES ===
        ApplyEnemy("Assets/_Game/ScriptableObjects/Enemies/NormalEnemyData.asset", 100f, 10f, 6.0f);
        ApplyEnemy("Assets/_Game/ScriptableObjects/Enemies/RangedEnemyData.asset", 50f, 5f, 6.0f);
        ApplyEnemy("Assets/_Game/ScriptableObjects/Enemies/TankEnemyData.asset", 200f, 15f, 9.0f);
        ApplyEnemy("Assets/_Game/ScriptableObjects/Enemies/ChargedEnemyData.asset", 75f, 10f, 3.5f);

        AssetDatabase.SaveAssets();
        Debug.Log("Successfully applied all new Weapon and Enemy balance attributes!");
    }

    private static void ApplyWeapon(string path, float damage, float fireRate, bool recoil, float kick, float maxRecoil, float recovery)
    {
        var weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
        if (weapon == null)
        {
            Debug.LogWarning($"Weapon asset not found at: {path}");
            return;
        }

        var so = new SerializedObject(weapon);
        so.FindProperty("damage").floatValue = damage;
        so.FindProperty("fireRate").floatValue = fireRate;
        so.FindProperty("enableRecoil").boolValue = recoil;
        so.FindProperty("upwardKick").floatValue = kick;
        so.FindProperty("maximumRecoil").floatValue = maxRecoil;
        so.FindProperty("recovery").floatValue = recovery;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(weapon);
        Debug.Log($"Updated Weapon: {weapon.name} -> Dmg: {damage}, FR: {fireRate}, Recoil: {recoil}");
    }

    private static void ApplyEnemy(string path, float hp, float attack, float duration)
    {
        var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
        if (enemy == null)
        {
            Debug.LogWarning($"Enemy asset not found at: {path}");
            return;
        }

        var so = new SerializedObject(enemy);
        so.FindProperty("healthPoint").floatValue = hp;
        so.FindProperty("attackPoint").floatValue = attack;
        so.FindProperty("approachDuration").floatValue = duration;
        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(enemy);
        Debug.Log($"Updated Enemy: {enemy.name} -> HP: {hp}, Atk: {attack}, TTI: {duration}s");
    }
}
