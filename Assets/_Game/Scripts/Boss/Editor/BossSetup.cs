using System.Linq;
using GabrielBigardi.SpriteAnimator;
using UnityEditor;
using UnityEngine;

public static class BossSetup
{
    private const string BossConfigPath = "Assets/_Game/ScriptableObjects/Enemies/BossConfig.asset";
    private const string BossPrefabPath = "Assets/_Game/Prefabs/Boss/Boss.prefab";
    private const string ChargedDataPath = "Assets/_Game/ScriptableObjects/Enemies/ChargedEnemyData.asset";
    private const string NormalDataPath = "Assets/_Game/ScriptableObjects/Enemies/NormalEnemyData.asset";
    private const string ProjectileSheetPath = "Assets/_Game/Arts/Enemy/EnemyBossProjectile.png";

    [MenuItem("Game/Boss/Wire Boss Config And Prefab")]
    public static void WireBossConfigAndPrefab()
    {
        var config = AssetDatabase.LoadAssetAtPath<BossConfig>(BossConfigPath);
        if (config == null)
            throw new System.InvalidOperationException($"Missing BossConfig asset at {BossConfigPath}.");

        var chargedData = AssetDatabase.LoadAssetAtPath<EnemyData>(ChargedDataPath);
        var normalData = AssetDatabase.LoadAssetAtPath<EnemyData>(NormalDataPath);
        var sprites = AssetDatabase.LoadAllAssetsAtPath(ProjectileSheetPath).OfType<Sprite>().ToArray();
        var redSprite = sprites.FirstOrDefault(s => s.name == "EnemyBossProjectile_0");
        var blueSprite = sprites.FirstOrDefault(s => s.name == "EnemyBossProjectile_1");

        if (chargedData == null || normalData == null)
            throw new System.InvalidOperationException("Missing ChargedEnemyData or NormalEnemyData asset.");
        if (redSprite == null || blueSprite == null)
            throw new System.InvalidOperationException("Missing EnemyBossProjectile_0 or EnemyBossProjectile_1 sprites.");

        var serializedConfig = new SerializedObject(config);
        serializedConfig.FindProperty("health").floatValue = 2500f;
        serializedConfig.FindProperty("chargedEnemyData").objectReferenceValue = chargedData;
        serializedConfig.FindProperty("normalEnemyData").objectReferenceValue = normalData;
        serializedConfig.FindProperty("redProjectileSprite").objectReferenceValue = redSprite;
        serializedConfig.FindProperty("blueProjectileSprite").objectReferenceValue = blueSprite;
        serializedConfig.FindProperty("baseProjectileDamage").floatValue = 10f;
        serializedConfig.FindProperty("damageBonusPerProjectile").floatValue = 5f;
        serializedConfig.FindProperty("buffChance").floatValue = 0.5f;
        serializedConfig.FindProperty("redWaitBuffReduction").floatValue = 0.2f;
        serializedConfig.FindProperty("blueSpeedBuffFraction").floatValue = 0.2f;
        serializedConfig.FindProperty("redProjectileCount").intValue = 8;
        serializedConfig.FindProperty("redOrbitDuration").floatValue = 10f;
        serializedConfig.FindProperty("blueProjectileCount").intValue = 4;
        serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);

        var bossRoot = PrefabUtility.LoadPrefabContents(BossPrefabPath);
        try
        {
            var encounter = bossRoot.GetComponent<BossEncounter>();
            var rootAnimator = bossRoot.GetComponent<SpriteAnimator>();
            var spinner = bossRoot.GetComponentsInChildren<SpriteAnimator>(true).FirstOrDefault(a => a != rootAnimator);
            if (encounter != null && spinner != null)
            {
                var serializedBoss = new SerializedObject(encounter);
                serializedBoss.FindProperty("spinnerAnimator").objectReferenceValue = spinner;
                serializedBoss.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(encounter);
            }
            PrefabUtility.SaveAsPrefabAsset(bossRoot, BossPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(bossRoot);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
}
