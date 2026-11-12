using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PolishCowTowerLevelSmokeTest
{
    private const string Key = "PolishCow.TowerLevelsTest";
    private const string Report = "Logs/TowerLevelsSmokeTest.txt";
    private static IEnumerator routine;
    private static double deadline;
    private static int lastFrame;

    static PolishCowTowerLevelSmokeTest()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Key, false)) return;
            deadline = EditorApplication.timeSinceStartup + 180;
            lastFrame = -1;
            routine = Run();
        };
        EditorApplication.update += () =>
        {
            if (routine == null) return;
            EditorApplication.QueuePlayerLoopUpdate();
            if (lastFrame == Time.frameCount) return;
            lastFrame = Time.frameCount;
            try
            {
                if (EditorApplication.timeSinceStartup > deadline) throw new Exception("Tower level tests timed out.");
                if (routine.MoveNext()) return;
                File.AppendAllText(Report, "PASS: All four towers, three levels, range, damage, animation and UI.\n");
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

    [MenuItem("Tools/PolishCow/Towers/Run Tower Level Smoke Test")]
    public static void Start()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().name != "Main" || SceneManager.GetActiveScene().isDirty)
            throw new InvalidOperationException("Open saved Main in Edit Mode first.");
        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report, "RUNNING: Tower levels in Main\n");
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
        Time.timeScale = 4;
        var context = UnityEngine.Object.FindAnyObjectByType<DefenseSceneContext>();
        context.Timer.enabled = false;
        context.Progress.AddExperience(4000);
        context.Inventory.Add(ItemType.Credits, 10000);
        var slot = context.Slots.First(s => s.SlotKind == DefenseBuildingSlot.Kind.Tower && s.IsUnlocked && s.Occupant == null);
        for (int kind = 0; kind < 4; kind++)
        {
            var definition = AssetDatabase.LoadAssetAtPath<TowerDefinition>("Assets/_Game/Data/Defense/Tower" + kind + ".asset");
            var shop = UnityEngine.Object.FindAnyObjectByType<DefenseShopUI>();
            shop.SelectTower(definition);
            shop.HandleSlotClick(slot);
            yield return null;
            var tower = slot.Occupant.GetComponent<TowerController>();
            var data = new SerializedObject(tower);
            var body = (SpriteRenderer)data.FindProperty("spriteRenderer").objectReferenceValue;
            var weapon = (SpriteRenderer)data.FindProperty("weaponRenderer").objectReferenceValue;
            var animator = weapon.GetComponent<Animator>();
            Sprite previousBase = null;
            Sprite previousWeapon = null;
            float previousRange = 0;
            int previousDamage = 0;
            int shots = 0;
            bool firedAnimation = false;
            tower.OnShot += t => { shots++; firedAnimation |= t.IsFiringAnimation; };
            for (int level = 1; level <= 3; level++)
            {
                if (level > 1)
                {
                    var upgrades = slot.GetComponent<BuildingUpgradeController>();
                    Require(upgrades.TryUpgrade(), definition.kind + " upgrades to " + TowerDefinition.LevelLabel(level));
                    yield return null;
                }
                string label = definition.kind + " " + TowerDefinition.LevelLabel(level);
                Require(tower.CurrentLevel == level && slot.GetComponent<BuildingController>().CurrentLevel == level, label + " level stays synchronized");
                Require(tower.CurrentDamage > previousDamage && tower.CurrentRange > previousRange, label + " damage and range increase");
                Require(body.sprite != null && weapon.sprite != null && body.sprite != previousBase && weapon.sprite != previousWeapon,
                    label + " has distinct base and weapon artwork");
                Sprite idleWeapon = weapon.sprite;
                Require(Mathf.Approximately(weapon.sprite.pixelsPerUnit / weapon.transform.localScale.x, body.sprite.pixelsPerUnit),
                    label + " weapon has consistent pixel scale across levels");
                int damage = tower.CurrentDamage;
                float range = tower.CurrentRange;
                tower.ApplyLevel(level);
                Require(tower.CurrentDamage == damage && tower.CurrentRange == range && body.sprite != null, label + " repeated ApplyLevel is absolute");
                Require(!tower.IsFiringAnimation, label + " is idle without enemies");
                var targetObject = new GameObject("TransientTowerTarget", typeof(CircleCollider2D));
                targetObject.GetComponent<CircleCollider2D>().isTrigger = true;
                var target = targetObject.AddComponent<EnemyTargetDummy>();
                var targetData = new SerializedObject(target);
                targetData.FindProperty("health").intValue = 100000;
                targetData.FindProperty("flying").boolValue = true;
                targetData.ApplyModifiedPropertiesWithoutUndo();
                target.transform.position = tower.transform.position + Vector3.right * (range + .5f);
                target.Initialize(context);
                int beforeShots = shots;
                float wait = Time.time + 1.1f / definition.shotsPerSecond;
                while (Time.time < wait) yield return null;
                Require(shots == beforeShots && !tower.IsFiringAnimation, label + " does not fire or animate outside range");
                float distance = level == 1 ? range - .25f : (previousRange + range) * .5f;
                target.transform.position = tower.transform.position + Vector3.right * distance;
                float hitDeadline = Time.time + distance / 12 + 3 / definition.shotsPerSecond;
                firedAnimation = false;
                int health;
                do
                {
                    yield return null;
                    targetData.Update();
                    health = targetData.FindProperty("health").intValue;
                } while (health == 100000 && Time.time < hitDeadline);
                Require(shots > beforeShots && firedAnimation, label + " plays Fire animation when an enemy enters range");
                Require(100000 - health == damage, label + " projectile deals exact level damage to flying enemy");
                context.Game.SetPaused(true);
                yield return null;
                float pausedTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
                yield return null;
                yield return null;
                Require(Mathf.Approximately(pausedTime, animator.GetCurrentAnimatorStateInfo(0).normalizedTime), label + " animation pauses with game");
                context.Game.SetPaused(false);
                targetObject.SetActive(false);
                UnityEngine.Object.Destroy(targetObject);
                yield return null;
                yield return null;
                float recoilDeadline = Time.time + 1 / definition.shotsPerSecond + .1f;
                while (tower.IsFiringAnimation && Time.time < recoilDeadline) yield return null;
                Require(!tower.IsFiringAnimation && weapon.sprite == idleWeapon, label + " returns to idle after target removal");
                var menu = UnityEngine.Object.FindAnyObjectByType<TowerActionMenuUI>(FindObjectsInactive.Include);
                menu.OpenMenu(tower, slot);
                var menuData = new SerializedObject(menu);
                var title = (TMPro.TMP_Text)menuData.FindProperty("txtTitle").objectReferenceValue;
                var stats = (TMPro.TMP_Text)menuData.FindProperty("txtStats").objectReferenceValue;
                Require(title.text.EndsWith(TowerDefinition.LevelLabel(level)) && stats.text.Contains(damage.ToString()) && stats.text.Contains(range.ToString("0.##")),
                    label + " menu displays actual rank, damage and range");
                var ring = tower.GetComponentInChildren<LineRenderer>();
                Require(ring != null && Mathf.Abs(Vector3.Distance(ring.GetPosition(0), tower.transform.position) - range) < .01f,
                    label + " range circle matches targeting radius");
                menu.CloseMenu();
                previousRange = range;
                previousDamage = damage;
                previousBase = body.sprite;
                previousWeapon = idleWeapon;
            }
            yield return null;
            Require(slot.GetComponent<BuildingController>().TrySell(out _), definition.kind + " sells after level III");
            yield return null;
            yield return null;
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        File.AppendAllText(Report, "PASS: " + message + "\n");
    }
}
