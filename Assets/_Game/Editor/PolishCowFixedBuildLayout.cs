using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PolishCowFixedBuildLayout
{
    // Names remain stable so scene references and repeat integrations keep working.
    private static readonly Dictionary<string, Vector2> Positions = new Dictionary<string, Vector2>
    {
        { "CowSlot_-7_3", new Vector2(-7, 2.4f) },
        { "MachineSlot_0_3", new Vector2(11, 4) },
        // Level one: outer approaches, both bends and overlapping coverage near the keep.
        { "TowerSlot_-12_-4", new Vector2(-7, -2.5f) },
        { "TowerSlot_-6_-8", new Vector2(-4.5f, 7.5f) },
        { "TowerSlot_6_-8", new Vector2(5.5f, 6) },
        { "TowerSlot_12_-4", new Vector2(9, -3) },
        { "CowSlot_-10_-5", new Vector2(-11, -3.5f) },
        { "CowSlot_-10_-9", new Vector2(3, -4) },
        // Level two: pads flank the east/west roads instead of distant pasture corners.
        { "TowerSlot_-20_4", new Vector2(-18, 2.5f) },
        { "TowerSlot_-26_-4", new Vector2(-27, -3) },
        { "CowSlot_-23_5", new Vector2(-24, 2.5f) },
        { "CowSlot_-23_-7", new Vector2(-21, -3) },
        { "TowerSlot_20_4", new Vector2(18, 2.5f) },
        { "TowerSlot_26_-4", new Vector2(27, -3) },
        { "MachineSlot_24_5", new Vector2(23, 2.5f) },
        // Level three: every pad is within a level-one arrow tower's range of its lane.
        { "TowerSlot_-12_18", new Vector2(-4, 13) },
        { "TowerSlot_12_18", new Vector2(4, 18) },
        { "CowSlot_-20_18", new Vector2(-4, 21) },
        { "MachineSlot_20_18", new Vector2(4, 13) },
        { "TowerSlot_-12_-18", new Vector2(-4, -15) },
        { "TowerSlot_12_-18", new Vector2(4, -19) },
        { "CowSlot_-24_-18", new Vector2(-4, -22) },
        { "CowSlot_-18_-18", new Vector2(4, -13.5f) },
        { "MachineSlot_20_-18", new Vector2(4, -23) }
    };

    [MenuItem("Tools/PolishCow/Fix Fixed Build Layout")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode before changing the build layout.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Game/Scenes/Main.unity")
            throw new InvalidOperationException("Open Main first.");
        ApplyToScene(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void ApplyToScene(Scene scene)
    {
        foreach (var slot in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DefenseBuildingSlot>(true)))
        {
            Undo.RecordObject(slot.transform, "Space fixed build slots");
            var data = new SerializedObject(slot);
            var occupant = data.FindProperty("initialOccupant").objectReferenceValue as GameObject;
            // Main has one shared barn and one existing processor; all vacant pads are defenses.
            if (occupant == null)
            {
                data.FindProperty("kind").enumValueIndex = (int)DefenseBuildingSlot.Kind.Tower;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.RecordPrefabInstancePropertyModifications(slot);
            }
            var previous = slot.transform.position;
            if (Positions.TryGetValue(slot.name, out var position)) slot.transform.position = position;
            // A large pad must not multiply the scale of its future child building.
            slot.transform.localScale = Vector3.one;
            var pad = slot.GetComponent<SpriteRenderer>();
            if (pad != null)
            {
                Undo.RecordObject(pad, "Resize build pad");
                pad.drawMode = SpriteDrawMode.Sliced;
                pad.size = Vector2.one * 2.4f;
            }
            if (occupant != null && occupant.transform.parent != slot.transform)
            {
                Undo.RecordObject(occupant.transform, "Align existing building with slot");
                occupant.transform.position += slot.transform.position - previous;
                PrefabUtility.RecordPrefabInstancePropertyModifications(occupant.transform);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(slot.transform);
        }
        EditorSceneManager.MarkSceneDirty(scene);
    }
}
