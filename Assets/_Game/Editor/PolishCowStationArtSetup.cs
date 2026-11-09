using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PolishCowStationArtSetup
{
    private const string Art = "Assets/_Game/Art/Features/Economy/FarmRanchLife/";
    private const string Data = "Assets/_Game/Data/Features/Economy/StationSprites/";

    [MenuItem("Tools/PolishCow/Team Base/Apply Farm Production and Delivery Art")]
    public static void Apply()
    {
        Scene original = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || original.isDirty) throw new Exception("Save the active scene and exit Play Mode before applying station art.");
        foreach (Scene scene in Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt))
            if (scene.isDirty) throw new Exception("Save every loaded scene before applying station art.");
        Directory.CreateDirectory(Data);
        AssetDatabase.Refresh();
        Sprite[] production = Frames("Production","cheese_press_idle_work4_done_6f.png",16,32,6);
        Sprite[] delivery = Frames("Delivery","shipping_bin_lid_open_4f.png",32,24,4);
        UpdatePrefab("Assets/_Game/Prefabs/Features/Production/ProductionMachine.prefab",production,delivery);
        UpdatePrefab("Assets/_Game/Prefabs/Features/Orders/OrderSample.prefab",production,delivery);
        string[] paths = {
            "Assets/_Game/Scenes/Main.unity",
            "Assets/_Game/Scenes/Workspaces/EconomyWorkspace.unity",
            "Assets/_Game/Scenes/Tests/ProductionTest.unity",
            "Assets/_Game/Scenes/Tests/OrderTest.unity"
        };
        foreach (string path in paths)
        {
            if (!File.Exists(path)) continue;
            Scene scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try
            {
                Directory.CreateDirectory("output/backups");
                string backup = "output/backups/" + Path.GetFileNameWithoutExtension(path) + "-before-station-art.unity";
                if (!File.Exists(backup)) File.Copy(path,backup);
                foreach (ProductionMachine machine in All<ProductionMachine>(scene)) Production(machine,production);
                foreach (OrderController orders in All<OrderController>(scene)) Delivery(orders,delivery);
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (string area in new[]{"Production","Delivery"})
                    {
                        Transform placeholder = root.transform.Find("PrototypeMap/Areas/" + area);
                        if (placeholder == null) continue;
                        var renderer = placeholder.GetComponent<SpriteRenderer>(); if (renderer != null) renderer.enabled = false;
                        Transform label = placeholder.Find("Label"); if (label != null) label.gameObject.SetActive(false);
                    }
                }
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Could not save " + path);
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene,true); }
        }
        SceneManager.SetActiveScene(original);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/StationArtSetup.txt","PASS: Farm & Ranch Life art applied to production/delivery prefabs, Main, EconomyWorkspace, ProductionTest and OrderTest. Six production frames and four shipping-bin frames.\n");
        Debug.Log("PASS Production/delivery station art installed.");
    }
    private static void UpdatePrefab(string path,Sprite[] production,Sprite[] delivery)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            foreach (ProductionMachine machine in root.GetComponentsInChildren<ProductionMachine>(true)) Production(machine,production);
            foreach (OrderController orders in root.GetComponentsInChildren<OrderController>(true)) Delivery(orders,delivery);
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }
    private static void Production(ProductionMachine machine,Sprite[] frames)
    {
        var old = machine.GetComponent<SpriteRenderer>(); if (old != null) old.enabled = false;
        Transform visual = Child(machine.transform,"ProductionVisual"); visual.localPosition = Vector3.zero; visual.localScale = Vector3.one * 1.35f;
        SpriteRenderer body = Component<SpriteRenderer>(visual.gameObject); body.sprite = frames[0]; body.color = Color.white;
        Component<WorldDepthSorting>(visual.gameObject); body.sortingOrder = WorldDepthSorting.GetSortingOrder(visual.position.y);
        var adapter = Component<ProductionStationVisual>(visual.gameObject); Set(adapter,"machine",machine); Set(adapter,"body",body); SetFrames(adapter,frames);
        var collider = machine.GetComponent<BoxCollider2D>(); if (collider != null) { collider.size = new Vector2(1.2f,0.55f); collider.offset = new Vector2(0,0.2f); }
        Transform status = machine.transform.Find("StatusText"); if (status != null) status.localPosition = new Vector3(0,3.15f,0);
        Transform bar = machine.transform.Find("ProgressBar"); if (bar != null) bar.localPosition = new Vector3(0,2.85f,0);
        var hint = new SerializedObject(machine); hint.FindProperty("interactionText").stringValue = "Chế biến sữa"; hint.ApplyModifiedPropertiesWithoutUndo();
        Record(old); Record(visual); Record(body); Record(collider); Record(status); Record(bar);
    }
    private static void Delivery(OrderController orders,Sprite[] frames)
    {
        OrderInteraction interaction = orders.GetComponent<OrderInteraction>();
        if (interaction == null) interaction = orders.GetComponentInParent<OrderInteraction>();
        Transform root = interaction != null ? interaction.transform : orders.transform;
        var old = root.GetComponent<SpriteRenderer>(); if (old != null) old.enabled = false;
        Transform visual = Child(root,"DeliveryVisual"); visual.localPosition = Vector3.zero; visual.localScale = Vector3.one * 1.25f;
        SpriteRenderer body = Component<SpriteRenderer>(visual.gameObject); body.sprite = frames[0]; body.color = Color.white;
        Component<WorldDepthSorting>(visual.gameObject); body.sortingOrder = WorldDepthSorting.GetSortingOrder(visual.position.y);
        var adapter = Component<DeliveryStationVisual>(visual.gameObject); Set(adapter,"orders",orders); Set(adapter,"body",body); SetFrames(adapter,frames);
        var collider = root.GetComponent<BoxCollider2D>(); if (collider != null) { collider.size = new Vector2(2.3f,0.45f); collider.offset = new Vector2(0,0.2f); }
        Record(old); Record(visual); Record(body); Record(collider);
    }
    private static Sprite[] Frames(string name,string file,int width,int height,int count)
    {
        string source = Art + file;
        var importer = (TextureImporter)AssetImporter.GetAtPath(source);
        if (importer == null) throw new Exception("Missing station artwork: " + source);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(source);
        if (texture.width != width * count || texture.height != height) throw new Exception("Unexpected station sprite-sheet dimensions.");
        Sprite[] frames = new Sprite[count];
        for (int i = 0; i < count; i++)
        {
            string path = Data + name + i + ".asset";
            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (frames[i] != null) continue;
            frames[i] = Sprite.Create(texture,new Rect(i*width,0,width,height),new Vector2(0.5f,0),16,0,SpriteMeshType.FullRect);
            frames[i].name = name + i; AssetDatabase.CreateAsset(frames[i],path);
        }
        return frames;
    }
    private static T[] All<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>(true)).ToArray();
    private static void Record(UnityEngine.Object target) { if (target != null && PrefabUtility.IsPartOfPrefabInstance(target)) PrefabUtility.RecordPrefabInstancePropertyModifications(target); }
    private static T Component<T>(GameObject root) where T : Component { T value = root.GetComponent<T>(); return value != null ? value : root.AddComponent<T>(); }
    private static Transform Child(Transform root,string name) { Transform found = root.Find(name); if (found != null) return found; var obj = new GameObject(name); obj.transform.SetParent(root,false); return obj.transform; }
    private static void Set(UnityEngine.Object target,string name,UnityEngine.Object value) { var data = new SerializedObject(target); data.FindProperty(name).objectReferenceValue = value; data.ApplyModifiedPropertiesWithoutUndo(); }
    private static void SetFrames(UnityEngine.Object target,Sprite[] frames)
    { var data = new SerializedObject(target); var p = data.FindProperty("frames"); p.arraySize = frames.Length; for (int i = 0; i < frames.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = frames[i]; data.ApplyModifiedPropertiesWithoutUndo(); }
}
