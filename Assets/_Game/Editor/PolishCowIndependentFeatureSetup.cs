using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PolishCowIndependentFeatureSetup
{
    private const string TestFolder = "Assets/_Game/Scenes/Tests/";
    private const string DummyPath = "Assets/_Game/Prefabs/Features/Building/DummyBuilding.prefab";

    [MenuItem("Tools/PolishCow/Prepare Independent Feature Tests")]
    public static void PrepareIndependentTests()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Hãy thoát Play Mode trước khi chuẩn bị scene thử độc lập.");
            return;
        }
        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
        if (stage != null && stage.scene.isDirty)
        {
            Debug.LogError("Prefab đang có chỉnh sửa chưa lưu. Hãy tự lưu trước khi chuẩn bị điểm nối.");
            return;
        }
        string[] names = { "CowTest", "ProductionTest", "BuildingTest", "OrderTest" };
        // Không tự lưu chồng lên phần đang sửa dở của thành viên.
        for (int index = 0; index < names.Length; index++)
        {
            Scene loaded = SceneManager.GetSceneByPath(TestFolder + names[index] + ".unity");
            if (loaded.isLoaded && loaded.isDirty)
            {
                Debug.LogError(names[index] + " chưa lưu. Hãy kiểm tra và tự Ctrl+S trước khi chạy menu.");
                return;
            }
        }
        PrepareSamplePrefab("Assets/_Game/Prefabs/Features/Cows/CowSample.prefab");
        PrepareSamplePrefab("Assets/_Game/Prefabs/Features/Production/ProductionSample.prefab");
        PrepareSamplePrefab("Assets/_Game/Prefabs/Features/Building/BuildingSample.prefab");
        PrepareSamplePrefab("Assets/_Game/Prefabs/Features/Orders/OrderSample.prefab");
        PrepareSamplePrefab("Assets/_Game/Prefabs/Shared/InteractableSample.prefab");
        PrepareDummyPrefab();
        PolishCowParallelBaseSetup.PreparePrefabs();

        Scene previous = SceneManager.GetActiveScene();
        try
        {
            for (int index = 0; index < names.Length; index++)
            {
                string path = TestFolder + names[index] + ".unity";
                if (!File.Exists(path))
                {
                    Debug.LogError("Thiếu " + path + ". Chạy Prepare Team Workspace trước.");
                    continue;
                }
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.isLoaded;
                if (opened)
                {
                    scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                }
                try
                {
                    SceneManager.SetActiveScene(scene);
                    if (!ConfigureSceneConnections(scene))
                    {
                        continue;
                    }
                    InventoryManager inventory = PolishCowWorkspaceAudit.Collect<InventoryManager>(scene)[0];
                    PolishCowParallelBaseSetup.ConfigureScene(scene);
                    // Đặt số tuyệt đối bằng dữ liệu scene, không gọi Add và không Apply prefab.
                    SerializedObject data = new SerializedObject(inventory);
                    if (scene.name == "CowTest")
                    {
                        data.FindProperty("feed").intValue = 20;
                    }
                    else if (scene.name == "ProductionTest")
                    {
                        data.FindProperty("rawMilk").intValue = 50;
                    }
                    else if (scene.name == "BuildingTest")
                    {
                        data.FindProperty("credits").intValue = 1000;
                        ConfigureBuildingTest(scene);
                    }
                    else if (scene.name == "OrderTest")
                    {
                        data.FindProperty("bottledMilk").intValue = 100;
                        data.FindProperty("credits").intValue = 100;
                    }
                    data.ApplyModifiedProperties();
                    EditorSceneManager.MarkSceneDirty(scene);
                    if (!EditorSceneManager.SaveScene(scene))
                    {
                        Debug.LogError("Không lưu được " + path);
                    }
                    bool valid = PolishCowWorkspaceAudit.AuditScene(scene);
                    Debug.Log(scene.name + ": dữ liệu thử đã đặt tuyệt đối, không cộng dồn. " +
                        (valid ? "Đạt Audit." : "Còn lỗi Audit cần sửa.") + " Chưa xác nhận Play Mode.");
                }
                finally
                {
                    // Giữ scene đang mở và mọi thay đổi chưa lưu nếu gặp lỗi.
                    if (opened && !scene.isDirty)
                    {
                        EditorSceneManager.CloseScene(scene, true);
                    }
                }
            }
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
        }
    }

    // Dùng lại khi công cụ cũ vừa tạo một scene thử mới.
    public static bool ConfigureSceneConnections(Scene scene)
    {
        if (scene.path != TestFolder + "CowTest.unity" && scene.path != TestFolder + "ProductionTest.unity" &&
            scene.path != TestFolder + "BuildingTest.unity" && scene.path != TestFolder + "OrderTest.unity")
        {
            return false;
        }
        List<InventoryManager> inventories = PolishCowWorkspaceAudit.Collect<InventoryManager>(scene);
        List<InventoryHUD> huds = PolishCowWorkspaceAudit.Collect<InventoryHUD>(scene);
        if (inventories.Count != 1 || huds.Count != 1 ||
            new SerializedObject(huds[0]).FindProperty("inventory").objectReferenceValue != inventories[0])
        {
            Debug.LogError(scene.name + ": cần một kho và một HUD cùng trỏ kho đó. Không chọn kho tự động khi bị trùng/sai.");
            return false;
        }
        InventoryManager inventory = inventories[0];
        GameObject work = FindNamed(scene, "FeatureWork");
        if (work == null)
        {
            Debug.LogError(scene.name + ": không có FeatureWork duy nhất. Giữ nguyên công việc thành viên.");
            return false;
        }
        InventoryInteractionSample[] samples = work.GetComponentsInChildren<InventoryInteractionSample>(true);
        for (int index = 0; index < samples.Length; index++)
        {
            GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot(samples[index].gameObject);
            if (root == null || !root.transform.IsChildOf(work.transform))
            {
                root = samples[index].gameObject;
            }
            FeatureReferences references = root.GetComponent<FeatureReferences>();
            if (references == null)
            {
                references = Undo.AddComponent<FeatureReferences>(root);
            }
            SerializedObject data = new SerializedObject(samples[index]);
            SetIfMissing(data, "featureReferences", references);
            // Ô cũ chỉ phục vụ chuyển đổi; gameplay mẫu đọc kho qua FeatureReferences.
            data.FindProperty("inventory").objectReferenceValue = inventory;
            if (scene.name == "CowTest")
            {
                data.FindProperty("inspectItem").intValue = (int)ItemType.Feed;
            }
            else if (scene.name == "ProductionTest")
            {
                data.FindProperty("inspectItem").intValue = (int)ItemType.RawMilk;
            }
            else if (scene.name == "OrderTest")
            {
                data.FindProperty("inspectItem").intValue = (int)ItemType.BottledMilk;
            }
            data.ApplyModifiedProperties();
        }
        List<FeatureReferenceInitializer> initializers = PolishCowWorkspaceAudit.Collect<FeatureReferenceInitializer>(scene);
        if (initializers.Count == 0)
        {
            GameObject initializerObject = new GameObject("FeatureReferenceInitializer");
            SceneManager.MoveGameObjectToScene(initializerObject, scene);
            Undo.RegisterCreatedObjectUndo(initializerObject, "Create Feature Initializer");
            initializers.Add(Undo.AddComponent<FeatureReferenceInitializer>(initializerObject));
        }
        // Giữ danh sách đã gán, chỉ thêm FeatureReferences còn thiếu vào bộ nối đầu tiên.
        List<FeatureReferences> features = PolishCowWorkspaceAudit.Collect<FeatureReferences>(scene);
        for (int index = 0; index < initializers.Count; index++)
        {
            SerializedObject data = new SerializedObject(initializers[index]);
            data.FindProperty("inventory").objectReferenceValue = inventory;
            data.ApplyModifiedProperties();
        }
        SerializedObject initializerData = new SerializedObject(initializers[0]);
        SerializedProperty list = initializerData.FindProperty("features");
        for (int index = 0; index < features.Count; index++)
        {
            if (!IsListed(features[index], initializers))
            {
                int newIndex = list.arraySize;
                list.arraySize++;
                list.GetArrayElementAtIndex(newIndex).objectReferenceValue = features[index];
            }
        }
        initializerData.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        return true;
    }

    public static bool IsListed(FeatureReferences feature, List<FeatureReferenceInitializer> initializers)
    {
        for (int index = 0; index < initializers.Count; index++)
        {
            SerializedProperty list = new SerializedObject(initializers[index]).FindProperty("features");
            for (int itemIndex = 0; itemIndex < list.arraySize; itemIndex++)
            {
                if (list.GetArrayElementAtIndex(itemIndex).objectReferenceValue == feature)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static void PrepareSamplePrefab(string path)
    {
        if (!File.Exists(path))
        {
            Debug.LogWarning("Chưa có prefab mẫu: " + path);
            return;
        }
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            FeatureReferences references = root.GetComponent<FeatureReferences>();
            bool changed = false;
            if (references == null)
            {
                references = root.AddComponent<FeatureReferences>();
                changed = true;
            }
            InventoryInteractionSample[] samples = root.GetComponentsInChildren<InventoryInteractionSample>(true);
            for (int index = 0; index < samples.Length; index++)
            {
                SerializedObject data = new SerializedObject(samples[index]);
                if (SetIfMissing(data, "featureReferences", references))
                {
                    data.ApplyModifiedPropertiesWithoutUndo();
                    changed = true;
                }
            }
            if (changed)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void PrepareDummyPrefab()
    {
        if (!File.Exists(DummyPath))
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/World/Buildings/FarmHouse.prefab");
            if (source == null)
            {
                Debug.LogError("Thiếu FarmHouse để làm hình mẫu công trình giả.");
                return;
            }
            Scene previous = SceneManager.GetActiveScene();
            Scene scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scratch);
                GameObject template = (GameObject)PrefabUtility.InstantiatePrefab(source, scratch);
                template.name = "DummyBuilding";
                if (template.GetComponent<FeatureReferences>() == null)
                {
                    template.AddComponent<FeatureReferences>();
                }
                if (template.GetComponent<UpgradeTargetSample>() == null)
                {
                    template.AddComponent<UpgradeTargetSample>();
                }
                PrefabUtility.SaveAsPrefabAsset(template, DummyPath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded)
                {
                    SceneManager.SetActiveScene(previous);
                }
                EditorSceneManager.CloseScene(scratch, true);
            }
        }
        // Prefab giả đã có thì giữ nguyên, không ghi đè chỉnh sửa thủ công.
    }

    private static void ConfigureBuildingTest(Scene scene)
    {
        GameObject sample = FindNamed(scene, "BuildingSample");
        GameObject dummy = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPath);
        if (sample == null || dummy == null || sample.GetComponent<InventoryInteractionSample>() == null)
        {
            Debug.LogWarning("Giữ phần Building đã sửa: không tìm thấy mẫu cũ/prefab giả. Gán BuildingConnectionSample thủ công nếu cần.");
            return;
        }
        FeatureReferences references = sample.GetComponent<FeatureReferences>();
        BuildingConnectionSample test = sample.GetComponent<BuildingConnectionSample>();
        if (test == null)
        {
            test = Undo.AddComponent<BuildingConnectionSample>(sample);
        }
        Transform spawn = sample.transform.Find("BuildSpawnPoint");
        if (spawn == null)
        {
            GameObject point = new GameObject("BuildSpawnPoint");
            point.transform.SetParent(sample.transform, false);
            point.transform.localPosition = new Vector3(-6f, -1f, 0f);
            Undo.RegisterCreatedObjectUndo(point, "Create Dummy Spawn Point");
            spawn = point.transform;
        }
        SerializedObject testData = new SerializedObject(test);
        SetIfMissing(testData, "featureReferences", references);
        SetIfMissing(testData, "buildingPrefab", dummy);
        SetIfMissing(testData, "spawnPoint", spawn);
        testData.ApplyModifiedProperties();
        SerializedObject sampleData = new SerializedObject(sample.GetComponent<InventoryInteractionSample>());
        SetIfMissing(sampleData, "buildingTest", test);
        sampleData.ApplyModifiedProperties();
    }

    [MenuItem("Tools/PolishCow/Verify Independent Feature Contracts")]
    public static void VerifyContracts()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Menu Verify này dùng Edit Mode. Thử runtime bằng E trong BuildingTest.");
            return;
        }
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPath);
        if (prefab == null)
        {
            Debug.LogError("Chạy Prepare Independent Feature Tests để có DummyBuilding trước.");
            return;
        }
        Scene previous = SceneManager.GetActiveScene();
        Scene scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scratch);
            GameObject systems = new GameObject("TemporaryInventory");
            InventoryManager inventory = systems.AddComponent<InventoryManager>();
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scratch);
            FeatureReferences references = instance.GetComponent<FeatureReferences>();
            UpgradeTargetSample upgrade = instance.GetComponent<UpgradeTargetSample>();
            if (references == null || upgrade == null)
            {
                Debug.LogError("Prefab giả thiếu FeatureReferences/UpgradeTargetSample.");
                return;
            }
            int creditsBefore = inventory.GetAmount(ItemType.Credits);
            references.Initialize(inventory);
            upgrade.ApplyLevel(2);
            int firstLevel = upgrade.CurrentLevel;
            upgrade.ApplyLevel(2);
            bool valid = references.Inventory == inventory && firstLevel == 2 &&
                upgrade.CurrentLevel == firstLevel && inventory.GetAmount(ItemType.Credits) == creditsBefore;
            if (valid)
            {
                Debug.Log("ĐẠT VERIFY EDIT MODE: prefab giả nhận đúng kho, ApplyLevel(2) hai lần vẫn cấp 2, Credits giữ nguyên. Chưa xác nhận Play Mode.");
            }
            else
            {
                Debug.LogError("Verify thất bại: kiểm tra Initialize, cấp tuyệt đối hoặc tài nguyên bị thay đổi.");
            }
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
            EditorSceneManager.CloseScene(scratch, true);
        }
    }

    private static bool SetIfMissing(SerializedObject data, string fieldName, Object value)
    {
        SerializedProperty field = data.FindProperty(fieldName);
        if (field.objectReferenceValue == null)
        {
            field.objectReferenceValue = value;
            return true;
        }
        return false;
    }

    private static GameObject FindNamed(Scene scene, string objectName)
    {
        List<Transform> transforms = PolishCowWorkspaceAudit.Collect<Transform>(scene);
        GameObject found = null;
        for (int index = 0; index < transforms.Count; index++)
        {
            if (transforms[index].name == objectName)
            {
                if (found != null)
                {
                    Debug.LogError(scene.name + ": trùng " + objectName + ". Không chọn hoặc tạo thêm.");
                    return null;
                }
                found = transforms[index].gameObject;
            }
        }
        return found;
    }
}
