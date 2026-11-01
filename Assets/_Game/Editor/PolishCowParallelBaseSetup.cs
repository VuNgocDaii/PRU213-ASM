using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PolishCowParallelBaseSetup
{
    public const string Folder = "Assets/_Game/Prefabs/Features/Building/";

    // Called by the existing setup after its dirty-scene guards.
    public static void PreparePrefabs()
    {
        CreateStub("DummyCowBarn", true);
        CreateStub("DummyMachine", false);
    }

    private static void CreateStub(string name, bool barn)
    {
        string path = Folder + name + ".prefab";
        if (File.Exists(path)) return; // Never replace member work.
        Scene previous = SceneManager.GetActiveScene();
        Scene scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scratch);
            GameObject root = new GameObject(name);
            root.AddComponent<FeatureReferences>();
            root.AddComponent<UpgradeTargetSample>();
            if (barn)
            {
                GameObject area = new GameObject("RoamingArea");
                area.transform.SetParent(root.transform, false);
                area.AddComponent<RoamingArea>();
            }
            else root.AddComponent<PowerSource>();
            string visualPath = barn
                ? "Assets/_Game/Prefabs/World/Buildings/CowBarn.prefab"
                : "Assets/_Game/Prefabs/Features/Production/ProductionSample.prefab";
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(visualPath);
            if (source != null)
            {
                // Copy only the renderer: no duplicate interactions or inventories in a stub.
                SpriteRenderer renderer = source.GetComponentInChildren<SpriteRenderer>(true);
                if (renderer != null)
                {
                    GameObject visual = new GameObject("Visual");
                    visual.transform.SetParent(root.transform, false);
                    SpriteRenderer target = visual.AddComponent<SpriteRenderer>();
                    target.sprite = renderer.sprite;
                    target.color = renderer.color;
                }
            }
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scratch, true);
        }
    }

    public static void ConfigureScene(Scene scene)
    {
        // Add only under the team's workspace, never change Main/map/player.
        Transform work = null;
        foreach (Transform candidate in PolishCowWorkspaceAudit.Collect<Transform>(scene))
        {
            if (candidate.name != "FeatureWork") continue;
            if (work != null) throw new System.InvalidOperationException("Duplicate FeatureWork");
            work = candidate;
        }
        if (work == null) return;
        if (scene.name == "CowTest" && work.GetComponentInChildren<RoamingArea>(true) == null)
        {
            GameObject area = new GameObject("TestRoamingArea");
            area.transform.SetParent(work, false);
            foreach (FeatureReferences feature in work.GetComponentsInChildren<FeatureReferences>(true))
            {
                if (feature.name == "CowSample") { area.transform.position = feature.transform.position; break; }
            }
            area.AddComponent<RoamingArea>();
            Undo.RegisterCreatedObjectUndo(area, "Create test roaming area");
        }
        if (scene.name == "ProductionTest" && work.GetComponentInChildren<PowerSource>(true) == null)
        {
            GameObject power = new GameObject("TestPowerSource");
            power.transform.SetParent(work, false);
            power.AddComponent<PowerSource>();
            Undo.RegisterCreatedObjectUndo(power, "Create test power source");
        }
    }

    [MenuItem("Tools/PolishCow/Prepare Parallel Feature Base")]
    public static void Prepare()
    {
        PolishCowIndependentFeatureSetup.PrepareIndependentTests();
    }

    [MenuItem("Tools/PolishCow/Verify Parallel Feature Base")]
    public static void Verify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Verify Parallel Feature Base requires Edit Mode.");
            return;
        }
        Scene previous = SceneManager.GetActiveScene();
        Scene scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        Random.State randomState = Random.state;
        try
        {
            SceneManager.SetActiveScene(scratch);
            InventoryManager inventory = new GameObject("Inventory").AddComponent<InventoryManager>();
            foreach (string name in new[] { "DummyCowBarn", "DummyMachine" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + name + ".prefab");
                Require(prefab != null, name + " missing; run Prepare Parallel Feature Base.");
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scratch);
                FeatureReferences references = instance.GetComponent<FeatureReferences>();
                UpgradeTarget target = instance.GetComponent<UpgradeTarget>();
                Require(references != null && target != null, name + " missing root contract.");
                references.Initialize(inventory);
                int credits = inventory.GetAmount(ItemType.Credits);
                target.ApplyLevel(2);
                target.ApplyLevel(2);
                UpgradeTargetSample sample = target as UpgradeTargetSample;
                Require(references.Inventory == inventory && sample != null && sample.CurrentLevel == 2 &&
                    inventory.GetAmount(ItemType.Credits) == credits, name + " failed inventory/upgrade contract.");
                if (name == "DummyCowBarn")
                {
                    RoamingArea area = instance.GetComponentInChildren<RoamingArea>();
                    Require(area != null, "Barn missing RoamingArea.");
                    area.transform.position = new Vector3(12f, -8f, 0f);
                    Vector2 padding = new Vector2(0.35f, 0.4f);
                    Vector2[] outsidePoints = {
                        new Vector2(100f, -100f), new Vector2(-100f, 100f),
                        new Vector2(100f, 100f), new Vector2(-100f, -100f)
                    };
                    foreach (Vector2 outside in outsidePoints)
                    {
                        Vector2 clamped = area.ClampPosition(outside, padding);
                        Require(area.Contains(clamped, padding),
                            "Clamp must keep collider inside translated area. Point=" + clamped +
                            "; center=" + area.WorldBounds.center + "; size=" + area.WorldBounds.size);
                        Require(!area.Contains(outside, padding), "Outside point must be rejected.");
                    }
                    for (int index = 0; index < 100; index++)
                        Require(area.Contains(area.GetRandomPoint(padding), padding), "Random point outside area.");
                    Require(!area.Contains(area.WorldBounds.center, new Vector2(100f, 100f)),
                        "Oversized collider must not fit.");
                }
                else
                {
                    PowerSource power = instance.GetComponent<PowerSource>();
                    Require(power != null && power.HasPower, "Machine missing initial power.");
                    int changes = 0;
                    power.OnPowerChanged += value => changes++;
                    power.SetPower(false);
                    power.SetPower(false);
                    Require(!power.HasPower && changes == 1, "Power off must notify once.");
                    power.SetPower(true);
                    Require(power.HasPower && changes == 2, "Power on must notify once.");
                }
            }
            Debug.Log("PASS Parallel Feature Base: root contracts, repeated level, inventory, roaming bounds and power. Edit Mode only.");
        }
        finally
        {
            Random.state = randomState;
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scratch, true);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new System.InvalidOperationException(message);
    }
}
