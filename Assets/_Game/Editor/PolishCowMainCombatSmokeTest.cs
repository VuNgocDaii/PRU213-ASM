using System;
using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class PolishCowMainCombatSmokeTest
{
    private const string Key = "PolishCow.MainCombatSmoke";
    private const string Report = "Logs/MainCombatSmokeTest.txt";
    private static IEnumerator routine;
    private static double deadline;
    private static int lastFrame = -1;

    static PolishCowMainCombatSmokeTest()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                deadline = EditorApplication.timeSinceStartup + 180;
                routine = Run();
                lastFrame = -1;
            }
        };
        EditorApplication.update += () =>
        {
            if (routine == null) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.isPlaying && lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Combat test timed out.");
                if (routine.MoveNext()) return;
                File.AppendAllText(Report, "PASS: Main combat integration completed.\n");
                Stop();
            }
            catch (Exception exception)
            {
                File.AppendAllText(Report, "FAIL: " + exception + "\n");
                Debug.LogException(exception);
                Stop();
            }
        };
    }

    [MenuItem("Tools/PolishCow/Run Main Combat Smoke Test")]
    public static void Start()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "Main" || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Open saved Main in Edit Mode first.");
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report, "RUNNING: Main combat integration\n");
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Stop()
    {
        routine = null;
        SessionState.SetBool(Key, false);
        Time.timeScale = 1;
        EditorApplication.ExitPlaymode();
    }

    private static IEnumerator Run()
    {
        Application.runInBackground = true;
        yield return null;
        yield return null;
        var context = UnityEngine.Object.FindAnyObjectByType<DefenseSceneContext>();
        var inventory = context.Inventory;
        var spawner = UnityEngine.Object.FindAnyObjectByType<WaveSpawner>();
        Require(context.IsReady && spawner != null && spawner.Context == context, "Enemy spawner connected to Main");
        Require(context.Timer.IsCounting, "First wave countdown starts automatically");
        Require(UnityEngine.Object.FindObjectsByType<InventoryManager>().Length == 1, "One shared inventory");
        var cowShop = UnityEngine.Object.FindAnyObjectByType<CowShop>();
        Require(AssetDatabase.GetAssetPath(cowShop.GetComponent<SpriteRenderer>().sprite).EndsWith("cowShop.png"), "CowShop uses requested artwork");
        inventory.Add(ItemType.Credits, 10000);
        int cows = UnityEngine.Object.FindObjectsByType<CowController>().Length;
        cowShop.Interact();
        Require(UnityEngine.Object.FindObjectsByType<CowController>().Length == cows + 1, "CowShop purchases into barn");
        var cow = UnityEngine.Object.FindAnyObjectByType<CowInteraction>();
        foreach (var movement in UnityEngine.Object.FindObjectsByType<CowMovement>()) movement.enabled = false;
        var flyDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Defense/Enemy3.asset");
        var fly = spawner.SpawnEnemy(flyDefinition, 1);
        fly.transform.position = cow.transform.position;
        float infectionDeadline = Time.time + 3;
        while (fly.IsAlive && Time.time < infectionDeadline) yield return null;
        Require(!cow.Health.IsHealthy, "Fly reaches cow and causes disease");
        inventory.Add(ItemType.Medicine, 2);
        Require(cow.TryHealCow() && cow.Health.IsHealthy, "Cow can be healed using medicine");
        var buildingFeature = UnityEngine.Object.FindAnyObjectByType<BuildingFeature>();
        var readyBarn = context.Slots.First(s => s.SlotKind == DefenseBuildingSlot.Kind.Cow && s.Occupant != null);
        Require(readyBarn.GetComponent<BuildingController>().Instance == readyBarn.Occupant, "Initial barn is registered for upgrades");
        Require(context.Slots.Count(s => s.SlotKind == DefenseBuildingSlot.Kind.Cow) == 1, "Main keeps exactly one shared barn");
        Require(buildingFeature.TryUpgradeOnSlot(readyBarn), "Shared barn upgrades without constructing another barn");
        yield return null;
        var shop = UnityEngine.Object.FindAnyObjectByType<DefenseShopUI>();
        var entries = new SerializedObject(shop).FindProperty("entries");
        var definition = (TowerDefinition)entries.GetArrayElementAtIndex(0).FindPropertyRelative("definition").objectReferenceValue;
        var slot = context.Slots.First(s => s.SlotKind == DefenseBuildingSlot.Kind.Tower && s.IsUnlocked && s.Occupant == null);
        int credits = inventory.GetAmount(ItemType.Credits);
        shop.SelectTower(definition);
        shop.HandleSlotClick(slot);
        Require(slot.Occupant != null && inventory.GetAmount(ItemType.Credits) == credits - definition.buildPrice, "Tower built with one payment");
        yield return null;
        var tower = slot.Occupant.GetComponent<TowerController>();
        var building = slot.GetComponent<BuildingController>();
        var upgrade = slot.GetComponent<BuildingUpgradeController>();
        Require(building.Instance == tower.gameObject && !upgrade.CanUpgrade(out _), "Tower and building state agree; level one castle blocks upgrades");
        context.Progress.AddExperience(495);
        var actions = UnityEngine.Object.FindAnyObjectByType<TowerActionMenuUI>(FindObjectsInactive.Include);
        actions.OpenMenu(tower, slot);
        Field<Button>(actions, "btnUpgrade").onClick.Invoke();
        Require(tower.CurrentLevel == 2 && building.CurrentLevel == 2, "Tower menu upgrades through building controller");
        yield return null;
        Field<Button>(actions, "btnSell").onClick.Invoke();
        yield return null;
        Require(slot.Occupant == null && !building.IsOccupied, "Selling frees tower slot");
        shop.SelectTower(definition);
        shop.HandleSlotClick(slot);
        yield return null;
        Require(slot.Occupant != null, "Sold slot can build another tower");

        Require(context.Game.BeginNextWave(), "First combat wave begins");
        var normalDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Game/Data/Defense/Enemy0.asset");
        var target = spawner.SpawnEnemy(normalDefinition, 1);
        target.transform.position = slot.transform.position + Vector3.up * .5f;
        if (target is GroundEnemy ground) ground.SetRoute(null);
        context.Game.SetPaused(true);
        Vector3 pausedPosition = target.transform.position;
        yield return null;
        yield return null;
        Require(target.transform.position == pausedPosition && Time.timeScale == 0, "Pause freezes enemies");
        context.Game.SetPaused(false);
        Time.timeScale = 8;
        float killDeadline = Time.time + 10;
        while (target.IsAlive && Time.time < killDeadline) yield return null;
        Require(!target.IsAlive, "Main tower projectiles kill a real enemy");

        Time.timeScale = 32;
        int lastWave = 0;
        // Use transient damage to exercise all ten real spawning/pooling/reward cycles quickly.
        while (context.Game.State != GameState.Won && context.Game.State != GameState.Lost)
        {
            if (context.Game.State == GameState.Preparing && spawner.AliveCount == 0)
                context.Game.BeginNextWave();
            if (lastWave != context.Game.CurrentWave)
            {
                lastWave = context.Game.CurrentWave;
                Require(true, "Wave " + lastWave + " starts");
            }
            foreach (var enemy in context.Enemies.ToArray()) enemy.TakeDamage(100000);
            yield return null;
        }
        Require(context.Game.State == GameState.Won && context.Game.CurrentWave == 10, "Final boss death wins wave ten");
        var battle = UnityEngine.Object.FindAnyObjectByType<BattleUI>();
        Require(Field<GameObject>(battle, "gameOverPanel").activeInHierarchy, "Victory panel visible");
        ScreenCapture.CaptureScreenshot("output/main-victory.png");
        yield return null;
        Field<Button>(battle, "btnRestart").onClick.Invoke();
        yield return null;
        yield return null;
        context = UnityEngine.Object.FindAnyObjectByType<DefenseSceneContext>();
        Require(context != null && context.Game.CurrentWave == 0 && context.Inventory.GetAmount(ItemType.Credits) == 300 && Time.timeScale == 1,
            "Restart reloads Main with fresh inventory and wave state");
        context.CastleHealth.TakeDamage(context.CastleHealth.MaxHealth);
        battle = UnityEngine.Object.FindAnyObjectByType<BattleUI>();
        Require(context.Game.State == GameState.Lost && Field<GameObject>(battle, "gameOverPanel").activeInHierarchy,
            "Castle destruction shows defeat panel");
        ScreenCapture.CaptureScreenshot("output/main-defeat.png");
        yield return null;
        Field<Button>(battle, "btnRestart").onClick.Invoke();
        yield return null;
        yield return null;
        context = UnityEngine.Object.FindAnyObjectByType<DefenseSceneContext>();
        Require(context.Game.State == GameState.Preparing && context.CastleHealth.CurrentHealth == context.CastleHealth.MaxHealth,
            "Restart after defeat restores castle");
        Directory.CreateDirectory("output");
        ScreenCapture.CaptureScreenshot("output/main-complete.png");
        yield return null;
        yield return null;
    }

    private static T Field<T>(UnityEngine.Object target, string field) where T : UnityEngine.Object =>
        new SerializedObject(target).FindProperty(field).objectReferenceValue as T;

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        File.AppendAllText(Report, "PASS: " + message + "\n");
    }
}
