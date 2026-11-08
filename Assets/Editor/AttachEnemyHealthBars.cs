using UnityEditor;
using UnityEngine;

public static class AttachEnemyHealthBars
{
    [MenuItem("Tools/Attach Enemy Health Bars To Prefabs")]
    public static void Execute()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Game/Prefabs/Enemies" });
        int count = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var root = PrefabUtility.LoadPrefabContents(path);
            var enemy = root.GetComponent<Enemy>();
            if (enemy != null)
            {
                if (root.GetComponent<EnemyHealthBar>() == null)
                {
                    root.AddComponent<EnemyHealthBar>();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                    count++;
                    Debug.Log($"Added EnemyHealthBar to {path}");
                }
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"Attached EnemyHealthBar to {count} enemy prefabs.");
    }
}
