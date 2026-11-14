using System.Linq;
using GabrielBigardi.SpriteAnimator;
using UnityEditor;
using UnityEngine;

public static class BossSetup
{
    private const string BossConfigPath = "Assets/_Game/ScriptableObjects/Enemies/BossConfig.asset";
    private const string BossPrefabPath = "Assets/_Game/Prefabs/Boss/Boss.prefab";
    private const string BossAttackTargetPrefabPath = "Assets/_Game/Prefabs/Boss/BossAttackTarget.prefab";
    private const string RangedProjectilePrefabPath = "Assets/_Game/Prefabs/Enemies/RangedProjectile.prefab";
    private const string ChargedDataPath = "Assets/_Game/ScriptableObjects/Enemies/ChargedEnemyData.asset";
    private const string NormalDataPath = "Assets/_Game/ScriptableObjects/Enemies/NormalEnemyData.asset";
    private const string ProjectileSheetPath = "Assets/_Game/Arts/Enemy/EnemyBossProjectile.png";
    private const string BossLaughClipPath = "Assets/_Game/Audio/SFX/Enemy/Boss Laugh.wav";
    private const string BossHitClipPath = "Assets/_Game/Audio/SFX/Enemy/Boss Hitt.wav";
    private const string ProjectileHitClipPath = "Assets/_Game/Audio/SFX/Enemy/Projectile Hit.wav";
    private const string BossLaughCuePath = "Assets/_Game/ScriptableObjects/Audio/BossLaugh.asset";
    private const string BossHitCuePath = "Assets/_Game/ScriptableObjects/Audio/BossHit.asset";
    private const string ProjectileHitCuePath = "Assets/_Game/ScriptableObjects/Audio/ProjectileHit.asset";

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

        var bossLaughCue = EnsureAudioCue(BossLaughCuePath, BossLaughClipPath, 0.9f, Vector2.one, 0.1f, 2);
        var bossHitCue = EnsureAudioCue(BossHitCuePath, BossHitClipPath, 0.75f, new Vector2(0.95f, 1.05f), 0.05f, 6);
        var projectileHitCue = EnsureAudioCue(ProjectileHitCuePath, ProjectileHitClipPath, 0.75f, new Vector2(0.95f, 1.05f), 0.03f, 8);

        var serializedConfig = new SerializedObject(config);
        serializedConfig.FindProperty("health").floatValue = 12500f;
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
        serializedConfig.FindProperty("bossLaughSound").objectReferenceValue = bossLaughCue;
        serializedConfig.FindProperty("bossHitSound").objectReferenceValue = bossHitCue;
        serializedConfig.FindProperty("projectileHitSound").objectReferenceValue = projectileHitCue;
        serializedConfig.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);

        WirePrefabAudio<BossAttackTarget>(BossAttackTargetPrefabPath, "defaultHitSound", projectileHitCue);
        WirePrefabAudio<EnemyProjectile>(RangedProjectilePrefabPath, "hitSound", projectileHitCue);

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

    private static AudioCue EnsureAudioCue(
        string assetPath,
        string clipPath,
        float volume,
        Vector2 pitch,
        float cooldown,
        int maximumVoices)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clipPath);
        if (clip == null)
            throw new System.InvalidOperationException($"Missing AudioClip at {clipPath}.");

        var cue = AssetDatabase.LoadAssetAtPath<AudioCue>(assetPath);
        if (cue == null)
        {
            cue = ScriptableObject.CreateInstance<AudioCue>();
            AssetDatabase.CreateAsset(cue, assetPath);
        }

        cue.clips = new[] { clip };
        cue.volume = volume;
        cue.pitch = pitch;
        cue.cooldown = cooldown;
        cue.maximumVoices = maximumVoices;
        cue.ui = false;
        cue.loop = false;
        cue.loopStartSeconds = 0f;
        EditorUtility.SetDirty(cue);
        return cue;
    }

    private static void WirePrefabAudio<TComponent>(string prefabPath, string fieldName, AudioCue cue)
        where TComponent : Component
    {
        var root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            var component = root.GetComponent<TComponent>();
            if (component == null)
                throw new System.InvalidOperationException($"Missing {typeof(TComponent).Name} on prefab {prefabPath}.");

            var serialized = new SerializedObject(component);
            serialized.FindProperty(fieldName).objectReferenceValue = cue;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(component);
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
