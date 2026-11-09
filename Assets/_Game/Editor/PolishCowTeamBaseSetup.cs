using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class PolishCowTeamBaseSetup
{
    private const string Workspaces = "Assets/_Game/Scenes/Workspaces";
    private static readonly string[] Roles = { "Economy", "Building", "Enemies", "Towers" };
    private static readonly Type[] Modules = { typeof(EconomyFeature), typeof(BuildingFeature), typeof(EnemiesFeature), typeof(TowersFeature) };
    private const string Prefabs = "Assets/_Game/Prefabs/Modules/";

    [MenuItem("Tools/PolishCow/Team Base/Prepare Four Workspaces")]
    public static void Prepare()
    {
        Scene main = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || main.path != "Assets/_Game/Scenes/Main.unity" || main.isDirty)
            throw new Exception("Open saved Main in Edit Mode. Team setup never discards unsaved edits.");
        for (int i = 0; i < Roles.Length; i++)
        {
            Directory.CreateDirectory(Prefabs + Roles[i]);
            Directory.CreateDirectory("Assets/_Game/Data/Features/" + Roles[i]);
            Directory.CreateDirectory("Assets/_Game/Art/Features/" + Roles[i]);
        }
        Directory.CreateDirectory(Workspaces);
        AssetDatabase.Refresh();
        for (int i = 0; i < Roles.Length; i++) CreateModulePrefab(i);
        AttachContext(main, -1);
        Save(main);
        for (int i = 0; i < Roles.Length; i++)
        {
            string path = Workspaces + "/" + Roles[i] + "Workspace.unity";
            if (File.Exists(path)) continue;
            string source = i == 0 ? main.path : "Assets/_Game/Scenes/Tests/" + (i == 1 ? "CoreTest" : i == 2 ? "EnemyTest" : "TowerTest") + ".unity";
            // Economy is copied from Main on disk, not opened a second time as the same scene.
            if (!AssetDatabase.CopyAsset(source,path)) throw new Exception("Cannot create " + path);
            Scene workspace = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(workspace);
            try
            {
                var inventory = All<InventoryManager>(workspace).Single();
                Set(inventory,"credits",1000); Set(inventory,"feed",20); Set(inventory,"rawMilk",50); Set(inventory,"bottledMilk",20); Set(inventory,"medicine",5);
                Transform moduleContainer = Root(workspace,"FeatureModules").transform;
                // Remove only the freshly copied module instances, never member-owned assets.
                foreach (Transform child in moduleContainer.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                InstantiateModule(moduleContainer,i);
                AddPlayer(workspace);
                if (!All<Light2D>(workspace).Any()) new GameObject("Global Light 2D").AddComponent<Light2D>().lightType = Light2D.LightType.Global;
                AddFixtures(workspace,i);
                AttachContext(workspace,i);
                Save(workspace);
            }
            finally { EditorSceneManager.CloseScene(workspace,true); SceneManager.SetActiveScene(main); }
        }
        AssetDatabase.SaveAssets();
        Verify();
        Debug.Log("Four independent team workspaces are ready. See Docs/TEAM_4_WORKSTREAMS.md.");
    }
    private static void CreateModulePrefab(int role)
    {
        string path = Prefabs + Roles[role] + "/" + Roles[role] + "Feature.prefab";
        if (File.Exists(path)) return;
        var template = new GameObject(Roles[role] + "Feature");
        try
        {
            template.AddComponent<FeatureReferences>(); template.AddComponent(Modules[role]);
            PrefabUtility.SaveAsPrefabAsset(template,path);
        }
        finally { UnityEngine.Object.DestroyImmediate(template); }
    }
    private static void InstantiateModule(Transform parent,int role)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + Roles[role] + "/" + Roles[role] + "Feature.prefab");
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
        instance.name = Roles[role] + "Feature";
    }
    private static void AttachContext(Scene scene,int onlyRole)
    {
        InventoryManager inventory = All<InventoryManager>(scene).Single();
        GameObject systems = inventory.gameObject;
        var context = Component<DefenseSceneContext>(systems);
        Set(context,"inventory",inventory); Set(context,"game",All<GameManager>(scene).Single());
        Set(context,"progress",All<CastleProgress>(scene).Single()); Set(context,"castleHealth",All<CastleHealth>(scene).Single()); Set(context,"timer",All<WaveTimer>(scene).Single());
        Transform target = All<Transform>(scene).First(t => t.name == "CastleTarget"); Set(context,"castleTarget",target);
        SetArray(context,"slots",All<DefenseBuildingSlot>(scene)); SetArray(context,"routes",All<EnemyRoute>(scene));
        SetArray(context,"waves",Enumerable.Range(1,10).Select(i => AssetDatabase.LoadAssetAtPath<WaveDefinition>("Assets/_Game/Data/Defense/Wave" + i.ToString("00") + ".asset")).ToArray());
        foreach (DefenseBuildingSlot slot in All<DefenseBuildingSlot>(scene)) Set(slot,"defenseContext",context);
        Transform container = Root(scene,"FeatureModules").transform;
        if (onlyRole < 0)
            for (int i = 0; i < Roles.Length; i++) if (container.Find(Roles[i]+"Feature") == null) InstantiateModule(container,i);
        var installer = Component<DefenseModuleInstaller>(systems);
        var fixtures = scene.GetRootGameObjects().FirstOrDefault(o => o.name == "TestSupport");
        Set(installer,"context",context);
        SetArray(installer,"featureRoots",fixtures != null ? new[]{container,fixtures.transform} : new[]{container});
        var initializer = Component<FeatureReferenceInitializer>(systems); Set(initializer,"inventory",inventory); SetArray(initializer,"features",All<FeatureReferences>(scene));
        var game = All<GameManager>(scene).Single();
        SetArray(game,"playerControls",All<PlayerMovement>(scene).Cast<UnityEngine.Object>().Concat(All<PlayerInteraction>(scene)).ToArray());
    }
    private static void AddPlayer(Scene scene)
    {
        if (All<PlayerMovement>(scene).Any()) return;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Shared/Player.prefab");
        var player = (GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        player.transform.position = new Vector3(0,-5,0);
        Canvas canvas = All<Canvas>(scene).First(c => c.renderMode == RenderMode.ScreenSpaceOverlay);
        var hintObject = new GameObject("InteractionHint",typeof(RectTransform),typeof(TextMeshProUGUI)); hintObject.transform.SetParent(canvas.transform,false);
        var hint = hintObject.GetComponent<TextMeshProUGUI>(); hint.font = TMP_Settings.defaultFontAsset; hint.fontSize = 24; hint.alignment = TextAlignmentOptions.Center; hint.raycastTarget = false;
        hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f,0); hint.rectTransform.anchoredPosition = new Vector2(0,40); hint.rectTransform.sizeDelta = new Vector2(700,60);
        Set(player.GetComponent<PlayerInteraction>(),"interactionHint",hint);
    }
    private static void AddFixtures(Scene scene,int role)
    {
        var support = Root(scene,"TestSupport"); support.AddComponent<DefenseSandboxPanel>();
        if (role == 0) return;
        Sprite marker = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Prototype/PrototypeMarker.asset");
        var enemy = Fixture(support.transform,"EnemyTargetDummy",new Vector2(4,2),marker,new Color(0.85f,0.25f,0.2f));
        enemy.AddComponent<EnemyTargetDummy>();
        var cow = Fixture(support.transform,"CowDiseaseTargetDummy",new Vector2(-5,3),marker,new Color(0.95f,0.9f,0.7f));
        cow.AddComponent<CowDiseaseTargetDummy>();
        if (role == 3)
        {
            var flying = Fixture(support.transform,"FlyingEnemyDummy",new Vector2(7,3),marker,new Color(0.7f,0.4f,0.9f));
            Set(flying.AddComponent<EnemyTargetDummy>(),"flying",true);
        }
    }
    private static GameObject Fixture(Transform parent,string name,Vector2 position,Sprite sprite,Color color)
    {
        var obj = new GameObject(name); obj.transform.SetParent(parent,false); obj.transform.position = position;
        var renderer = obj.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = 500;
        return obj;
    }

    [MenuItem("Tools/PolishCow/Team Base/Verify Four Workspaces")]
    public static void Verify()
    {
        Scene original = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty) throw new Exception("Verify from a saved scene in Edit Mode.");
        foreach (string role in Roles)
        {
            string path = Workspaces + "/" + role + "Workspace.unity";
            if (!File.Exists(path)) throw new Exception("Missing workspace " + role);
            Scene scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                Require(All<InventoryManager>(scene).Length == 1 && All<GameManager>(scene).Length == 1,"One inventory/game: " + role);
                Require(All<PlayerMovement>(scene).Length == 1 && All<PlayerInteraction>(scene).Length == 1,"One player/interaction: " + role);
                Require(All<DefenseSceneContext>(scene).Single().IsReady,"Context ready: " + role);
                Require(All<DefenseModuleInstaller>(scene).Length == 1,"One installer: " + role);
                var roots = scene.GetRootGameObjects().Single(o => o.name == "FeatureModules").transform;
                Require(roots.childCount == 1 && roots.GetChild(0).name == role + "Feature","One owned module: " + role);
                Require(All<DefenseBuildingSlot>(scene).All(s => new SerializedObject(s).FindProperty("defenseContext").objectReferenceValue != null),"Slots receive context: " + role);
            }
            finally { EditorSceneManager.CloseScene(scene,true); SceneManager.SetActiveScene(original); }
        }
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/TeamBaseVerify.txt","PASS: four independent workspaces; one player/inventory/game/context/module installer per workspace; one owned feature module; all slots wired.\n");
        Debug.Log("PASS Four workspaces verified.");
    }
    private static void Require(bool value,string message) { if (!value) throw new Exception(message); }
    private static GameObject Root(Scene scene,string name)
    { var existing = scene.GetRootGameObjects().FirstOrDefault(o => o.name == name); if (existing != null) return existing; var root = new GameObject(name); SceneManager.MoveGameObjectToScene(root,scene); return root; }
    private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>(true)).ToArray();
    private static T Component<T>(GameObject obj) where T : Component { T existing = obj.GetComponent<T>(); return existing != null ? existing : obj.AddComponent<T>(); }
    private static void Save(Scene scene) { EditorSceneManager.MarkSceneDirty(scene); if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Cannot save " + scene.path); }
    private static void Set(UnityEngine.Object target,string name,object value)
    {
        var data = new SerializedObject(target); var p = data.FindProperty(name);
        if (p == null) throw new Exception("Missing property " + name);
        if (value is int number) p.intValue = number; else if (value is bool flag) p.boolValue = flag; else p.objectReferenceValue = value as UnityEngine.Object;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetArray(UnityEngine.Object target,string name,UnityEngine.Object[] values)
    {
        var data = new SerializedObject(target); var p = data.FindProperty(name); p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; data.ApplyModifiedPropertiesWithoutUndo();
    }
}
