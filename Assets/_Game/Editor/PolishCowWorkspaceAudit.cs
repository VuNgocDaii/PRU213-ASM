using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PolishCowWorkspaceAudit
{
    private const string MainPath = "Assets/_Game/Scenes/Main.unity";
    private const string TestFolder = "Assets/_Game/Scenes/Tests/";

    [MenuItem("Tools/PolishCow/Audit Team Workspace")]
    public static void AuditWorkspace()
    {
        string[] paths = { MainPath, TestFolder + "CowTest.unity", TestFolder + "ProductionTest.unity",
            TestFolder + "BuildingTest.unity", TestFolder + "OrderTest.unity" };
        int passed = 0;
        for (int index = 0; index < paths.Length; index++)
        {
            if (AuditSavedScene(paths[index]))
            {
                passed++;
            }
        }
        Debug.Log("Audit: " + passed + "/5 scene đạt kiểm tra component/tham chiếu. Đây không phải kiểm tra Play Mode.");
    }

    public static bool AuditSavedScene(string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Hãy thoát Play Mode trước khi Audit.");
            return false;
        }
        if (!File.Exists(path))
        {
            Debug.LogError("CHƯA CÓ SCENE TRÊN ĐĨA: " + path);
            return false;
        }
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = SceneManager.GetSceneByPath(path);
        bool opened = !scene.isLoaded;
        if (opened)
        {
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }
        try
        {
            if (scene.isDirty)
            {
                Debug.LogWarning(path + ": đang kiểm tra trạng thái Editor chưa lưu, không phải bản trên đĩa.");
            }
            return AuditScene(scene);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded)
            {
                SceneManager.SetActiveScene(previous);
            }
            if (opened)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }
    }

    [MenuItem("Tools/PolishCow/Repair Current Test Scene References")]
    public static void RepairCurrentTest()
    {
        Scene scene = SceneManager.GetActiveScene();
        string[] names = { "CowTest", "ProductionTest", "BuildingTest", "OrderTest" };
        bool isTest = false;
        for (int index = 0; index < names.Length; index++)
        {
            if (scene.path == TestFolder + names[index] + ".unity")
            {
                isTest = true;
            }
        }
        if (!isTest)
        {
            Debug.LogError("Hãy mở một trong bốn scene Tests và đặt làm active scene.");
            return;
        }
        RepairScene(scene, false);
    }

    public static void SetupMainInteraction()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != MainPath)
        {
            Debug.LogError("Hãy mở Main và đặt làm active scene.");
            return;
        }
        RepairScene(scene, true);
    }

    public static bool AuditScene(Scene scene)
    {
        bool valid = true;
        List<GameObject> players = FindPlayers(scene);
        List<InventoryManager> inventories = Collect<InventoryManager>(scene);
        List<InventoryHUD> huds = Collect<InventoryHUD>(scene);
        List<Canvas> canvases = FindHUDCanvases(scene);
        List<EventSystem> events = Collect<EventSystem>(scene);
        valid = CheckCount(scene, "Player", players.Count) && valid;
        valid = CheckCount(scene, "InventoryManager", inventories.Count) && valid;
        valid = CheckCount(scene, "Canvas HUD", canvases.Count) && valid;
        valid = CheckCount(scene, "InventoryHUD", huds.Count) && valid;
        valid = CheckCount(scene, "EventSystem", events.Count) && valid;
        InventoryManager inventory = inventories.Count == 1 ? inventories[0] : null;
        if (inventory != null && !inventory.isActiveAndEnabled)
        {
            Report(scene, "InventoryManager đang tắt.", inventory);
            valid = false;
        }
        if (players.Count == 1)
        {
            GameObject player = players[0];
            PlayerMovement movement = player.GetComponent<PlayerMovement>();
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            Collider2D collider = player.GetComponent<Collider2D>();
            if (movement == null || !movement.isActiveAndEnabled || body == null ||
                !body.simulated || body.bodyType != RigidbodyType2D.Dynamic || body.gravityScale != 0f ||
                collider == null || !collider.enabled || collider.isTrigger)
            {
                Report(scene, "Player thiếu hoặc tắt movement/Rigidbody2D/collider hợp lệ.", player);
                valid = false;
            }
            else
            {
                valid = CheckFields(scene, movement, new string[] { "playerRigidbody", "playerSpriteRenderer",
                    "upSprite", "downSprite", "leftSprite", "rightSprite" }) && valid;
            }
            PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
            if (interaction == null || !interaction.isActiveAndEnabled)
            {
                Report(scene, "Player thiếu hoặc tắt PlayerInteraction.", player);
                valid = false;
            }
            else
            {
                valid = CheckFields(scene, interaction, new string[] { "interactionHint" }) && valid;
                SerializedObject data = new SerializedObject(interaction);
                TextMeshProUGUI hint = data.FindProperty("interactionHint").objectReferenceValue as TextMeshProUGUI;
                if (hint != null && (hint.gameObject.scene != scene || !hint.enabled ||
                    hint.GetComponentInParent<Canvas>() == null))
                {
                    Report(scene, "Interaction Hint phải là chữ UI đang bật component trong chính scene.", interaction);
                    valid = false;
                }
                if (data.FindProperty("interactionRadius").floatValue <= 0f ||
                    data.FindProperty("interactionLayerMask").intValue == 0)
                {
                    Report(scene, "Interaction Radius phải > 0 và LayerMask không được Nothing.", interaction);
                    valid = false;
                }
            }
        }
        if (huds.Count == 1)
        {
            InventoryHUD hud = huds[0];
            valid = CheckFields(scene, hud, new string[] {
                "inventory", "creditsText", "feedText", "rawMilkText", "bottledMilkText" }) && valid;
            if (!hud.isActiveAndEnabled || hud.GetComponentInParent<Canvas>() == null)
            {
                Report(scene, "InventoryHUD đang tắt hoặc không nằm trong Canvas.", hud);
                valid = false;
            }
        }
        if (events.Count == 1 && (!events[0].isActiveAndEnabled ||
            events[0].GetComponent<InputSystemUIInputModule>() == null ||
            !events[0].GetComponent<InputSystemUIInputModule>().isActiveAndEnabled))
        {
            Report(scene, "EventSystem cần InputSystemUIInputModule đang bật.", events[0]);
            valid = false;
        }
        if (canvases.Count == 1)
        {
            if (!canvases[0].isActiveAndEnabled)
            {
                Report(scene, "Canvas HUD đang tắt.", canvases[0]);
                valid = false;
            }
            string[] icons = { "CreditsIcon", "FeedIcon", "RawMilkIcon", "BottledMilkIcon" };
            for (int index = 0; index < icons.Length; index++)
            {
                GameObject iconObject = FindNamedUnder(canvases[0].transform, icons[index]);
                Image image = iconObject != null ? iconObject.GetComponent<Image>() : null;
                if (image == null || image.sprite == null)
                {
                    Report(scene, "Thiếu icon hoặc sprite " + icons[index], canvases[0]);
                    valid = false;
                }
            }
        }
        List<MonoBehaviour> scripts = Collect<MonoBehaviour>(scene);
        int inventoryFields = 0;
        for (int index = 0; index < scripts.Count; index++)
        {
            MonoBehaviour script = scripts[index];
            if (script == null)
            {
                Report(scene, "Có Missing Script.", null);
                valid = false;
                continue;
            }
            SerializedObject data = new SerializedObject(script);
            SerializedProperty field = data.GetIterator();
            while (field.NextVisible(true))
            {
                // Bao gồm component tính năng do thành viên viết, cả đối tượng đang tắt.
                if (IsInventoryField(field))
                {
                    inventoryFields++;
                    if (inventory == null || field.objectReferenceValue != inventory)
                    {
                        Report(scene, script.name + "." + field.propertyPath +
                            " chưa trỏ đúng kho duy nhất của scene.", script);
                        valid = false;
                    }
                }
                else if (!(script is FeatureReferenceInitializer) &&
                    field.propertyType == SerializedPropertyType.ObjectReference && field.type.Contains("FeatureReferences"))
                {
                    FeatureReferences reference = field.objectReferenceValue as FeatureReferences;
                    if (reference == null || reference.gameObject.scene != scene)
                    {
                        Report(scene, script.name + "." + field.propertyPath + " chưa gán FeatureReferences của scene.", script);
                        valid = false;
                    }
                }
            }
        }
        List<FeatureReferences> featureRoots = Collect<FeatureReferences>(scene);
        List<FeatureReferenceInitializer> initializers = Collect<FeatureReferenceInitializer>(scene);
        for (int index = 0; index < initializers.Count; index++)
        {
            if (!initializers[index].isActiveAndEnabled)
            {
                Report(scene, "FeatureReferenceInitializer đang tắt.", initializers[index]);
                valid = false;
            }
        }
        for (int index = 0; index < featureRoots.Count; index++)
        {
            if (Application.isPlaying)
            {
                if (inventory == null || featureRoots[index].Inventory != inventory)
                {
                    Report(scene, featureRoots[index].name + ": chưa Initialize đúng kho.", featureRoots[index]);
                    valid = false;
                }
            }
            else if (!PolishCowIndependentFeatureSetup.IsListed(featureRoots[index], initializers))
            {
                Report(scene, featureRoots[index].name + ": chưa có trong danh sách FeatureReferenceInitializer.", featureRoots[index]);
                valid = false;
            }
        }
        List<InventoryInteractionSample> samples = Collect<InventoryInteractionSample>(scene);
        for (int index = 0; index < samples.Count; index++)
        {
            valid = CheckFields(scene, samples[index], new string[] { "featureReferences" }) && valid;
        }
        List<BuildingConnectionSample> buildingTests = Collect<BuildingConnectionSample>(scene);
        for (int index = 0; index < buildingTests.Count; index++)
        {
            valid = CheckFields(scene, buildingTests[index], new string[] {
                "featureReferences", "buildingPrefab", "spawnPoint" }) && valid;
        }
        if (scene.path != MainPath && FindNamed(scene, "FeatureWork") == null)
        {
            Report(scene, "Thiếu nhóm FeatureWork.", null);
            valid = false;
        }
        if (scene.path == MainPath && FindNamed(scene, "InteractionTest") == null)
        {
            Report(scene, "Thiếu đối tượng InteractionTest trong Main.", null);
            valid = false;
        }
        List<Interactable> targets = Collect<Interactable>(scene);
        if (targets.Count == 0)
        {
            Report(scene, "Chưa có Interactable để thử nhấn E.", null);
            valid = false;
        }
        int mask = 0;
        if (players.Count == 1 && players[0].GetComponent<PlayerInteraction>() != null)
        {
            mask = new SerializedObject(players[0].GetComponent<PlayerInteraction>()).FindProperty("interactionLayerMask").intValue;
        }
        int usableTargets = 0;
        for (int index = 0; index < targets.Count; index++)
        {
            Interactable target = targets[index];
            // Đối tượng bị tắt có thể thuộc logic thành viên; không tự bật nó.
            if (!target.isActiveAndEnabled)
            {
                Debug.LogWarning(scene.name + ": Interactable đang tắt: " + target.name, target);
                continue;
            }
            Collider2D[] colliders = target.GetComponentsInChildren<Collider2D>(true);
            bool reachable = false;
            for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
            {
                Collider2D collider = colliders[colliderIndex];
                if (collider.enabled && collider.gameObject.activeInHierarchy &&
                    (mask & (1 << collider.gameObject.layer)) != 0 &&
                    (!collider.isTrigger || Physics2D.queriesHitTriggers))
                {
                    reachable = true;
                }
            }
            if (!reachable)
            {
                Report(scene, target.name + ": Không có collider đang bật thuộc LayerMask, hoặc trigger đang bị Physics2D bỏ qua.", target);
                valid = false;
            }
            else
            {
                usableTargets++;
            }
        }
        if (usableTargets == 0)
        {
            Report(scene, "Không có mục tiêu đang bật với collider/layer hợp lệ để thử E.", null);
            valid = false;
        }
        Debug.Log(scene.path + ": Player=" + players.Count + ", Inventory=" + inventories.Count +
            ", HUD Canvas=" + canvases.Count + ", EventSystem=" + events.Count +
            ", ô tham chiếu kho=" + inventoryFields + ". Kết quả: " + (valid ? "ĐẠT AUDIT" : "CÒN THIẾU/LỖI"));
        return valid;
    }

    private static void RepairScene(Scene scene, bool addMainSample)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Hãy thoát Play Mode trước khi sửa bổ sung.");
            return;
        }
        List<GameObject> players = FindPlayers(scene);
        List<InventoryManager> inventories = Collect<InventoryManager>(scene);
        List<Canvas> canvases = FindHUDCanvases(scene);
        List<InventoryHUD> huds = Collect<InventoryHUD>(scene);
        List<EventSystem> events = Collect<EventSystem>(scene);
        if (players.Count != 1 || inventories.Count != 1 || canvases.Count != 1 ||
            huds.Count > 1 || events.Count > 1)
        {
            Debug.LogError("Không chọn tự động khi thiếu/trùng Player, kho, Canvas HUD hoặc EventSystem. Hãy xem Audit và xử lý các đối tượng này trong Inspector.");
            AuditScene(scene);
            return;
        }
        string[] uniqueNames = { "HUD", "InteractionHint", "InteractionTest", "FeatureWork" };
        List<Transform> transforms = Collect<Transform>(scene);
        for (int nameIndex = 0; nameIndex < uniqueNames.Length; nameIndex++)
        {
            int count = 0;
            for (int index = 0; index < transforms.Count; index++)
            {
                if (transforms[index].name == uniqueNames[nameIndex])
                {
                    count++;
                }
            }
            if (count > 1)
            {
                Report(scene, "Trùng tên " + uniqueNames[nameIndex] + "; không sửa hoặc tạo thêm. Hãy kiểm tra Hierarchy.", null);
                return;
            }
        }
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Repair PolishCow Scene References");
        InventoryManager inventory = inventories[0];
        Canvas canvas = canvases[0];
        GameObject hudObject = FindNamedUnder(canvas.transform, "HUD");
        if (hudObject == null)
        {
            Debug.LogError("Thiếu nhóm HUD dưới Canvas. Giữ scene; hãy bổ sung hoặc chọn đúng HUD trước.");
            return;
        }
        InventoryHUD hud = huds.Count == 1 ? huds[0] : Undo.AddComponent<InventoryHUD>(hudObject);
        string[] fields = { "creditsText", "feedText", "rawMilkText", "bottledMilkText" };
        string[] names = { "CreditsText", "FeedText", "RawMilkText", "BottledMilkText" };
        SerializedObject hudData = new SerializedObject(hud);
        for (int index = 0; index < fields.Length; index++)
        {
            SerializedProperty field = hudData.FindProperty(fields[index]);
            TextMeshProUGUI current = field.objectReferenceValue as TextMeshProUGUI;
            if (current == null || current.gameObject.scene != scene)
            {
                GameObject textObject = FindNamedUnder(canvas.transform, names[index]);
                TextMeshProUGUI text = textObject != null ? textObject.GetComponent<TextMeshProUGUI>() : null;
                if (text != null)
                {
                    field.objectReferenceValue = text;
                }
            }
        }
        hudData.ApplyModifiedProperties();
        GameObject player = players[0];
        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        if (interaction == null)
        {
            interaction = Undo.AddComponent<PlayerInteraction>(player);
        }
        SerializedObject interactionData = new SerializedObject(interaction);
        TextMeshProUGUI hint = interactionData.FindProperty("interactionHint").objectReferenceValue as TextMeshProUGUI;
        if (hint == null || hint.gameObject.scene != scene)
        {
            GameObject hintObject = FindNamedUnder(canvas.transform, "InteractionHint");
            if (hintObject == null)
            {
                hintObject = new GameObject("InteractionHint", typeof(RectTransform));
                hintObject.transform.SetParent(hudObject.transform, false);
                hintObject.layer = hudObject.layer;
                Undo.RegisterCreatedObjectUndo(hintObject, "Create Interaction Hint");
                hint = Undo.AddComponent<TextMeshProUGUI>(hintObject);
                TextMeshProUGUI[] sourceTexts = hudObject.GetComponentsInChildren<TextMeshProUGUI>(true);
                for (int index = 0; index < sourceTexts.Length; index++)
                {
                    if (sourceTexts[index] != hint && sourceTexts[index].font != null)
                    {
                        hint.font = sourceTexts[index].font;
                        break;
                    }
                }
                hint.fontSize = 30f;
                hint.alignment = TextAlignmentOptions.Center;
                hint.raycastTarget = false;
                hint.text = "Nhấn E: Tương tác";
                RectTransform rect = hint.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.sizeDelta = new Vector2(900f, 60f);
                rect.anchoredPosition = new Vector2(0f, 40f);
                hintObject.SetActive(false);
            }
            else
            {
                hint = hintObject.GetComponent<TextMeshProUGUI>();
            }
            interactionData.FindProperty("interactionHint").objectReferenceValue = hint;
        }
        interactionData.ApplyModifiedProperties();
        Undo.RecordObject(interaction, "Enable Player Interaction");
        interaction.enabled = true;
        if (hint != null)
        {
            Undo.RecordObject(hint, "Enable Interaction Hint Component");
            hint.enabled = true;
        }
        if (events.Count == 0)
        {
            GameObject eventObject = new GameObject("EventSystem");
            SceneManager.MoveGameObjectToScene(eventObject, scene);
            Undo.RegisterCreatedObjectUndo(eventObject, "Create Missing EventSystem");
            Undo.AddComponent<EventSystem>(eventObject);
            InputSystemUIInputModule module = Undo.AddComponent<InputSystemUIInputModule>(eventObject);
            module.AssignDefaultActions();
        }
        else if (events[0].GetComponent<BaseInputModule>() == null)
        {
            InputSystemUIInputModule module = Undo.AddComponent<InputSystemUIInputModule>(events[0].gameObject);
            module.AssignDefaultActions();
        }
        // Chỉ sửa các ô InventoryManager; không đụng dữ liệu, vị trí hoặc logic thành viên.
        List<MonoBehaviour> scripts = Collect<MonoBehaviour>(scene);
        for (int index = 0; index < scripts.Count; index++)
        {
            if (scripts[index] == null)
            {
                continue;
            }
            SerializedObject data = new SerializedObject(scripts[index]);
            SerializedProperty field = data.GetIterator();
            while (field.NextVisible(true))
            {
                if (IsInventoryField(field) && field.objectReferenceValue != inventory)
                {
                    Debug.Log(scene.name + ": gán lại " + scripts[index].name + "." + field.propertyPath, scripts[index]);
                    field.objectReferenceValue = inventory;
                }
            }
            data.ApplyModifiedProperties();
        }
        if (!addMainSample && FindNamed(scene, "FeatureWork") == null)
        {
            GameObject work = new GameObject("FeatureWork");
            SceneManager.MoveGameObjectToScene(work, scene);
            Undo.RegisterCreatedObjectUndo(work, "Create Missing FeatureWork");
        }
        if (addMainSample)
        {
            EnsureMainSample(scene, player, interaction);
        }
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        bool valid = AuditScene(scene);
        Debug.Log(scene.name + ": " + (valid ? "Đạt Audit sau bổ sung." : "Vẫn còn lỗi, xem Audit phía trên.") +
            " Chưa lưu. Hãy kiểm tra rồi Ctrl+S; có thể Undo. Chưa xác nhận Play Mode.");
    }

    private static void EnsureMainSample(Scene scene, GameObject player, PlayerInteraction interaction)
    {
        GameObject sample = FindNamed(scene, "InteractionTest");
        if (sample == null)
        {
            Sprite sprite = null;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Data/Prototype/PrototypeMarker.asset");
            for (int index = 0; index < assets.Length; index++)
            {
                if (assets[index] is Sprite)
                {
                    sprite = (Sprite)assets[index];
                    break;
                }
            }
            if (sprite == null)
            {
                Debug.LogError("Thiếu PrototypeMarker: chưa tạo được mẫu tương tác trong Main.");
                return;
            }
            sample = new GameObject("InteractionTest");
            SceneManager.MoveGameObjectToScene(sample, scene);
            sample.transform.position = player.transform.position + Vector3.right * 2f;
            sample.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            Undo.RegisterCreatedObjectUndo(sample, "Create Main Interaction Sample");
            SpriteRenderer renderer = Undo.AddComponent<SpriteRenderer>(sample);
            renderer.sprite = sprite;
            renderer.color = Color.yellow;
            renderer.sortingOrder = player.GetComponent<SpriteRenderer>().sortingOrder + 1;
        }
        if (sample.GetComponent<Interactable>() == null)
        {
            Undo.AddComponent<Interactable>(sample);
        }
        if (sample.GetComponentInChildren<Collider2D>(true) == null)
        {
            Undo.AddComponent<BoxCollider2D>(sample);
        }
        Undo.RecordObject(sample, "Enable Main Interaction Sample");
        sample.SetActive(true);
        Interactable target = sample.GetComponent<Interactable>();
        Undo.RecordObject(target, "Enable Main Sample Interaction");
        target.enabled = true;
        Collider2D singleCollider = sample.GetComponent<Collider2D>();
        if (singleCollider != null)
        {
            Undo.RecordObject(singleCollider, "Enable Main Sample Collider");
            singleCollider.enabled = true;
        }
        // Thêm layer của mẫu vào mask, giữ các layer khác người dùng chọn.
        SerializedObject data = new SerializedObject(interaction);
        SerializedProperty mask = data.FindProperty("interactionLayerMask");
        Collider2D[] sampleColliders = sample.GetComponentsInChildren<Collider2D>(true);
        for (int index = 0; index < sampleColliders.Length; index++)
        {
            mask.intValue = mask.intValue | (1 << sampleColliders[index].gameObject.layer);
        }
        data.ApplyModifiedProperties();
        Selection.activeGameObject = sample;
    }

    private static bool IsInventoryField(SerializedProperty field)
    {
        return field.propertyType == SerializedPropertyType.ObjectReference &&
            (field.type.Contains("InventoryManager") || field.objectReferenceValue is InventoryManager);
    }

    private static bool CheckFields(Scene scene, Component component, string[] fields)
    {
        bool valid = true;
        SerializedObject data = new SerializedObject(component);
        for (int index = 0; index < fields.Length; index++)
        {
            SerializedProperty field = data.FindProperty(fields[index]);
            Object reference = field != null ? field.objectReferenceValue : null;
            Component target = reference as Component;
            if (reference == null || (target != null && target.gameObject.scene != scene))
            {
                Report(scene, component.name + "." + fields[index] + " thiếu hoặc trỏ ngoài scene.", component);
                valid = false;
            }
            TextMeshProUGUI text = reference as TextMeshProUGUI;
            if (text != null && (text.font == null || !text.enabled))
            {
                Report(scene, component.name + "." + fields[index] + " thiếu font hoặc component chữ đang tắt.", text);
                valid = false;
            }
        }
        return valid;
    }

    private static bool CheckCount(Scene scene, string label, int count)
    {
        if (count != 1)
        {
            Report(scene, "Cần đúng một " + label + ", hiện có " + count + " (tính cả đối tượng đang tắt).", null);
            return false;
        }
        return true;
    }

    private static void Report(Scene scene, string message, Object context)
    {
        Debug.LogError(scene.path + ": " + message, context);
    }

    public static List<T> Collect<T>(Scene scene) where T : Component
    {
        List<T> result = new List<T>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int index = 0; index < roots.Length; index++)
        {
            T[] components = roots[index].GetComponentsInChildren<T>(true);
            for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
            {
                result.Add(components[componentIndex]);
            }
        }
        return result;
    }

    private static List<GameObject> FindPlayers(Scene scene)
    {
        List<GameObject> result = new List<GameObject>();
        List<Transform> transforms = Collect<Transform>(scene);
        for (int index = 0; index < transforms.Count; index++)
        {
            GameObject target = transforms[index].gameObject;
            if (target.name == "Player" || target.GetComponent<PlayerMovement>() != null ||
                target.GetComponent<PlayerInteraction>() != null)
            {
                result.Add(target);
            }
        }
        return result;
    }

    private static List<Canvas> FindHUDCanvases(Scene scene)
    {
        List<Canvas> result = new List<Canvas>();
        List<Canvas> canvases = Collect<Canvas>(scene);
        for (int index = 0; index < canvases.Count; index++)
        {
            Canvas canvas = canvases[index];
            if (canvas.GetComponentInChildren<InventoryHUD>(true) != null ||
                (canvas.name == "Canvas" && canvas.transform.Find("HUD") != null))
            {
                result.Add(canvas);
            }
        }
        return result;
    }

    private static GameObject FindNamed(Scene scene, string name)
    {
        List<Transform> transforms = Collect<Transform>(scene);
        GameObject found = null;
        for (int index = 0; index < transforms.Count; index++)
        {
            if (transforms[index].name == name)
            {
                if (found != null)
                {
                    Report(scene, "Trùng tên " + name + "; không tự chọn hoặc xóa.", found);
                    return null;
                }
                found = transforms[index].gameObject;
            }
        }
        return found;
    }

    private static GameObject FindNamedUnder(Transform root, string name)
    {
        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        GameObject found = null;
        for (int index = 0; index < transforms.Length; index++)
        {
            if (transforms[index].name == name)
            {
                if (found != null)
                {
                    return null;
                }
                found = transforms[index].gameObject;
            }
        }
        return found;
    }
}
