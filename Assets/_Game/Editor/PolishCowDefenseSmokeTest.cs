using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PolishCowDefenseSmokeTest
{
    private const string Key = "PolishCow.DefenseSmoke";
    private const string Report = "Logs/DefenseFoundationSmokeTest.txt";
    private static IEnumerator routine;
    private static double deadline;
    static PolishCowDefenseSmokeTest()
    {
        EditorApplication.playModeStateChanged += Changed;
        EditorApplication.update += Tick;
    }
    [MenuItem("Tools/PolishCow/Expanded Map/Run Foundation Play Mode Test")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != "Assets/_Game/Scenes/Main.unity" || SceneManager.GetActiveScene().isDirty)
            throw new Exception("Open saved Main in Edit Mode before testing.");
        PolishCowExpandedMapSetup.Verify();
        Directory.CreateDirectory("Logs"); File.WriteAllText(Report,"RUNNING: defense foundation Play Mode test\n");
        SessionState.SetBool(Key,true); EditorApplication.isPlaying = true;
    }
    private static void Changed(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key,false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) { routine = Test(); deadline = EditorApplication.timeSinceStartup + 90; }
        if (state == PlayModeStateChange.ExitingPlayMode && routine != null) Finish(new Exception("Play Mode stopped before test completion."));
    }
    private static void Tick()
    {
        if (routine == null) return;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Foundation test timed out.");
            EditorApplication.QueuePlayerLoopUpdate();
            if (!routine.MoveNext()) Finish(null);
        }
        catch (Exception exception) { Finish(exception); }
    }
    private static IEnumerator Test()
    {
        Application.runInBackground = true;
        yield return null;
        var progress = All<CastleProgress>().Single(); var health = All<CastleHealth>().Single(); var game = All<GameManager>().Single();
        var timer = All<WaveTimer>().Single(); var inventory = All<InventoryManager>().Single(); var zones = All<MapZone>(); var slots = All<DefenseBuildingSlot>();
        Check(All<FeatureReferences>().All(r => r.Inventory == inventory),"All features share the scene inventory");
        Check(zones.Count(z => z.IsUnlocked) == 1,"Level 1 opens one zone");
        Check(slots.Count(s => s.IsUnlocked && s.SlotKind == DefenseBuildingSlot.Kind.Tower) == 4,"Level 1 unlocks four tower slots");
        Check(slots.Count(s => s.Occupant != null) == 2,"Existing barn and machine occupy their slots");
        Check(!timer.IsCounting && game.State == GameState.Preparing,"Combat waits for a spawner to be connected");
        int medicine = inventory.GetAmount(ItemType.Medicine), spoiled = inventory.GetAmount(ItemType.SpoiledMilk);
        inventory.Add(ItemType.Medicine,2); inventory.Add(ItemType.SpoiledMilk,3);
        Check(inventory.TryRemove(ItemType.Medicine,1) && inventory.GetAmount(ItemType.Medicine) == medicine + 1,"Medicine supports inventory transactions");
        Check(inventory.TryRemove(ItemType.SpoiledMilk,3) && inventory.GetAmount(ItemType.SpoiledMilk) == spoiled,"Spoiled milk supports inventory transactions");

        DefenseBuildingSlot slot = slots.First(s => s.IsUnlocked && s.SlotKind == DefenseBuildingSlot.Kind.Tower);
        GameObject template = new GameObject("Temporary test tower"); template.SetActive(false);
        template.AddComponent<FeatureReferences>(); template.AddComponent<UpgradeTargetSample>();
        int credits = inventory.GetAmount(ItemType.Credits);
        Check(!slot.TryBuild(template,10,DefenseBuildingSlot.Kind.Cow),"Building rejects the wrong slot kind");
        Check(!slot.TryBuild(template,int.MaxValue,DefenseBuildingSlot.Kind.Tower),"Building rejects insufficient credits");
        bool reentrantAccepted = false;
        Action reentrant = () => { reentrantAccepted |= slot.TryBuild(template,10,DefenseBuildingSlot.Kind.Tower); };
        inventory.OnChanged += reentrant;
        try { Check(slot.TryBuild(template,10,DefenseBuildingSlot.Kind.Tower),"Building creates and initializes a tower"); }
        finally { inventory.OnChanged -= reentrant; }
        Check(!reentrantAccepted && slot.Occupant.GetComponent<FeatureReferences>().Inventory == inventory,"Payment callbacks cannot build twice");
        Check(!slot.TryBuild(template,10,DefenseBuildingSlot.Kind.Tower) && inventory.GetAmount(ItemType.Credits) == credits - 10,"Occupied slot rejects repeat payment");
        UnityEngine.Object.Destroy(template);

        health.TakeDamage(25);
        progress.AddExperience(495);
        Check(progress.CurrentLevel == 2 && health.CurrentHealth == 175 && health.MaxHealth == 200,"Level 2 increases capacity while preserving damage");
        Check(zones.Count(z => z.IsUnlocked) == 3 && slots.Count(s => s.IsUnlocked && s.SlotKind == DefenseBuildingSlot.Kind.Tower) == 8,"Level 2 opens three zones and eight tower slots");
        progress.AddExperience(2715);
        Check(progress.CurrentLevel == 3 && zones.All(z => z.IsUnlocked),"3210 EXP opens all five zones");
        progress.AddExperience(int.MaxValue);
        Check(progress.CurrentLevel == 3 && progress.Experience == int.MaxValue,"EXP saturates and castle never exceeds level 3");

        var wave = ScriptableObject.CreateInstance<WaveDefinition>(); wave.preparationSeconds = 0.05f;
        SetArray(timer,"waves",new UnityEngine.Object[]{wave});
        int starts = 0, ends = 0; game.OnWaveStarted += _ => starts++; game.OnWaveEnded += _ => ends++;
        Check(timer.StartCountdown() && !timer.StartCountdown(),"Countdown starts once");
        float remaining = timer.RemainingSeconds;
        game.SetPaused(true); yield return null; yield return null;
        Check(game.State == GameState.Paused && timer.RemainingSeconds == remaining && Time.timeScale == 0,"Pause freezes countdown and simulation");
        game.SetPaused(false);
        while (game.CurrentWave == 0) yield return null;
        Check(starts == 1 && game.State == GameState.Fighting,"Countdown emits one wave start");
        Check(!game.CompleteWave(2) && game.CompleteWave(1) && !game.CompleteWave(1) && ends == 1,"Wave completion rejects duplicates and wrong wave");
        UnityEngine.Object.Destroy(wave);

        // Existing farm still feeds and produces; this is separate from the old five-order demo test.
        var cow = All<CowController>().First(); int feed = inventory.GetAmount(ItemType.Feed);
        Check(cow.TryFeed() && !cow.TryFeed() && inventory.GetAmount(ItemType.Feed) == feed - cow.RequiredFeedCost,"Cow feed cycle remains connected after map migration");
        var orders = All<OrderController>().Single();
        var orderData = new SerializedObject(orders); orderData.FindProperty("droneEffect").objectReferenceValue = null; orderData.ApplyModifiedPropertiesWithoutUndo();
        for (int i = 0; i < 6; i++)
        {
            inventory.Add(ItemType.BottledMilk,orders.CurrentOrder.requiredBottledMilk);
            Check(orders.TryDeliverOrder(),"Unlimited Main accepts order " + (i + 1));
        }
        Check(!orders.HasOrderLimit && !orders.IsCompletedAll && orders.CompletedOrdersCount == 6,"Main continues beyond the five-order demo");

        var camera = All<Camera>().Single(c => c.CompareTag("MainCamera"));
        var follow = camera.GetComponent<ExpandedMapCamera>();
        follow.enabled = false;
        camera.orthographicSize = 28;
        camera.transform.position = new Vector3(0,0,-10);
        Directory.CreateDirectory("output");
        ScreenCapture.CaptureScreenshot("output/expanded-map-overview.png");
        yield return null; yield return null;
        follow.enabled = true; camera.orthographicSize = 7.5f;
        int destructions = 0, losses = 0; health.OnCastleDestroyed += () => destructions++; game.OnGameOver += won => { if (!won) losses++; };
        health.TakeDamage(int.MaxValue); health.TakeDamage(1);
        Check(health.CurrentHealth == 0 && destructions == 1 && losses == 1 && game.State == GameState.Lost,"Castle destruction emits one loss");
        Check(!All<PlayerMovement>().Single().enabled && !All<PlayerInteraction>().Single().enabled,"Game over locks player movement and interaction");
        Directory.CreateDirectory("output");
        ScreenCapture.CaptureScreenshot("output/expanded-map-play.png");
        yield return null; yield return null;
    }
    private static T[] All<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>(true)).ToArray();
    private static void SetArray(UnityEngine.Object target,string name,UnityEngine.Object[] values)
    {
        var data = new SerializedObject(target); var array = data.FindProperty(name); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Check(bool value,string message)
    { if (!value) throw new Exception(message); File.AppendAllText(Report,"PASS: " + message + "\n"); }
    private static void Finish(Exception failure)
    {
        routine = null; SessionState.SetBool(Key,false);
        File.AppendAllText(Report,failure == null ? "PASS: Foundation Play Mode test completed; runtime test state was not saved.\n" : "FAIL: " + failure + "\n");
        if (failure != null) Debug.LogException(failure); else Debug.Log("PASS Defense foundation Play Mode test.");
        EditorApplication.isPlaying = false;
    }
}
