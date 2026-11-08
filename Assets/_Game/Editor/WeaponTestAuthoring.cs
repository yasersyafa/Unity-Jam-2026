using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Linq;

public static class WeaponTestAuthoring
{
    [MenuItem("Tools/Weapon Testing/Refresh Weapon Catalog")]
    public static void Refresh()
    {
        if (EditorApplication.isPlaying) { Debug.LogError("Stop Play Mode before refreshing the catalog."); return; }
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/WeaponTest.unity") { Debug.LogError("Open WeaponTest first."); return; }
        var target = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WeaponTestController>(true)).Single();
        Catalog(target); EditorSceneManager.MarkSceneDirty(scene);
    }
    private static void Catalog(Object target)
    {
        var assets = AssetDatabase.FindAssets("t:WeaponData", new[] { "Assets/_Game/ScriptableObjects/Weapons" }).Select(g => AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(w => w.Tier).ThenBy(w => w.name).ToArray();
        var so = new SerializedObject(target); var field = so.FindProperty("weapons"); field.arraySize = assets.Length;
        for (int i = 0; i < assets.Length; i++) field.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Set(Object target, string name, Object value) { var so = new SerializedObject(target); so.FindProperty(name).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Gameplay/GameplayRig.prefab");
        // Copy the authored shooting rig, then remove gameplay owners before saving the test scene.
        var rig = (GameObject)PrefabUtility.InstantiatePrefab(source); PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var type in new[] { typeof(EnemySpawner), typeof(PlayerDamageFeedback), typeof(PlayerHealthUI), typeof(PlayerHealth), typeof(WaveBannerPresentation), typeof(WeaponDurationFeedback), typeof(SlotPanelPresentation), typeof(SlotRewardPresentation) })
            foreach (var component in rig.GetComponentsInChildren(type, true)) Object.DestroyImmediate(component);
        var shooting = rig.GetComponentInChildren<ShootingController>(true);
        var feedback = rig.GetComponentInChildren<WeaponFeedback>(true);
        var camera = rig.GetComponentInChildren<Camera>(true); camera.tag = "MainCamera";
        var soFeedback = new SerializedObject(feedback);
        var canvas = (Canvas)soFeedback.FindProperty("canvas").objectReferenceValue;
        var crosshair = (RectTransform)soFeedback.FindProperty("crosshair").objectReferenceValue;
        var image = (Image)soFeedback.FindProperty("weaponImage").objectReferenceValue;
        foreach (Transform child in canvas.transform.Cast<Transform>().ToArray())
            if (!crosshair.IsChildOf(child) && !image.transform.IsChildOf(child) && child != crosshair && child != image.transform) Object.DestroyImmediate(child.gameObject);
        var weapons = AssetDatabase.FindAssets("t:WeaponData",new[]{"Assets/_Game/ScriptableObjects/Weapons"}).Select(g=>AssetDatabase.LoadAssetAtPath<WeaponData>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
        Set(shooting,"weapon",weapons.First(w=>w.name=="Batu_Biasa"));
        foreach (var existing in rig.GetComponentsInChildren<EventSystem>(true)) Object.DestroyImmediate(existing.gameObject);
        var events = new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule)); events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Game/Fonts/BarlowCondensed/BarlowCondensed-SemiBold SDF.asset");
        GameObject Text(string name, Transform parent, Vector2 pos, string content)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false);
            var rect=(RectTransform)go.transform; rect.sizeDelta=new Vector2(650,130); rect.anchoredPosition=pos;
            var text=go.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSize=26;text.text=content;text.raycastTarget=false;return go;
        }
        Text("Instructions",canvas.transform,new Vector2(0,380),"WEAPON TEST\nTab: pilih senjata · Klik arena: mulai menembak");
        var panel=new GameObject("Weapon Selection",typeof(RectTransform),typeof(Image));panel.transform.SetParent(canvas.transform,false);
        ((RectTransform)panel.transform).sizeDelta=new Vector2(740,450);panel.GetComponent<Image>().color=new Color(0.08f,0.08f,0.12f,0.98f);
        var info=Text("Information",panel.transform,new Vector2(0,30),"").GetComponent<TextMeshProUGUI>();
        // Instantiate TMP's native dropdown template rather than rebuilding its scrolling hierarchy.
        TMP_DefaultControls.Resources resources=new TMP_DefaultControls.Resources();
        var dropdownObject=TMP_DefaultControls.CreateDropdown(resources);dropdownObject.transform.SetParent(panel.transform,false);
        var dropdown=dropdownObject.GetComponent<TMP_Dropdown>();((RectTransform)dropdown.transform).anchoredPosition=new Vector2(0,150);((RectTransform)dropdown.transform).sizeDelta=new Vector2(600,50);
        foreach(var text in dropdownObject.GetComponentsInChildren<TMP_Text>(true)){text.font=font;text.fontSize=24;}
        var item=dropdown.template.Find("Viewport/Content/Item") as RectTransform;
        if(item!=null)item.sizeDelta=new Vector2(item.sizeDelta.x,36f);
        var button=new GameObject("Uji Senjata",typeof(RectTransform),typeof(Image),typeof(Button));button.transform.SetParent(panel.transform,false);
        ((RectTransform)button.transform).sizeDelta=new Vector2(300,60);((RectTransform)button.transform).anchoredPosition=new Vector2(0,-150);Text("Label",button.transform,Vector2.zero,"Uji Senjata").GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Center;
        button.GetComponent<Image>().color=new Color(0.18f,0.3f,0.55f,1f);
        button.GetComponent<Button>().targetGraphic=button.GetComponent<Image>();
        var controller=new GameObject("Weapon Test Controller").AddComponent<WeaponTestController>();Set(controller,"shooting",shooting);Set(controller,"dropdown",dropdown);Set(controller,"panel",panel);Set(controller,"information",info);Set(controller,"testButton",button.GetComponent<Button>());Catalog(controller);
        var enemy=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Enemies/NormalEnemy.prefab").GetComponentInChildren<SpriteRenderer>();
        for(int i=0;i<3;i++){var target=new GameObject("Practice Target "+(i+1),typeof(SpriteRenderer),typeof(BoxCollider2D),typeof(ShootingDummy));target.transform.position=new Vector3((i-1)*3f,0,0);target.GetComponent<SpriteRenderer>().sprite=enemy.sprite;target.GetComponent<SpriteRenderer>().sortingOrder=50;target.GetComponent<BoxCollider2D>().size=enemy.sprite.bounds.size;}
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/WeaponTest.unity");Debug.Log("WEAPON TEST SCENE CREATED");
    }
}
