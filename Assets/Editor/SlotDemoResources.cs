using UnityEditor;

[InitializeOnLoad]
public static class SlotDemoResources
{
    static SlotDemoResources()
    {
        EditorApplication.delayCall += EnsureFontResources;
    }

    private static void EnsureFontResources()
    {
        if (AssetDatabase.LoadAssetAtPath<TMPro.TMP_Settings>("Assets/TextMesh Pro/Resources/TMP Settings.asset") == null)
            TMPro.TMP_PackageResourceImporter.ImportResources(true, false, false);
    }
}
