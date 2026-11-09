using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Explicit menus plus a one-shot local request for running the same workflow from tools.
[InitializeOnLoad]
public static class PolishCowMainGameplaySetup
{
    private const string MainPath = "Assets/_Game/Scenes/Main.unity";
    private const string RequestPath = "Temp/PolishCowMainIntegration.request";
    private const string ReportPath = "Logs/MainGameplaySmokeTest.txt";
    private const string RunningKey = "PolishCow.MainSmokeTest";
    private static IEnumerator smokeTest;
    private static double deadline;
    private static readonly List<string> results = new List<string>();

    static PolishCowMainGameplaySetup()
    {
        EditorApplication.update += Poll;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private static void Poll()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        if (File.Exists(RequestPath) && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            string request = File.ReadAllText(RequestPath).Trim();
            File.Delete(RequestPath);
            try
            {
                if (request == "layout-and-smoke")
                {
                    Scene main = SceneManager.GetActiveScene();
                    if (main.path != MainPath) throw new Exception("Open Main before adjusting the order layout.");
                    ApplyOrderLayout(main);
                    EditorSceneManager.MarkSceneDirty(main);
                    if (!EditorSceneManager.SaveScene(main)) throw new Exception("Could not save Main layout.");
                    RunSmokeTest();
                }
                else if (request == "smoke") RunSmokeTest();
                else IntegrateAndRun();
            }
            catch (Exception exception) { Fail(exception); }
        }
        if (smokeTest == null) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Gameplay smoke test timed out.");
            if (!smokeTest.MoveNext()) Finish();
        }
        catch (Exception exception) { Fail(exception); }
    }

    [MenuItem("Tools/PolishCow/Integrate Orders Into Main")]
    public static void Integrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode before integration.");
        Scene main = SceneManager.GetSceneByPath(MainPath);
        if (main.isLoaded && main.isDirty) throw new Exception("Main has unsaved Editor changes. Save it before integration.");
        main = EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
        var inventories = PolishCowWorkspaceAudit.Collect<InventoryManager>(main);
        if (inventories.Count != 1) throw new Exception("Main must have exactly one inventory.");
        InventoryManager inventory = inventories[0];
        GameObject shop = EnsurePrefab(main, "FeedShop", "Assets/_Game/Prefabs/Features/Orders/FeedShop.prefab", new Vector3(4, -1, 0));
        GameObject orders = EnsurePrefab(main, "OrderDelivery", "Assets/_Game/Prefabs/Features/Orders/OrderSample.prefab", new Vector3(7, -4, 0));
        // The shop art's full-height collider leaves its interaction origin out of reach.
        BoxCollider2D shopCollider = shop.GetComponent<BoxCollider2D>();
        shopCollider.size = new Vector2(1.2f, 0.7f);
        shopCollider.offset = new Vector2(0, -0.2f);
        Set(shop.GetComponent<FeedShop>(), "interactionText", "Mua 5 Feed (10 Credits)");
        Set(orders.GetComponent<OrderInteraction>(), "interactionText", "Giao đơn hàng");
        // Replace the old delivery placeholder's blocking collider with the actual station.
        foreach (Transform transform in PolishCowWorkspaceAudit.Collect<Transform>(main))
        {
            if (transform.name != "Delivery") continue;
            BoxCollider2D collider = transform.GetComponent<BoxCollider2D>();
            if (collider != null) collider.enabled = false;
        }
        Canvas canvas = orders.GetComponentInChildren<Canvas>(true);
        canvas.transform.localScale = Vector3.one;
        OrderUI ui = orders.GetComponentInChildren<OrderUI>(true);
        var uiData = new SerializedObject(ui);
        var info = (TextMeshProUGUI)uiData.FindProperty("orderInfoText").objectReferenceValue;
        // Keep order details below the inventory HUD, including at smaller resolutions.
        RectTransform panel = ui.GetComponent<RectTransform>();
        panel.anchoredPosition = new Vector2(-24, -150);
        panel.sizeDelta = new Vector2(400, 300);
        Transform progress = panel.Find("OrderProgress");
        if (progress == null)
        {
            var textObject = new GameObject("OrderProgress", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(panel, false);
            progress = textObject.transform;
        }
        var progressText = progress.GetComponent<TextMeshProUGUI>();
        progressText.font = info.font;
        progressText.fontSize = 22;
        progressText.color = Color.white;
        progressText.raycastTarget = false;
        var rect = progressText.rectTransform;
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 12);
        rect.sizeDelta = new Vector2(-32, 36);
        Set(ui, "orderProgressText", progressText);
        // Remove the obsolete sample listener; OrderUI attaches the actual controller action.
        var button = (Button)new SerializedObject(ui).FindProperty("deliverButton").objectReferenceValue;
        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(button.onClick, 0);
        FeatureReferenceInitializer initializer = inventory.GetComponent<FeatureReferenceInitializer>();
        if (initializer == null) initializer = inventory.gameObject.AddComponent<FeatureReferenceInitializer>();
        Set(initializer, "inventory", inventory);
        var data = new SerializedObject(initializer);
        var features = PolishCowWorkspaceAudit.Collect<FeatureReferences>(main);
        var list = data.FindProperty("features");
        list.arraySize = features.Count;
        for (int i = 0; i < features.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = features[i];
        data.ApplyModifiedPropertiesWithoutUndo();
        ApplyOrderLayout(main);
        EditorSceneManager.MarkSceneDirty(main);
        if (!EditorSceneManager.SaveScene(main)) throw new Exception("Could not save Main.");
        Debug.Log("Main integrated: cow, machine, FeedShop, orders and drone share one inventory.");
    }

    private static void ApplyOrderLayout(Scene scene)
    {
        OrderUI ui = PolishCowWorkspaceAudit.Collect<OrderUI>(scene)[0];
        var data = new SerializedObject(ui);
        var info = (TextMeshProUGUI)data.FindProperty("orderInfoText").objectReferenceValue;
        LayoutText(info, new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(-32, 110), true);
        var notification = (TextMeshProUGUI)data.FindProperty("notificationText").objectReferenceValue;
        LayoutText(notification, new Vector2(0.5f, 1), new Vector2(0, -134), new Vector2(-32, 44), true);
        var button = (Button)data.FindProperty("deliverButton").objectReferenceValue;
        var rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 0);
        rect.anchorMax = new Vector2(1, 0);
        rect.pivot = new Vector2(0.5f, 0);
        rect.anchoredPosition = new Vector2(0, 60);
        rect.sizeDelta = new Vector2(-32, 48);
        var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        label.text = "Giao hàng";
        label.fontSize = 24;
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        var complete = (GameObject)data.FindProperty("demoCompletePanel").objectReferenceValue;
        complete.GetComponent<Image>().color = new Color(0.04f, 0.08f, 0.12f, 0.88f);
        var completeText = complete.GetComponentInChildren<TextMeshProUGUI>(true);
        completeText.text = "CHÚC MỪNG!\nBạn đã hoàn thành 5 đơn hàng.";
        completeText.fontSize = 36;
        completeText.alignment = TextAlignmentOptions.Center;
        completeText.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        completeText.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        completeText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        completeText.rectTransform.anchoredPosition = Vector2.zero;
        completeText.rectTransform.sizeDelta = new Vector2(800, 240);
        PrefabUtility.RecordPrefabInstancePropertyModifications(info);
        PrefabUtility.RecordPrefabInstancePropertyModifications(info.rectTransform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(notification.rectTransform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
        PrefabUtility.RecordPrefabInstancePropertyModifications(label);
        PrefabUtility.RecordPrefabInstancePropertyModifications(label.rectTransform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(complete.GetComponent<Image>());
        PrefabUtility.RecordPrefabInstancePropertyModifications(completeText);
        PrefabUtility.RecordPrefabInstancePropertyModifications(completeText.rectTransform);
    }

    private static void LayoutText(TextMeshProUGUI text, Vector2 anchor, Vector2 position, Vector2 size, bool stretch)
    {
        RectTransform rect = text.rectTransform;
        rect.anchorMin = new Vector2(stretch ? 0 : anchor.x, anchor.y);
        rect.anchorMax = new Vector2(stretch ? 1 : anchor.x, anchor.y);
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static GameObject EnsurePrefab(Scene scene, string name, string path, Vector3 position)
    {
        foreach (GameObject root in scene.GetRootGameObjects()) if (root.name == name) return root;
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (asset == null) throw new Exception("Missing prefab: " + path);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, scene);
        instance.name = name;
        instance.transform.position = position;
        return instance;
    }

    private static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(property).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(UnityEngine.Object target, string property, string value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(property).stringValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/PolishCow/Integrate Main and Run Gameplay Smoke Test")]
    public static void IntegrateAndRun()
    {
        Integrate();
        RunSmokeTest();
    }

    [MenuItem("Tools/PolishCow/Run Main Gameplay Smoke Test")]
    public static void RunSmokeTest()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != MainPath)
            throw new Exception("Open Main in Edit Mode before running the gameplay smoke test.");
        Directory.CreateDirectory("Logs");
        File.WriteAllText(ReportPath, "RUNNING: Main gameplay smoke test\n");
        SessionState.SetBool(RunningKey, true);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(RunningKey, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            results.Clear();
            deadline = EditorApplication.timeSinceStartup + 90;
            smokeTest = RunGameplay();
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            Time.timeScale = 1;
            if (smokeTest != null) Fail(new Exception("Play Mode stopped before test completion."));
        }
    }

    private static IEnumerator RunGameplay()
    {
        Application.runInBackground = true;
        yield return null; // Allow all Start methods and UI subscriptions to complete.
        Scene scene = SceneManager.GetActiveScene();
        Require(scene.path == MainPath, "Main entered Play Mode");
        InventoryManager inventory = PolishCowWorkspaceAudit.Collect<InventoryManager>(scene)[0];
        foreach (FeatureReferences feature in PolishCowWorkspaceAudit.Collect<FeatureReferences>(scene))
            Require(feature.Inventory == inventory, feature.name + " shares the Main inventory");
        var cow = PolishCowWorkspaceAudit.Collect<CowController>(scene)[0];
        var machine = PolishCowWorkspaceAudit.Collect<ProductionMachine>(scene)[0];
        var shop = PolishCowWorkspaceAudit.Collect<FeedShop>(scene)[0];
        var orders = PolishCowWorkspaceAudit.Collect<OrderController>(scene)[0];
        // Exercise the legacy demo contract without changing Main's unlimited-order setting.
        var orderTestData = new SerializedObject(orders);
        orderTestData.FindProperty("limitDemoOrders").boolValue = true;
        orderTestData.ApplyModifiedPropertiesWithoutUndo();
        var ui = PolishCowWorkspaceAudit.Collect<OrderUI>(scene)[0];
        var uiData = new SerializedObject(ui);
        var info = (TextMeshProUGUI)uiData.FindProperty("orderInfoText").objectReferenceValue;
        var button = (Button)uiData.FindProperty("deliverButton").objectReferenceValue;
        Directory.CreateDirectory("output");
        ScreenCapture.CaptureScreenshot("output/main-gameplay-start.png");
        yield return null;
        yield return null;
        Time.timeScale = 8;
        cow.GetComponent<CowMovement>().enabled = false;
        int credits = inventory.GetAmount(ItemType.Credits);
        int feed = inventory.GetAmount(ItemType.Feed);
        InteractThroughPlayer(shop);
        Require(inventory.GetAmount(ItemType.Credits) == credits - 10 && inventory.GetAmount(ItemType.Feed) == feed + 5, "Shop interaction spends 10 Credits and adds 5 Feed");
        Require(!orders.TryDeliverOrder(), "Delivery rejects insufficient bottles");
        int rawBefore = inventory.GetAmount(ItemType.RawMilk);
        for (int i = 0; i < 2; i++)
        {
            InteractThroughPlayer(cow.GetComponent<CowInteraction>());
            int afterFeed = inventory.GetAmount(ItemType.Feed);
            Require(!cow.TryFeed() && inventory.GetAmount(ItemType.Feed) == afterFeed, "Cow rejects overlapping feed cycles");
            while (cow.CurrentState == CowState.Producing) yield return null;
            InteractThroughPlayer(cow.GetComponent<CowInteraction>());
        }
        Require(inventory.GetAmount(ItemType.RawMilk) == rawBefore + 2, "Two cow cycles produce 2 RawMilk");
        int bottles = inventory.GetAmount(ItemType.BottledMilk);
        InteractThroughPlayer(machine);
        Require(inventory.GetAmount(ItemType.RawMilk) == rawBefore, "Machine consumes 2 RawMilk");
        PowerSource power = machine.GetComponent<PowerSource>();
        power.SetPower(false);
        float pausedTime = (float)GetField(machine, "remainingTime");
        float waitUntil = Time.time + 1;
        while (Time.time < waitUntil) yield return null;
        Require(Mathf.Approximately(pausedTime, (float)GetField(machine, "remainingTime")), "Power loss pauses production");
        power.SetPower(true);
        while (!(bool)GetField(machine, "isReady")) yield return null;
        InteractThroughPlayer(machine);
        Require(inventory.GetAmount(ItemType.BottledMilk) == bottles + 1, "Machine collection adds 1 BottledMilk");
        Require(info.text.Contains(">" + (bottles + 1) + "</color>"), "Order UI refreshes after inventory change");
        // Seed only transient Play Mode inventory to exercise every order and completion UI.
        int needed = orders.CurrentOrder.requiredBottledMilk;
        inventory.Add(ItemType.BottledMilk, Mathf.Max(0, needed - inventory.GetAmount(ItemType.BottledMilk)));
        int reward = orders.CurrentOrder.rewardCredits;
        credits = inventory.GetAmount(ItemType.Credits);
        button.onClick.Invoke();
        Require(orders.IsDelivering && !orders.TryDeliverOrder(), "UI starts delivery and duplicate delivery is rejected");
        Require(inventory.GetAmount(ItemType.Credits) == credits + reward, "Delivery credits the exact order reward");
        while (orders.IsDelivering) yield return null;
        Require(orders.CompletedOrdersCount == 1, "Drone completes delivery and advances order");
        for (int i = 1; i < orders.TargetOrderCount; i++)
        {
            inventory.Add(ItemType.BottledMilk, orders.CurrentOrder.requiredBottledMilk);
            InteractThroughPlayer(orders.GetComponent<OrderInteraction>());
            while (orders.IsDelivering) yield return null;
        }
        var complete = (GameObject)uiData.FindProperty("demoCompletePanel").objectReferenceValue;
        Require(orders.IsCompletedAll && complete.activeInHierarchy && !button.interactable, "Five orders show completion panel and disable delivery");
        Require(!orders.TryDeliverOrder(), "Delivery is rejected after demo completion");
        ScreenCapture.CaptureScreenshot("output/main-gameplay-complete.png");
        yield return null;
        yield return null;
    }

    private static object GetField(object target, string name)
    {
        return target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(target);
    }

    private static void InteractThroughPlayer(Interactable expected)
    {
        var player = PolishCowWorkspaceAudit.Collect<PlayerInteraction>(SceneManager.GetActiveScene())[0];
        var body = player.GetComponent<Rigidbody2D>();
        body.position = (Vector2)expected.transform.position + Vector2.down;
        player.transform.position = new Vector3(body.position.x, body.position.y, player.transform.position.z);
        Physics2D.SyncTransforms();
        typeof(PlayerInteraction).GetMethod("FindNearestTarget", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);
        var target = (Interactable)GetField(player, "currentTarget");
        Require(target == expected, "Player proximity selects " + expected.GetType().Name);
        target.Interact();
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        results.Add("PASS: " + message);
        File.AppendAllText(ReportPath, "PASS: " + message + "\n");
    }

    private static void Finish()
    {
        smokeTest = null;
        SessionState.SetBool(RunningKey, false);
        File.AppendAllText(ReportPath, "PASS: Main gameplay smoke test completed. Runtime-only test resources were not saved.\n");
        Debug.Log("PASS Main gameplay smoke test (" + results.Count + " checks). Report: " + ReportPath);
        Time.timeScale = 1;
        EditorApplication.ExitPlaymode();
    }

    private static void Fail(Exception exception)
    {
        smokeTest = null;
        SessionState.SetBool(RunningKey, false);
        Directory.CreateDirectory("Logs");
        File.AppendAllText(ReportPath, "FAIL: " + exception + "\n");
        Debug.LogException(exception);
        if (EditorApplication.isPlaying) { Time.timeScale = 1; EditorApplication.ExitPlaymode(); }
    }
}
