using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class PolishCowTeamWorkspaceSetup
{
    private const string MainPath = "Assets/_Game/Scenes/Main.unity";
    private const string TestFolder = "Assets/_Game/Scenes/Tests/";
    private const string SharedFolder = "Assets/_Game/Prefabs/Shared/";
    private const string FeatureFolder = "Assets/_Game/Prefabs/Features/";
    private const string WorldFolder = "Assets/_Game/Prefabs/World/";

    [MenuItem("Tools/PolishCow/Prepare Team Workspace")]
    public static void PrepareTeamWorkspace()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Hãy thoát Play Mode trước khi chuẩn bị workspace.");
            return;
        }
        if (!File.Exists(MainPath))
        {
            Debug.LogError("Không tìm thấy Main đã lưu. Menu không tạo lại Main.");
            return;
        }
        Scene main = SceneManager.GetSceneByPath(MainPath);
        if (main.isLoaded && main.isDirty)
        {
            Debug.LogError("Main có thay đổi chưa lưu. Hãy tự kiểm tra và Ctrl+S trước khi chạy menu.");
            return;
        }

        Scene previousScene = SceneManager.GetActiveScene();
        bool openedMain = !main.isLoaded;
        if (openedMain)
        {
            main = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Additive);
        }
        Scene scratch = default(Scene);
        try
        {
            // Chỉ đọc Main. Tất cả đối tượng tạm được tạo ở scene riêng.
            if (!ValidateBase(main, false))
            {
                Debug.LogError("Base Main còn thiếu tham chiếu bắt buộc. Xem lỗi phía trên; Main chưa bị sửa.");
                return;
            }
            CreateFolders();
            scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scratch);
            PrepareSharedPrefabs(main);
            PrepareFeaturePrefabs(main);
            AssetDatabase.SaveAssets();
            EditorSceneManager.CloseScene(scratch, true);

            string[] sceneNames = { "CowTest", "ProductionTest", "BuildingTest", "OrderTest" };
            string[] featureNames = { "Cows", "Production", "Building", "Orders" };
            for (int index = 0; index < sceneNames.Length; index++)
            {
                CreateTestScene(sceneNames[index], featureNames[index]);
            }
            Debug.Log("Đã xử lý tạo mới workspace. Scene có sẵn được Audit, không sửa hoặc coi là hoàn thiện khi còn lỗi. Chạy Audit Team Workspace để xem toàn bộ; chưa kiểm tra Play Mode.");
        }
        finally
        {
            if (scratch.IsValid() && scratch.isLoaded)
            {
                EditorSceneManager.CloseScene(scratch, true);
            }
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneManager.SetActiveScene(previousScene);
            }
            if (openedMain && main.IsValid() && main.isLoaded)
            {
                EditorSceneManager.CloseScene(main, true);
            }
        }
    }

    private static void CreateFolders()
    {
        string[] features = { "Cows", "Production", "Building", "Orders" };
        Directory.CreateDirectory(TestFolder);
        Directory.CreateDirectory(SharedFolder);
        Directory.CreateDirectory(WorldFolder + "Buildings");
        Directory.CreateDirectory(WorldFolder + "Environment");
        for (int index = 0; index < features.Length; index++)
        {
            Directory.CreateDirectory("Assets/_Game/Scripts/Features/" + features[index]);
            Directory.CreateDirectory(FeatureFolder + features[index]);
        }
        AssetDatabase.Refresh();
    }

    private static void PrepareSharedPrefabs(Scene main)
    {
        GameObject sourcePlayer = PolishCowPlayerSetup.FindPlayer(main);
        if (!File.Exists(SharedFolder + "Player.prefab"))
        {
            GameObject player = Object.Instantiate(sourcePlayer);
            player.name = "Player";
            PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
            if (interaction == null)
            {
                interaction = player.AddComponent<PlayerInteraction>();
            }
            // UI nằm ngoài Player nên prefab để trống, gán lại tại scene.
            SetReference(interaction, "interactionHint", null);
            EnsurePlayerSorting(player);
            SaveNewPrefab(player, SharedFolder + "Player.prefab");
            Object.DestroyImmediate(player);
        }

        if (!File.Exists(SharedFolder + "TeamServices.prefab"))
        {
            GameObject services = new GameObject("TeamServices");
            GameObject systems = Object.Instantiate(FindNamed(main, "GameSystems"), services.transform);
            systems.name = "GameSystems";
            GameObject canvas = Object.Instantiate(FindNamed(main, "Canvas"), services.transform);
            canvas.name = "Canvas";
            DisableInventoryTests(systems);
            InventoryManager inventory = systems.GetComponent<InventoryManager>();
            InventoryHUD hud = canvas.GetComponentInChildren<InventoryHUD>(true);
            SetReference(hud, "inventory", inventory);
            EnsureHint(canvas);
            SaveNewPrefab(services, SharedFolder + "TeamServices.prefab");
            Object.DestroyImmediate(services);
        }

        if (!File.Exists(SharedFolder + "InteractableSample.prefab"))
        {
            GameObject sample = CreateSample("InteractableSample", LoadMarker(), "Tương tác mẫu");
            SaveNewPrefab(sample, SharedFolder + "InteractableSample.prefab");
            Object.DestroyImmediate(sample);
        }

        // Xuất hình tạm/công trình đang có nếu chưa có prefab tương ứng.
        GameObject world = FindNamed(main, "World");
        Transform map = world.transform.Find("PrototypeMap");
        SaveChildrenAsPrefabs(map.Find("Areas"), WorldFolder + "Buildings/");
        SaveChildrenAsPrefabs(map.Find("Obstacles"), WorldFolder + "Environment/");
        // Các prefab WorldArt đã có giữ nguyên; phần mới không thuộc prefab được xuất thêm.
        SaveChildrenAsPrefabs(map.Find("WorldArt"), WorldFolder + "Environment/");
    }

    private static void SaveChildrenAsPrefabs(Transform group, string folder)
    {
        if (group == null)
        {
            return;
        }
        for (int index = 0; index < group.childCount; index++)
        {
            GameObject child = group.GetChild(index).gameObject;
            if (PrefabUtility.IsPartOfPrefabInstance(child))
            {
                continue;
            }
            string path = folder + child.name + ".prefab";
            if (!File.Exists(path))
            {
                GameObject copy = Object.Instantiate(child);
                copy.name = child.name;
                copy.transform.position = Vector3.zero;
                SaveNewPrefab(copy, path);
                Object.DestroyImmediate(copy);
            }
        }
    }

    private static void PrepareFeaturePrefabs(Scene main)
    {
        TMP_FontAsset font = GetHUDFont(FindNamed(main, "Canvas"));
        string[] features = { "Cows", "Production", "Building", "Orders" };
        string[] names = { "CowSample", "ProductionSample", "BuildingSample", "OrderSample" };
        for (int index = 0; index < features.Length; index++)
        {
            string path = FeatureFolder + features[index] + "/" + names[index] + ".prefab";
            if (File.Exists(path))
            {
                continue;
            }
            GameObject sample;
            if (index == 0)
            {
                Sprite cowSprite = LoadSprite(
                    "Assets/_Game/Art/Actor/Animal/Cow/SpriteSheetWhite.png", "CowWhite_R00_C00");
                if (cowSprite == null)
                {
                    throw new System.InvalidOperationException("Thiếu sprite CowWhite_R00_C00 đã cắt.");
                }
                sample = CreateSample(names[index], cowSprite, "Kiểm tra bò mẫu");
            }
            else if (index == 1)
            {
                sample = CreateSample(names[index], LoadMarker(), "Kiểm tra máy mẫu (hình tạm)");
                sample.GetComponent<SpriteRenderer>().color = new Color(0.35f, 0.6f, 0.8f);
            }
            else if (index == 2)
            {
                sample = CreateSample(names[index], LoadMarker(), "Kiểm tra vị trí xây cố định");
                sample.GetComponent<SpriteRenderer>().color = new Color(0.3f, 0.9f, 0.8f, 0.6f);
                GameObject visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                    WorldFolder + "Buildings/FarmHouse.prefab");
                if (visualPrefab == null)
                {
                    visualPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WorldFolder + "Buildings/CowBarn.prefab");
                }
                if (visualPrefab != null)
                {
                    GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab, sample.transform);
                    visual.name = "BuildingVisual";
                    visual.transform.localPosition = new Vector3(3f, 1f, 0f);
                }
            }
            else
            {
                Sprite crate = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/WorldSprites/Crate.asset");
                sample = CreateSample(names[index], crate != null ? crate : LoadMarker(), "Kiểm tra điểm giao hàng mẫu");
                CreateOrderPanel(sample, font);
            }
            // Các điểm này là chỗ đặt hiệu ứng/biểu tượng tương tác sau này.
            GameObject point = new GameObject("InteractionPoint");
            point.transform.SetParent(sample.transform, false);
            SaveNewPrefab(sample, path);
            Object.DestroyImmediate(sample);
        }
    }

    private static GameObject CreateSample(string objectName, Sprite sprite, string text)
    {
        if (sprite == null)
        {
            throw new System.InvalidOperationException("Thiếu sprite cho " + objectName);
        }
        GameObject sample = new GameObject(objectName);
        SpriteRenderer renderer = sample.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = WorldDepthSorting.GetSortingOrder(0f);
        sample.AddComponent<WorldDepthSorting>();
        BoxCollider2D collider = sample.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.7f, 0.4f);
        // Collider nằm gần chân, không che toàn bộ sprite.
        collider.offset = new Vector2(0f, sprite.pivot.y == 0f ? 0.2f : -0.2f);
        InventoryInteractionSample interaction = sample.AddComponent<InventoryInteractionSample>();
        FeatureReferences references = sample.AddComponent<FeatureReferences>();
        SerializedObject data = new SerializedObject(interaction);
        data.FindProperty("interactionText").stringValue = text;
        data.FindProperty("featureReferences").objectReferenceValue = references;
        data.ApplyModifiedPropertiesWithoutUndo();
        return sample;
    }

    private static void CreateOrderPanel(GameObject sample, TMP_FontAsset font)
    {
        GameObject canvasObject = new GameObject("OrderCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(sample.transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("OrderPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.one;
        panelRect.anchorMax = Vector2.one;
        panelRect.pivot = Vector2.one;
        panelRect.anchoredPosition = new Vector2(-24f, -24f);
        panelRect.sizeDelta = new Vector2(400f, 240f);
        panel.GetComponent<Image>().color = new Color(0.12f, 0.17f, 0.2f, 0.95f);
        TextMeshProUGUI title = CreateText(panel.transform, "OrderDescription", font,
            "ĐƠN HÀNG MẪU\nSữa chai: 3\nChưa có logic giao hàng", new Vector2(370f, 130f));
        title.rectTransform.anchoredPosition = new Vector2(0f, 40f);
        title.fontSize = 26f;

        GameObject buttonObject = new GameObject("DeliverButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(panel.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 16f);
        buttonRect.sizeDelta = new Vector2(330f, 55f);
        buttonObject.GetComponent<Image>().color = new Color(0.2f, 0.55f, 0.35f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        CreateText(buttonObject.transform, "Label", font, "Giao hàng (thử Console)", new Vector2(320f, 50f));
        InventoryInteractionSample interaction = sample.GetComponent<InventoryInteractionSample>();
        UnityEventTools.AddPersistentListener(button.onClick, interaction.Interact);
    }

    private static void CreateTestScene(string sceneName, string feature)
    {
        string path = TestFolder + sceneName + ".unity";
        if (File.Exists(path))
        {
            Debug.Log("Không ghi đè scene đã có; đang Audit: " + path);
            PolishCowWorkspaceAudit.AuditSavedScene(path);
            return;
        }
        if (!AssetDatabase.CopyAsset(MainPath, path))
        {
            throw new System.InvalidOperationException("Không sao chép được Main sang " + path);
        }
        Scene scene = default(Scene);
        bool saved = false;
        try
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            GameObject player = PolishCowPlayerSetup.FindPlayer(scene);
            GameObject canvas = FindNamed(scene, "Canvas");
            GameObject systems = FindNamed(scene, "GameSystems");
            InventoryManager inventory = systems.GetComponent<InventoryManager>();
            DisableInventoryTests(systems);
            GameObject services = FindNamed(scene, "TeamServices");
            if (services == null)
            {
                services = new GameObject("TeamServices");
            }
            systems.transform.SetParent(services.transform, true);
            canvas.transform.SetParent(services.transform, true);
            TextMeshProUGUI hint = EnsureHint(canvas);
            PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
            if (interaction == null)
            {
                interaction = player.AddComponent<PlayerInteraction>();
            }
            interaction.enabled = true;
            SetReference(interaction, "interactionHint", hint);
            SerializedObject interactionData = new SerializedObject(interaction);
            interactionData.FindProperty("interactionRadius").floatValue = 1.5f;
            interactionData.FindProperty("interactionLayerMask").intValue = ~0;
            interactionData.ApplyModifiedPropertiesWithoutUndo();
            player.transform.position = new Vector3(-1f, -3.5f, 0f);
            EnsurePlayerSorting(player);
            GameObject oldTest = FindNamed(scene, "InteractionTest");
            if (oldTest != null)
            {
                oldTest.SetActive(false);
            }

            GameObject work = FindNamed(scene, "FeatureWork");
            if (work == null)
            {
                work = new GameObject("FeatureWork");
            }
            string sampleName;
            if (feature == "Cows")
            {
                sampleName = "CowSample";
            }
            else if (feature == "Production")
            {
                sampleName = "ProductionSample";
            }
            else if (feature == "Building")
            {
                sampleName = "BuildingSample";
            }
            else
            {
                sampleName = "OrderSample";
            }
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                FeatureFolder + feature + "/" + sampleName + ".prefab");
            GameObject sample = (GameObject)PrefabUtility.InstantiatePrefab(prefab, work.transform);
            sample.transform.localPosition = new Vector3(1f, -3.5f, 0f);
            InventoryInteractionSample[] sampleInteractions = sample.GetComponentsInChildren<InventoryInteractionSample>(true);
            for (int index = 0; index < sampleInteractions.Length; index++)
            {
                SetReference(sampleInteractions[index], "inventory", inventory);
                PrefabUtility.RecordPrefabInstancePropertyModifications(sampleInteractions[index]);
                if (!CheckReferences(sampleInteractions[index], new string[] { "inventory" }))
                {
                    throw new System.InvalidOperationException("Không gán được kho cho đối tượng mẫu.");
                }
            }
            if (sampleInteractions.Length == 0)
            {
                Debug.LogWarning("Prefab đã có không dùng InventoryInteractionSample. Giữ prefab, hãy gán kho cho component tính năng của thành viên trong scene.");
            }
            if (!PolishCowIndependentFeatureSetup.ConfigureSceneConnections(scene) ||
                !ValidateBase(scene, true) || !PolishCowWorkspaceAudit.AuditScene(scene))
            {
                throw new System.InvalidOperationException("Scene thử còn thiếu tham chiếu: " + path);
            }
            saved = EditorSceneManager.SaveScene(scene);
            if (!saved)
            {
                throw new System.InvalidOperationException("Không lưu được scene thử: " + path);
            }
            Debug.Log("Đã tạo và gán tham chiếu: " + path);
        }
        finally
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
            // Chỉ dọn bản sao vừa tạo nếu lỗi; không xóa scene đã tồn tại.
            if (!saved)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }

    private static bool ValidateBase(Scene scene, bool requireInteraction)
    {
        bool valid = true;
        GameObject player = PolishCowPlayerSetup.FindPlayer(scene);
        GameObject systems = FindNamed(scene, "GameSystems");
        GameObject canvas = FindNamed(scene, "Canvas");
        GameObject camera = FindNamed(scene, "Main Camera");
        GameObject light = FindNamed(scene, "Global Light 2D");
        GameObject world = FindNamed(scene, "World");
        GameObject eventSystem = FindNamed(scene, "EventSystem");
        if (player == null || systems == null || canvas == null || camera == null ||
            light == null || world == null || eventSystem == null)
        {
            Debug.LogError(scene.name + ": Thiếu đối tượng base. Không tạo thêm hệ thống thay thế.");
            return false;
        }
        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Collider2D collider = player.GetComponent<Collider2D>();
        if (movement == null || !movement.enabled || body == null || !body.simulated ||
            body.bodyType != RigidbodyType2D.Dynamic || body.gravityScale != 0f ||
            collider == null || !collider.enabled || collider.isTrigger || !player.activeInHierarchy)
        {
            Debug.LogError(scene.name + ": Player thiếu movement/Rigidbody2D/collider hợp lệ.");
            valid = false;
        }
        if (movement != null && !CheckReferences(movement, new string[] {
            "playerRigidbody", "playerSpriteRenderer", "upSprite", "downSprite", "leftSprite", "rightSprite" }))
        {
            valid = false;
        }
        InventoryManager inventory = systems.GetComponent<InventoryManager>();
        InventoryHUD hud = canvas.GetComponentInChildren<InventoryHUD>(true);
        if (inventory == null || !inventory.isActiveAndEnabled || hud == null || !hud.isActiveAndEnabled)
        {
            Debug.LogError(scene.name + ": Thiếu kho/HUD đang bật.");
            valid = false;
        }
        else if (!CheckReferences(hud, new string[] {
            "inventory", "creditsText", "feedText", "rawMilkText", "bottledMilkText" }))
        {
            valid = false;
        }
        if (hud != null && inventory != null &&
            new SerializedObject(hud).FindProperty("inventory").objectReferenceValue != inventory)
        {
            Debug.LogError(scene.name + ": HUD phải dùng InventoryManager của chính scene này.");
            valid = false;
        }
        string[] icons = { "CreditsIcon", "FeedIcon", "RawMilkIcon", "BottledMilkIcon" };
        for (int index = 0; index < icons.Length; index++)
        {
            GameObject iconObject = FindNamed(scene, icons[index]);
            Image icon = iconObject != null ? iconObject.GetComponent<Image>() : null;
            if (icon == null || icon.sprite == null)
            {
                Debug.LogError(scene.name + ": Thiếu icon " + icons[index]);
                valid = false;
            }
        }
        Camera mainCamera = camera.GetComponent<Camera>();
        Transform map = world.transform.Find("PrototypeMap");
        Light2D sceneLight = light.GetComponent<Light2D>();
        if (mainCamera == null || !mainCamera.isActiveAndEnabled || sceneLight == null || !sceneLight.isActiveAndEnabled ||
            map == null || map.GetComponentInChildren<Tilemap>() == null ||
            eventSystem.GetComponent<EventSystem>() == null || eventSystem.GetComponent<InputSystemUIInputModule>() == null ||
            GetHUDFont(canvas) == null)
        {
            Debug.LogError(scene.name + ": Kiểm tra camera/ánh sáng/nền/EventSystem/font HUD.");
            valid = false;
        }
        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (interaction == null)
        {
            Debug.LogWarning(scene.name + ": Chưa gắn PlayerInteraction. Menu sẽ gắn ở bản sao scene thử.");
            if (requireInteraction)
            {
                valid = false;
            }
        }
        else if (requireInteraction)
        {
            if (!interaction.isActiveAndEnabled || !CheckReferences(interaction, new string[] { "interactionHint" }))
            {
                valid = false;
            }
        }
        GameObject[] roots = scene.GetRootGameObjects();
        int inventoryCount = 0;
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            inventoryCount += roots[rootIndex].GetComponentsInChildren<InventoryManager>(true).Length;
            Transform[] transforms = roots[rootIndex].GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < transforms.Length; index++)
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transforms[index].gameObject) > 0)
                {
                    Debug.LogError(scene.name + ": Missing Script tại " + transforms[index].name);
                    valid = false;
                }
            }
        }
        if (inventoryCount != 1)
        {
            Debug.LogError(scene.name + ": Cần đúng một InventoryManager, hiện có " + inventoryCount);
            valid = false;
        }
        return valid;
    }

    private static bool CheckReferences(Component component, string[] fields)
    {
        bool valid = true;
        SerializedObject data = new SerializedObject(component);
        for (int index = 0; index < fields.Length; index++)
        {
            SerializedProperty field = data.FindProperty(fields[index]);
            if (field == null || field.objectReferenceValue == null)
            {
                Debug.LogError(component.name + ": Chưa gán " + fields[index], component);
                valid = false;
            }
        }
        return valid;
    }

    private static TextMeshProUGUI EnsureHint(GameObject canvas)
    {
        Transform hud = canvas.transform.Find("HUD");
        Transform existing = hud.Find("InteractionHint");
        if (existing != null)
        {
            TextMeshProUGUI existingText = existing.GetComponent<TextMeshProUGUI>();
            if (existingText == null)
            {
                throw new System.InvalidOperationException("InteractionHint hiện có không phải TextMeshProUGUI.");
            }
            return existingText;
        }
        TextMeshProUGUI hint = CreateText(hud, "InteractionHint", GetHUDFont(canvas),
            "Nhấn E: Tương tác", new Vector2(900f, 60f));
        RectTransform rect = hint.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 40f);
        hint.gameObject.SetActive(false);
        return hint;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string objectName,
        TMP_FontAsset font, string value, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        textObject.layer = parent.gameObject.layer;
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 30f;
        text.text = value;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        text.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        text.rectTransform.sizeDelta = size;
        return text;
    }

    private static TMP_FontAsset GetHUDFont(GameObject canvas)
    {
        Transform credits = canvas.transform.Find("HUD/CreditsText");
        TextMeshProUGUI text = credits != null ? credits.GetComponent<TextMeshProUGUI>() : null;
        return text != null ? text.font : null;
    }

    private static void DisableInventoryTests(GameObject systems)
    {
        InventoryTester[] tests = systems.GetComponentsInChildren<InventoryTester>(true);
        for (int index = 0; index < tests.Length; index++)
        {
            tests[index].enabled = false;
        }
    }

    private static void EnsurePlayerSorting(GameObject player)
    {
        WorldDepthSorting sorting = player.GetComponent<WorldDepthSorting>();
        if (sorting == null)
        {
            sorting = player.AddComponent<WorldDepthSorting>();
            SerializedObject data = new SerializedObject(sorting);
            data.FindProperty("feetOffsetY").floatValue = -0.25f;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        float feetOffset = new SerializedObject(sorting).FindProperty("feetOffsetY").floatValue;
        player.GetComponent<SpriteRenderer>().sortingOrder =
            WorldDepthSorting.GetSortingOrder(player.transform.position.y + feetOffset);
    }

    private static void SetReference(Component component, string fieldName, Object reference)
    {
        SerializedObject data = new SerializedObject(component);
        data.FindProperty(fieldName).objectReferenceValue = reference;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SaveNewPrefab(GameObject template, string path)
    {
        if (!File.Exists(path))
        {
            if (PrefabUtility.SaveAsPrefabAsset(template, path) == null)
            {
                throw new System.InvalidOperationException("Không lưu được prefab: " + path);
            }
        }
    }

    private static Sprite LoadMarker()
    {
        return LoadSprite("Assets/_Game/Data/Prototype/PrototypeMarker.asset", "PrototypeMarkerSprite");
    }

    private static Sprite LoadSprite(string path, string spriteName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        for (int index = 0; index < assets.Length; index++)
        {
            Sprite sprite = assets[index] as Sprite;
            if (sprite != null && sprite.name == spriteName)
            {
                return sprite;
            }
        }
        return null;
    }

    private static GameObject FindNamed(Scene scene, string objectName)
    {
        GameObject found = null;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
        {
            Transform[] children = roots[rootIndex].GetComponentsInChildren<Transform>(true);
            for (int index = 0; index < children.Length; index++)
            {
                if (children[index].name == objectName)
                {
                    if (found != null)
                    {
                        Debug.LogError(scene.name + ": Có nhiều đối tượng tên " + objectName);
                        return null;
                    }
                    found = children[index].gameObject;
                }
            }
        }
        return found;
    }
}
