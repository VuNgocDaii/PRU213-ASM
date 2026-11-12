using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Editor-only autoplay: all kills use real towers and projectile damage.
[InitializeOnLoad]
public static class PolishCowVictoryPlaythrough
{
    private const string Key = "PolishCow.VictoryPlaythrough";
    private const string Report = "Logs/VictoryPlaythrough.txt";
    private const string NaturalKey = Key + ".Natural";
    private static IEnumerator routine;
    private static double deadline;
    private static int lastFrame;
    private static float oldScale;
    private static bool oldBackground;
    private static UnityEngine.Random.State oldRandom;

    static PolishCowVictoryPlaythrough()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                oldScale = Time.timeScale; oldBackground = Application.runInBackground;
                oldRandom = UnityEngine.Random.state;
                deadline = EditorApplication.timeSinceStartup + 600;
                lastFrame = -1; routine = Run();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode && routine != null)
                Finish("FAIL: Play Mode stopped before completion.");
        };
        EditorApplication.update += Tick;
    }

    [MenuItem("Tools/PolishCow/Tests/Run Full Victory Playthrough")]
    public static void Start() => StartWith(false);

    [MenuItem("Tools/PolishCow/Tests/Run Natural Economy Playthrough")]
    public static void StartNatural() => StartWith(true);

    private static void StartWith(bool natural)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path != "Assets/_Game/Scenes/Main.unity" || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Open saved Main in Edit Mode first.");
        Directory.CreateDirectory("Logs"); Directory.CreateDirectory("output");
        File.WriteAllText(Report, "RUNNING: full victory playthrough\n" +
            (natural ? "NATURAL: normal credits and earned XP only.\n" : "INTEGRATION FIXTURE: adds 20000 credits and 3210 castle XP in Play Mode to cover all zones and tower levels.\n") +
            "No direct enemy damage, forced victory or castle healing. Seed 213; simulation x8; preparation countdowns skipped through BeginNextWave.\n");
        SessionState.SetBool(NaturalKey, natural);
        SessionState.SetBool(Key, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Tick()
    {
        if (routine == null) return;
        EditorApplication.QueuePlayerLoopUpdate();
        if (!EditorApplication.isPlaying || lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        try
        {
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Playthrough timed out after 600 real seconds.");
            if (!routine.MoveNext()) Finish("PASS: full victory playthrough completed.");
        }
        catch (Exception e) { Finish("FAIL: " + e); Debug.LogException(e); }
    }

    private static void Finish(string result)
    {
        routine = null; SessionState.SetBool(Key, false);
        File.AppendAllText(Report, result + "\n");
        Time.timeScale = oldScale; Application.runInBackground = oldBackground;
        UnityEngine.Random.state = oldRandom;
        if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
    }

    private static IEnumerator Run()
    {
        Application.runInBackground = true; UnityEngine.Random.InitState(213);
        yield return null; yield return null;
        var context = UnityEngine.Object.FindAnyObjectByType<DefenseSceneContext>();
        var feature = UnityEngine.Object.FindAnyObjectByType<BuildingFeature>();
        Check(context != null && context.IsReady && feature != null, "Main initializes shared combat/building context");
        Check(context.Inventory.GetAmount(ItemType.Credits) == 300 && context.Game.CurrentWave == 0, "Fresh game starts at wave zero with 300 credits");
        Check(context.Slots.Count(s => s.SlotKind == DefenseBuildingSlot.Kind.Cow) == 1, "Exactly one barn");
        Check(context.Slots.Count(s => s.SlotKind == DefenseBuildingSlot.Kind.Tower) == 22, "22 defense pads");
        Check(context.Timer.IsCounting, "Automatic first-wave timer is running");
        bool natural = SessionState.GetBool(NaturalKey, false);
        if (!natural)
        {
            context.Inventory.Add(ItemType.Credits, 20000);
            context.Progress.AddExperience(3210);
            yield return null;
            Check(context.Progress.CurrentLevel == 3 && context.Slots.All(s => s.IsUnlocked), "Fixture opens all zones through castle progression");
        }
        context.Game.SetPaused(true);
        float countdown = context.Timer.RemainingSeconds;
        yield return null; yield return null;
        Check(Time.timeScale == 0 && countdown == context.Timer.RemainingSeconds, "Pause freezes countdown");
        context.Game.SetPaused(false); Time.timeScale = 8;
        int started = 0, completed = 0, kills = 0, shots = 0;
        context.Game.OnWaveStarted += wave => { Check(wave == ++started, "Wave " + wave + " starts in order"); Status(context, "START"); };
        context.Game.OnWaveEnded += wave => { completed++; Status(context, "CLEARED " + wave); };
        context.Game.OnEnemyKilled += (xp, credits) => kills++;
        int lastWave = 0;
        float nextPurchase = 0;
        while (context.Game.State != GameState.Won && context.Game.State != GameState.Lost)
        {
            if (Time.time >= nextPurchase)
            {
                nextPurchase = Time.time + .5f;
                // One transaction per slot per frame; buy using earned credits only.
                foreach (var slot in context.Slots.Where(s => s.IsUnlocked && s.SlotKind == DefenseBuildingSlot.Kind.Tower)
                    .OrderBy(s => Vector2.Distance(s.transform.position, context.CastleTarget.position)))
                {
                    if (slot.Occupant == null)
                    {
                        var entry = feature.Catalog.FindById(natural ? "tower_arrow" : "tower_sniper");
                        if (!entry.CanBuild(context.Progress, context.Inventory, slot, out _)) continue;
                        int before = context.Inventory.GetAmount(ItemType.Credits);
                        Check(feature.TryBuildOnSlot(slot, entry.id), "Build tower at " + slot.name);
                        Check(context.Inventory.GetAmount(ItemType.Credits) == before - entry.buildPrice, "Build charges once");
                        slot.Occupant.GetComponent<TowerController>().OnShot += _ => shots++;
                    }
                    else
                    {
                        var upgrade = slot.GetComponent<BuildingUpgradeController>();
                        if (upgrade != null && upgrade.CanUpgrade(out _))
                        {
                            int before = context.Inventory.GetAmount(ItemType.Credits), price = upgrade.NextPrice;
                            Check(feature.TryUpgradeOnSlot(slot), "Upgrade " + slot.name);
                            Check(context.Inventory.GetAmount(ItemType.Credits) == before - price, "Upgrade charges once");
                        }
                    }
                }
            }
            if (context.Game.State == GameState.Preparing) Check(context.Game.BeginNextWave(), "Start next wave through game API");
            if (lastWave != context.Game.CurrentWave)
            {
                lastWave = context.Game.CurrentWave;
                ScreenCapture.CaptureScreenshot("output/playthrough-wave-" + lastWave.ToString("00") + ".png");
            }
            yield return null;
        }
        Status(context, "END");
        Check(context.Game.State == GameState.Won, "Win with surviving castle");
        Check(started == context.Waves.Count && context.Game.CurrentWave == 10 && completed >= 9, "All ten waves reached; first nine cleared");
        Check(kills > 0 && shots > 0, "Real tower shots and enemy kills: " + shots + " shots, " + kills + " kills");
        Check(context.CastleHealth.CurrentHealth > 0, "Castle still has health");
        var battle = UnityEngine.Object.FindAnyObjectByType<BattleUI>();
        var panel = new SerializedObject(battle).FindProperty("gameOverPanel").objectReferenceValue as GameObject;
        Check(panel != null && panel.activeInHierarchy, "Victory panel visible");
        ScreenCapture.CaptureScreenshot("output/playthrough-victory.png");
        yield return null; yield return null;
        var restart = new SerializedObject(battle).FindProperty("btnRestart").objectReferenceValue as Button;
        Check(restart != null, "Restart button exists"); restart.onClick.Invoke();
        yield return null; yield return null;
        context = UnityEngine.Object.FindAnyObjectByType<DefenseSceneContext>();
        Check(context != null && context.Game.State == GameState.Preparing && context.Game.CurrentWave == 0 && context.Inventory.GetAmount(ItemType.Credits) == 300,
            "Restart returns to a fresh game");
        Check(context.CastleHealth.CurrentHealth == context.CastleHealth.MaxHealth && Time.timeScale == 1, "Restart restores castle and normal speed");
    }

    private static void Status(DefenseSceneContext c, string stage) => File.AppendAllText(Report,
        stage + ": wave=" + c.Game.CurrentWave + " castle=" + c.CastleHealth.CurrentHealth + "/" + c.CastleHealth.MaxHealth +
        " level=" + c.Progress.CurrentLevel + " credits=" + c.Inventory.GetAmount(ItemType.Credits) + " towers=" +
        c.Slots.Count(s => s.SlotKind == DefenseBuildingSlot.Kind.Tower && s.Occupant != null) + "\n");
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        File.AppendAllText(Report, "PASS: " + message + "\n");
    }
}
