using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PolishCowBuildingSetup
{
    public const string ScenePath = "Assets/_Game/Scenes/Tests/BuildingTest.unity";
    public const string Folder = "Assets/_Game/Prefabs/Features/Building/";

    [MenuItem("Tools/PolishCow/Building/Set Up BuildingTest")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes first");
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform work = Named(scene, "FeatureWork");
        if (work.Find("BuildingFeature") != null)
        {
            Debug.Log("BuildingTest already configured; preserving Inspector changes.");
            return;
        }
        InventoryManager inventory = Single<InventoryManager>(scene);
        Transform player = Single<PlayerInteraction>(scene).transform;
        TMP_FontAsset font = Single<InventoryHUD>(scene).GetComponentInChildren<TMP_Text>(true)?.font;
        if (font == null)
            foreach (TMP_Text text in PolishCowWorkspaceAudit.Collect<TMP_Text>(scene))
                if (text.font != null) { font = text.font; break; }
        if (font == null) throw new InvalidOperationException("Missing HUD font");
        // Disable only the obsolete Building demo; leave the shared prefab untouched.
        foreach (BuildingConnectionSample sample in work.GetComponentsInChildren<BuildingConnectionSample>(true))
            sample.gameObject.SetActive(false);
        GameObject feature = Child(work, "BuildingFeature");
        BuildingPanel panel = CreatePanel(feature.transform, font);
        GameObject barn = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Features/Cows/CowBarn.prefab");
        GameObject machine = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Features/Production/ProductionMachine.prefab");
        if (barn == null || machine == null) throw new InvalidOperationException("Missing feature prefabs");
        string[] names = { "Chuồng bò 1", "Chuồng bò 2", "Máy chế biến" };
        int[] prices = { 150, 250, 300 };
        for (int i = 0; i < 3; i++)
        {
            GameObject root = Child(feature.transform, "BuildingSlot" + (i + 1));
            root.transform.position = new Vector3(-6 + i * 6, -4, 0);
            FeatureReferences references = root.AddComponent<FeatureReferences>();
            BuildingController building = root.AddComponent<BuildingController>();
            BuildingUpgradeController upgrades = root.AddComponent<BuildingUpgradeController>();
            BuildingSlot slot = root.AddComponent<BuildingSlot>();
            root.AddComponent<CircleCollider2D>().isTrigger = true;
            GameObject spawn = Child(root.transform, "SpawnPoint");
            spawn.transform.localPosition = new Vector3(0, 3, 0);
            Set(building, "featureReferences", references);
            Set(building, "buildingName", names[i]);
            Set(building, "buildingPrefab", i == 2 ? machine : barn);
            Set(building, "spawnPoint", spawn.transform);
            Set(building, "buildPrice", prices[i]);
            Set(upgrades, "building", building);
            SetPrices(upgrades, i == 2 ? new[] { 200, 400 } : new[] { 150, 300 });
            Set(slot, "building", building);
            Set(slot, "upgrades", upgrades);
            Set(slot, "interactionText", "Xây dựng / nâng cấp " + names[i]);
            GameObject sign = Child(root.transform, "Sign");
            TextMeshPro label = sign.AddComponent<TextMeshPro>();
            label.font = font;
            label.text = names[i] + "\n[E]";
            label.fontSize = 3;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1, 0.88f, 0.35f);
            label.rectTransform.sizeDelta = new Vector2(3.2f, 1.2f);
            sign.transform.localPosition = new Vector3(0, 0, -1);
            // Save the self-contained asset BEFORE assigning scene-only references.
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "BuildingSlot" + (i + 1) + ".prefab");
            Set(slot, "panel", panel);
            Set(slot, "player", player);
        }
        Set(inventory, "credits", 1000);
        FeatureReferenceInitializer initializer = inventory.GetComponent<FeatureReferenceInitializer>();
        if (initializer == null) initializer = inventory.gameObject.AddComponent<FeatureReferenceInitializer>();
        Set(initializer, "inventory", inventory);
        var data = new SerializedObject(initializer);
        var features = PolishCowWorkspaceAudit.Collect<FeatureReferences>(scene);
        SerializedProperty list = data.FindProperty("features");
        list.arraySize = features.Count;
        for (int i = 0; i < features.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = features[i];
        data.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Could not save BuildingTest");
        AssetDatabase.SaveAssets();
        Debug.Log("BuildingTest saved: three slots, real prefabs, prices, shared inventory, panel and player assigned.");
    }

    private static BuildingPanel CreatePanel(Transform parent, TMP_FontAsset font)
    {
        GameObject root = new GameObject("BuildingCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.transform.SetParent(parent, false);
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        root.GetComponent<Canvas>().sortingOrder = 20;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        BuildingPanel panel = root.AddComponent<BuildingPanel>();
        GameObject popup = new GameObject("PopupLayer", typeof(RectTransform), typeof(Image));
        popup.transform.SetParent(root.transform, false);
        RectTransform rect = popup.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(480, 370);
        popup.GetComponent<Image>().color = new Color(0.04f, 0.1f, 0.09f, 0.97f);
        TMP_Text title = Text(popup.transform, font, "Title", 28, new Vector2(0, 133), new Vector2(430, 45));
        TMP_Text details = Text(popup.transform, font, "Details", 24, new Vector2(0, 57), new Vector2(430, 90));
        TMP_Text status = Text(popup.transform, font, "Status", 20, new Vector2(0, -35), new Vector2(430, 80));
        Button action = Button(popup.transform, font, "Confirm", "Xây", new Vector2(-78, -130), new Vector2(230, 52));
        Button close = Button(popup.transform, font, "Close", "Đóng", new Vector2(150, -130), new Vector2(120, 52));
        Set(panel, "panelRoot", popup);
        Set(panel, "titleText", title);
        Set(panel, "detailsText", details);
        Set(panel, "statusText", status);
        Set(panel, "actionText", action.GetComponentInChildren<TMP_Text>());
        Set(panel, "actionButton", action);
        Set(panel, "closeButton", close);
        PrefabUtility.SaveAsPrefabAsset(root, Folder + "BuildingCanvas.prefab");
        popup.SetActive(false);
        return panel;
    }

    private static TMP_Text Text(Transform parent, TMP_FontAsset font, string name, int size, Vector2 position, Vector2 dimensions)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.anchoredPosition = position;
        text.rectTransform.sizeDelta = dimensions;
        return text;
    }

    private static Button Button(Transform parent, TMP_FontAsset font, string name, string caption, Vector2 position, Vector2 dimensions)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        go.GetComponent<Image>().color = new Color(0.2f, 0.48f, 0.35f);
        Button button = go.GetComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        Text(go.transform, font, "Label", 23, Vector2.zero, dimensions).text = caption;
        return button;
    }

    private static GameObject Child(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static Transform Named(Scene scene, string name)
    {
        foreach (Transform item in PolishCowWorkspaceAudit.Collect<Transform>(scene))
            if (item.name == name) return item;
        throw new InvalidOperationException("Missing " + name);
    }

    private static T Single<T>(Scene scene) where T : Component
    {
        var items = PolishCowWorkspaceAudit.Collect<T>(scene);
        if (items.Count != 1) throw new InvalidOperationException("Expected one " + typeof(T).Name);
        return items[0];
    }

    public static void Set(UnityEngine.Object target, string field, object value)
    {
        var data = new SerializedObject(target);
        SerializedProperty property = data.FindProperty(field);
        if (property == null) throw new InvalidOperationException(target.name + ": missing " + field);
        if (value is int number) property.intValue = number;
        else if (value is string text) property.stringValue = text;
        else property.objectReferenceValue = value as UnityEngine.Object;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetPrices(BuildingUpgradeController target, int[] prices)
    {
        var data = new SerializedObject(target);
        var list = data.FindProperty("upgradePrices");
        list.arraySize = prices.Length;
        for (int i = 0; i < prices.Length; i++) list.GetArrayElementAtIndex(i).intValue = prices[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
