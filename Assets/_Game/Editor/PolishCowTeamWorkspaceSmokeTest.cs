using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PolishCowTeamWorkspaceSmokeTest
{
    private const string Running = "PolishCow.TeamSmoke.Running", Index = "PolishCow.TeamSmoke.Index", Original = "PolishCow.TeamSmoke.Original", Pending = "PolishCow.TeamSmoke.Pending";
    private const string Report = "Logs/TeamWorkspaceSmokeTest.txt";
    private static readonly string[] Roles = { "Economy", "Building", "Enemies", "Towers" };
    private static IEnumerator test;
    private static double deadline;
    static PolishCowTeamWorkspaceSmokeTest()
    {
        EditorApplication.playModeStateChanged += OnState;
        EditorApplication.update += Update;
    }
    [MenuItem("Tools/PolishCow/Team Base/Run Workspace Contract Tests")]
    public static void Run()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.isDirty || SessionState.GetBool(Running,false)) throw new Exception("Start team tests from a saved scene in Edit Mode.");
        PolishCowTeamBaseSetup.Verify();
        Directory.CreateDirectory("Logs"); File.WriteAllText(Report,"RUNNING: four workspace contract tests\n");
        SessionState.SetString(Original,scene.path); SessionState.SetInt(Index,0); SessionState.SetBool(Pending,false); SessionState.SetBool(Running,true);
        OpenNext();
    }
    private static void OpenNext()
    {
        if (!SessionState.GetBool(Running,false)) return;
        int index = SessionState.GetInt(Index,0);
        if (index >= Roles.Length)
        {
            File.AppendAllText(Report,"PASS: all four workspaces completed. Test state was not saved.\n");
            SessionState.SetBool(Running,false); RestoreScene(); return;
        }
        EditorSceneManager.OpenScene("Assets/_Game/Scenes/Workspaces/" + Roles[index] + "Workspace.unity",OpenSceneMode.Single);
        EditorApplication.EnterPlaymode();
    }
    private static void OnState(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Running,false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode) { test = CheckWorkspace(); deadline = EditorApplication.timeSinceStartup + 45; }
        else if (state == PlayModeStateChange.ExitingPlayMode && test != null) Fail(new Exception("Play Mode stopped during workspace test."));
        else if (state == PlayModeStateChange.EnteredEditMode) SessionState.SetBool(Pending,true);
    }
    private static void Update()
    {
        if (test == null)
        {
            // delayCall waits for an Inspector repaint, which may never occur in an unfocused editor.
            if (SessionState.GetBool(Running,false) && SessionState.GetBool(Pending,false) && !EditorApplication.isPlayingOrWillChangePlaymode)
            { SessionState.SetBool(Pending,false); OpenNext(); }
            return;
        }
        try
        {
            EditorApplication.QueuePlayerLoopUpdate();
            if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Workspace test timed out.");
            if (!test.MoveNext())
            {
                test = null; SessionState.SetInt(Index,SessionState.GetInt(Index,0)+1); SessionState.SetBool(Pending,true); EditorApplication.ExitPlaymode();
            }
        }
        catch (Exception error) { Fail(error); }
    }
    private static IEnumerator CheckWorkspace()
    {
        Application.runInBackground = true;
        yield return null; yield return null;
        string role = Roles[SessionState.GetInt(Index,0)];
        var context = All<DefenseSceneContext>().Single();
        Check(context.IsReady,role + ": context ready");
        Check(All<DefenseModule>().All(m => m.Context == context),role + ": all module and mock contexts injected");
        Check(All<FeatureReferences>().All(f => f.Inventory == context.Inventory),role + ": every feature uses the same inventory");
        var modules = All<DefenseModule>().ToArray();
        foreach (DefenseModule module in modules) module.Initialize(context);
        int expected = role == "Economy" ? 0 : role == "Towers" ? 2 : 1;
        Check(context.Enemies.Count == expected && context.Cows.Count == (role == "Economy" ? 0 : 1),role + ": idempotent target registration");
        if (expected > 0)
        {
            IEnemyTarget target = context.Enemies[0];
            var targetObject = target.TargetTransform.gameObject;
            targetObject.SetActive(false);
            Check(context.Enemies.Count == expected-1,role + ": disabled enemy unregisters");
            targetObject.SetActive(true);
            Check(context.Enemies.Count == expected,role + ": pooled enemy registers again");
            ((ISlowable)target).ApplySlow(0.7f,2);
            context.Cows[0].Infect();
            Check(!context.Cows[0].IsHealthy,role + ": fly-to-cow contract works without CowHealth dependency");
            target.TakeDamage(int.MaxValue);
            Check(!target.IsAlive && context.Enemies.Count == expected-1,role + ": tower-to-enemy damage contract works");
        }
        var template = new GameObject("Transient contract probe"); template.SetActive(false);
        template.AddComponent<FeatureReferences>(); template.AddComponent<UpgradeTargetSample>(); template.AddComponent<DefenseProbeModule>();
        DefenseBuildingSlot slot = context.Slots.First(s => s.IsUnlocked && s.SlotKind == DefenseBuildingSlot.Kind.Tower && s.Occupant == null);
        int credits = context.Inventory.GetAmount(ItemType.Credits);
        Check(slot.TryBuild(template,10,DefenseBuildingSlot.Kind.Tower),role + ": construction succeeds independently");
        var probe = slot.Occupant.GetComponent<DefenseProbeModule>();
        Check(probe.Context == context && probe.InitializationCount == 1 && slot.Occupant.GetComponent<FeatureReferences>().Inventory == context.Inventory,role + ": runtime construction passes inventory and context");
        Check(!slot.TryBuild(template,10,DefenseBuildingSlot.Kind.Tower) && context.Inventory.GetAmount(ItemType.Credits) == credits-10,role + ": no duplicate construction/payment");
        UnityEngine.Object.Destroy(template);
        Check(context.Game.BeginNextWave() && context.Game.CompleteWave(1),role + ": wave events can be exercised without other modules");
    }
    private static T[] All<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>(true)).ToArray();
    private static void Check(bool value,string message) { if (!value) throw new Exception(message); File.AppendAllText(Report,"PASS: " + message + "\n"); }
    private static void Fail(Exception error)
    {
        test = null; SessionState.SetBool(Running,false); File.AppendAllText(Report,"FAIL: " + error + "\n"); Debug.LogException(error);
        if (EditorApplication.isPlaying) { EditorApplication.playModeStateChanged += RestoreAfterFailure; EditorApplication.ExitPlaymode(); }
        else RestoreScene();
    }
    private static void RestoreAfterFailure(PlayModeStateChange state)
    { if (state != PlayModeStateChange.EnteredEditMode) return; EditorApplication.playModeStateChanged -= RestoreAfterFailure; EditorApplication.delayCall += RestoreScene; }
    private static void RestoreScene()
    { string path = SessionState.GetString(Original,""); if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path,OpenSceneMode.Single); }

    [MenuItem("Tools/PolishCow/Team Base/Resume Workspace Contract Tests")]
    public static void Resume()
    {
        if (SessionState.GetBool(Running,false) && !EditorApplication.isPlayingOrWillChangePlaymode) SessionState.SetBool(Pending,true);
    }
}
