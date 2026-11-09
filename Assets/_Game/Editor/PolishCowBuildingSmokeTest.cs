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

// Runs actual Play Mode transactions; no shared asset or Main changes.
[InitializeOnLoad]
public static class PolishCowBuildingSmokeTest
{
    private const string Running = "PolishCow.BuildingSmoke";
    private const string Batch = "PolishCow.BuildingSmokeBatch";
    private static IEnumerator routine;
    private static double deadline;
    private static readonly List<string> results = new List<string>();
    private static double readySince;

    static PolishCowBuildingSmokeTest()
    {
        EditorApplication.playModeStateChanged += StateChanged;
        EditorApplication.update += Tick;
    }

    public static void RunBatch()
    {
        PolishCowBuildingSetup.Setup();
        SessionState.SetBool(Batch, true);
        // Creating prefabs can trigger another import/reload after executeMethod returns.
        // Wait until that has settled instead of losing the coroutine during a reload.
        SessionState.SetBool(Running + ".Pending", true);
    }

    [MenuItem("Tools/PolishCow/Building/Run Play Mode Smoke Test")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play first");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save scenes first");
        EditorSceneManager.OpenScene(PolishCowBuildingSetup.ScenePath, OpenSceneMode.Single);
        SessionState.SetBool(Running, true);
        EditorApplication.isPlaying = true;
    }

    private static void StateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            results.Clear();
            deadline = EditorApplication.timeSinceStartup + 180;
            routine = Verify();
        }
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            SessionState.SetBool(Running, false);
            int exitCode = SessionState.GetInt(Running + ".ExitCode", 1);
            var inventories = PolishCowWorkspaceAudit.Collect<InventoryManager>(SceneManager.GetActiveScene());
            if (inventories.Count != 1 || inventories[0].GetAmount(ItemType.Credits) != 1000)
            {
                File.AppendAllText("Logs/BuildingSmokeTest.txt", "\nFAIL Play state leaked into scene\n");
                exitCode = 1;
            }
            else File.AppendAllText("Logs/BuildingSmokeTest.txt", "\nPASS Exiting Play restores scene Credits to 1000\n");
            if (SessionState.GetBool(Batch, false))
            {
                SessionState.SetBool(Batch, false);
                EditorApplication.Exit(exitCode);
            }
        }
    }

    private static void Tick()
    {
        if (SessionState.GetBool(Running + ".Pending", false))
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { readySince = 0; return; }
            if (readySince == 0) readySince = EditorApplication.timeSinceStartup;
            if (EditorApplication.timeSinceStartup - readySince < 3) return;
            SessionState.SetBool(Running + ".Pending", false);
            Run();
            return;
        }
        if (routine == null || EditorApplication.isCompiling) return;
        EditorApplication.QueuePlayerLoopUpdate();
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new TimeoutException("Building smoke timed out");
            if (!routine.MoveNext()) Finish(null);
        }
        catch (Exception error) { Finish(error); }
    }

    private static void Finish(Exception error)
    {
        routine = null;
        SessionState.SetInt(Running + ".ExitCode", error == null ? 0 : 1);
        if (error != null) results.Add("FAIL " + error);
        else results.Add("PASS Building Play Mode smoke suite");
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/BuildingSmokeTest.txt", results);
        Debug.Log(string.Join("\n", results));
        if (error != null) Debug.LogException(error);
        EditorApplication.isPlaying = false;
    }

    private static IEnumerator NextFrame()
    {
        int frame = Time.frameCount;
        while (Time.frameCount == frame) yield return null;
    }

    private static IEnumerator Verify()
    {
        Application.runInBackground = true;
        IEnumerator wait = NextFrame();
        while (wait.MoveNext()) yield return null;
        Scene scene = SceneManager.GetActiveScene();
        var slots = PolishCowWorkspaceAudit.Collect<BuildingSlot>(scene);
        slots.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        Check(slots.Count == 3, "Three fixed slots");
        InventoryManager inventory = PolishCowWorkspaceAudit.Collect<InventoryManager>(scene)[0];
        Check(inventory.GetAmount(ItemType.Credits) == 1000, "BuildingTest starts with 1000 Credits");
        Transform player = PolishCowWorkspaceAudit.Collect<PlayerInteraction>(scene)[0].transform;
        BuildingPanel panel = PolishCowWorkspaceAudit.Collect<BuildingPanel>(scene)[0];
        BuildingSlot first = slots[0];
        player.position = first.transform.position;
        first.Interact();
        first.Interact();
        Check(panel.SelectedSlot == first && inventory.GetAmount(ItemType.Credits) == 1000, "E opens only; repeated E does not purchase");
        panel.Confirm();
        panel.Confirm();
        Check(first.Building.IsOccupied && first.Building.CurrentLevel == 1 && inventory.GetAmount(ItemType.Credits) == 850,
            "Buy once; repeated UI click in same frame cannot upgrade");
        Check(!first.Building.TryBuild() && inventory.GetAmount(ItemType.Credits) == 850, "Cannot overwrite occupied slot");
        Check(first.Building.Instance.transform.parent == null &&
            first.Building.Instance.GetComponent<FeatureReferences>().Inventory == inventory, "Spawn is independent and uses shared inventory");
        Physics2D.SyncTransforms();
        PlayerInteraction interaction = player.GetComponent<PlayerInteraction>();
        typeof(PlayerInteraction).GetMethod("FindNearestTarget", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(interaction, null);
        Check((Interactable)typeof(PlayerInteraction).GetField("currentTarget", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(interaction) == first,
            "Slot sign remains nearest E target with a spawned cow present");
        wait = NextFrame(); while (wait.MoveNext()) yield return null;
        panel.Confirm();
        Check(first.Building.CurrentLevel == 2 && inventory.GetAmount(ItemType.Credits) == 700, "Barn level 2 costs 150");
        wait = NextFrame(); while (wait.MoveNext()) yield return null;
        panel.Confirm();
        Check(first.Building.CurrentLevel == 3 && inventory.GetAmount(ItemType.Credits) == 400, "Barn level 3 costs 300");
        wait = NextFrame(); while (wait.MoveNext()) yield return null;
        Check(!first.Upgrades.TryUpgrade() && inventory.GetAmount(ItemType.Credits) == 400 && first.Upgrades.IsMaxLevel,
            "Max level never charges");
        var panelData = new SerializedObject(panel);
        Check(!((Button)panelData.FindProperty("actionButton").objectReferenceValue).interactable &&
            ((TMP_Text)panelData.FindProperty("detailsText").objectReferenceValue).text.Contains("Cấp tối đa"), "Max-level UI is disabled and labelled");
        player.position = new Vector3(20, 20, 0);
        wait = NextFrame(); while (wait.MoveNext()) yield return null;
        Check(panel.SelectedSlot == null, "Panel closes when player leaves range");

        BuildingController second = slots[1].Building;
        inventory.TryRemove(ItemType.Credits, 400);
        player.position = slots[1].transform.position;
        slots[1].Interact();
        Check(!second.TryBuild() && !second.IsOccupied && inventory.GetAmount(ItemType.Credits) == 0 &&
            !((Button)panelData.FindProperty("actionButton").objectReferenceValue).interactable, "Insufficient build funds: no charge, disabled button");
        inventory.Add(ItemType.Credits, 1000);
        Check(((Button)panelData.FindProperty("actionButton").objectReferenceValue).interactable, "Open panel responds to inventory changes");
        bool reentered = false;
        Action reentrant = () => { if (second.IsBusy) reentered |= second.TryBuild(); };
        inventory.OnChanged += reentrant;
        Check(second.TryBuild(), "Second barn builds");
        inventory.OnChanged -= reentrant;
        Check(!reentered && inventory.GetAmount(ItemType.Credits) == 750 && !second.IsBusy, "Synchronous OnChanged cannot reenter payment");
        inventory.TryRemove(ItemType.Credits, 750);
        wait = NextFrame(); while (wait.MoveNext()) yield return null;
        Check(!slots[1].Upgrades.TryUpgrade() && second.CurrentLevel == 1 && inventory.GetAmount(ItemType.Credits) == 0,
            "Insufficient upgrade funds leave level and credits unchanged");
        inventory.Add(ItemType.Credits, 1000);
        BuildingController machine = slots[2].Building;
        Check(machine.TryBuild() && inventory.GetAmount(ItemType.Credits) == 700, "Machine costs 300");
        wait = NextFrame(); while (wait.MoveNext()) yield return null;
        Check(slots[2].Upgrades.TryUpgrade() && inventory.GetAmount(ItemType.Credits) == 500, "Machine level 2 costs 200");
        wait = NextFrame(); while (wait.MoveNext()) yield return null;
        Check(slots[2].Upgrades.TryUpgrade() && inventory.GetAmount(ItemType.Credits) == 100, "Machine level 3 costs 400");

        // Isolated fixtures use the dummy contract; no scene asset or other feature internals are mutated.
        GameObject dummy = AssetDatabase.LoadAssetAtPath<GameObject>(PolishCowBuildingSetup.Folder + "DummyCowBarn.prefab");
        BuildingController fixture = Fixture(dummy, inventory);
        Check(!fixture.TryBuild() && inventory.GetAmount(ItemType.Credits) == 100, "Fixture insufficient funds");
        inventory.Add(ItemType.Credits, 900);
        GameObject unstable = UnityEngine.Object.Instantiate(dummy);
        unstable.SetActive(false);
        PolishCowBuildingSetup.Set(fixture, "buildingPrefab", unstable);
        Action invalidate = () =>
        {
            if (fixture.IsBusy && unstable.GetComponent<FeatureReferences>() != null)
                UnityEngine.Object.DestroyImmediate(unstable.GetComponent<FeatureReferences>());
        };
        inventory.OnChanged += invalidate;
        Check(!fixture.TryBuild(), "Injected initialization failure after payment is rejected");
        inventory.OnChanged -= invalidate;
        Check(inventory.GetAmount(ItemType.Credits) == 1000 && !fixture.IsOccupied && !fixture.IsBusy,
            "Failed initialization refunds full payment, leaves empty slot and releases lock");
        PolishCowBuildingSetup.Set(fixture, "buildingPrefab", dummy);
        wait = NextFrame(); while (wait.MoveNext()) yield return null;
        Check(fixture.TryBuild() && inventory.GetAmount(ItemType.Credits) == 850, "Slot can retry after failed initialization");
        UpgradeTargetSample sample = fixture.Instance.GetComponent<UpgradeTargetSample>();
        sample.ApplyLevel(2); sample.ApplyLevel(2);
        Check(sample.CurrentLevel == 2, "Dummy ApplyLevel is absolute and repeatable");

        BuildingController prebuilt = Fixture(dummy, inventory);
        GameObject placed = UnityEngine.Object.Instantiate(dummy);
        placed.GetComponent<FeatureReferences>().Initialize(inventory);
        PolishCowBuildingSetup.Set(prebuilt, "existingBuilding", placed);
        PolishCowBuildingSetup.Set(prebuilt, "existingLevel", 2);
        Check(prebuilt.RegisterExistingBuilding() && prebuilt.CurrentLevel == 2 && inventory.GetAmount(ItemType.Credits) == 850,
            "Prebuilt level 2 registers without payment");
        BuildingUpgradeController prebuiltUpgrade = prebuilt.GetComponent<BuildingUpgradeController>();
        Check(prebuiltUpgrade.TryUpgrade() && prebuilt.CurrentLevel == 3 && inventory.GetAmount(ItemType.Credits) == 550,
            "Prebuilt upgrades using the same price table");

        BuildingController invalid = Fixture(dummy, inventory);
        PolishCowBuildingSetup.Set(invalid, "spawnPoint", null);
        Check(!invalid.TryBuild() && inventory.GetAmount(ItemType.Credits) == 550, "Missing spawn point cannot charge");
        PolishCowBuildingSetup.Set(invalid, "spawnPoint", invalid.transform);
        PolishCowBuildingSetup.Set(invalid, "buildPrice", 0);
        Check(!invalid.TryBuild() && inventory.GetAmount(ItemType.Credits) == 550, "Invalid price cannot charge");
        PolishCowBuildingSetup.Set(invalid, "buildPrice", 150);
        GameObject duplicate = UnityEngine.Object.Instantiate(dummy);
        duplicate.AddComponent<UpgradeTarget>();
        PolishCowBuildingSetup.Set(invalid, "buildingPrefab", duplicate);
        Check(!invalid.TryBuild() && inventory.GetAmount(ItemType.Credits) == 550, "Duplicate root upgrade targets cannot charge");
        PolishCowBuildingSetup.Set(invalid, "featureReferences", null);
        Check(!invalid.TryBuild() && inventory.GetAmount(ItemType.Credits) == 550, "Uninitialized controller cannot charge");
        foreach (BuildingSlot slot in slots)
        {
            GameObject building = slot.Building.Instance;
            foreach (Transform node in building.GetComponentsInChildren<Transform>(true))
                Check(node.GetComponents<Interactable>().Length <= 1, "Single Interactable per object: " + node.name);
        }
        // Verify the shared HUD has observed transactions without Building writing its text.
        InventoryHUD hud = PolishCowWorkspaceAudit.Collect<InventoryHUD>(scene)[0];
        TMP_Text creditsText = (TMP_Text)new SerializedObject(hud).FindProperty("creditsText").objectReferenceValue;
        Check(creditsText.text.Contains("550"), "HUD reflects shared inventory transactions");
        panel.Close();
        player.position = first.transform.position;
        first.Interact();
        if (!Application.isBatchMode)
        {
            Directory.CreateDirectory("output/validation");
            ScreenCapture.CaptureScreenshot("output/validation/building-max-level.png");
        }
        wait = NextFrame(); while (wait.MoveNext()) yield return null;
    }

    private static BuildingController Fixture(GameObject prefab, InventoryManager inventory)
    {
        GameObject root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PolishCowBuildingSetup.Folder + "BuildingSlot1.prefab"));
        root.transform.position = new Vector3(40, 40, 0);
        root.GetComponent<FeatureReferences>().Initialize(inventory);
        BuildingController controller = root.GetComponent<BuildingController>();
        PolishCowBuildingSetup.Set(controller, "buildingPrefab", prefab);
        return controller;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        results.Add("PASS " + message);
    }
}
