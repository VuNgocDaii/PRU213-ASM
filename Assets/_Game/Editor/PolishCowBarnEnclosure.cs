using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PolishCowBarnEnclosure
{
    [MenuItem("Tools/PolishCow/Rebuild Barn Enclosure")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/_Game/Scenes/Main.unity") throw new InvalidOperationException("Open Main first.");
        var content = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true))
            .First(t => t.name == "CentralFarm").Find("Content");
        foreach (Transform child in content.Cast<Transform>().ToArray())
            if (child.name.StartsWith("BarnBack") || child.name.StartsWith("BarnFront") || child.name == "BarnEnclosure")
                UnityEngine.Object.DestroyImmediate(child.gameObject);
        var root = new GameObject("BarnEnclosure").transform;
        root.SetParent(content, false);
        PolishCowFixedBuildLayout.ApplyToScene(scene);
        // Compact six by five metre enclosure, north of the main road.
        for (int x = -10; x <= -4; x++)
        {
            Piece(root, "Back_" + x, x, 6, x == -10 ? 16 : x == -4 ? 48 : 32, 0);
            if (x != -7)
                Piece(root, "Front_" + x, x, 1, x == -10 ? 16 : x == -4 ? 48 : 32, 32);
        }
        for (int y = 2; y <= 5; y++)
        {
            Piece(root, "Left_" + y, -10, y, 0, 16);
            Piece(root, "Right_" + y, -4, y, 0, 16);
        }
        var area = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<RoamingArea>(true)).First(a => a.transform.parent.name == "CowBarnFeature");
        // The house and fence are the barn's artwork; remove the old prototype marker.
        foreach (var renderer in area.transform.parent.GetComponentsInChildren<SpriteRenderer>(true))
            if (renderer.sprite != null && renderer.sprite.name == "PrototypeMarkerSprite")
            {
                renderer.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            }
        var barnSlot = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<DefenseBuildingSlot>(true))
            .Single(s => s.SlotKind == DefenseBuildingSlot.Kind.Cow);
        var barnPad = barnSlot.GetComponent<SpriteRenderer>();
        if (barnPad != null) { barnPad.enabled = false; PrefabUtility.RecordPrefabInstancePropertyModifications(barnPad); }
        area.transform.position = new Vector3(-7, 2.4f, 0);
        var data = new SerializedObject(area);
        data.FindProperty("size").vector2Value = new Vector2(4.8f, 1.6f);
        data.FindProperty("centerOffset").vector2Value = Vector2.zero;
        data.ApplyModifiedPropertiesWithoutUndo();
        foreach (var cow in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CowMovement>(true)))
        {
            var assigned = new SerializedObject(cow).FindProperty("roamingArea").objectReferenceValue;
            if (assigned != area) continue;
            cow.transform.position = area.ClampPosition(cow.transform.position, new Vector2(0.7f, 0.7f));
            PrefabUtility.RecordPrefabInstancePropertyModifications(cow.transform);
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(area.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(area);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Physics2D.SyncTransforms();
        Debug.Log("Barn enclosure rebuilt: four fenced sides, south entrance, cows confined to inner yard.");
    }

    private static void Piece(Transform root, string name, float x, float y, int tileX, int top)
    {
        string path = "Assets/_Game/Data/Defense/Sprites/BarnFence_" + tileX + "_" + top + ".asset";
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Game/Art/SproutLands/Tilesets/Fences.png");
            sprite = Sprite.Create(texture, new Rect(tileX, texture.height - top - 16, 16, 16), new Vector2(0.5f, 0), 16, 0, SpriteMeshType.FullRect);
            sprite.name = "BarnFence_" + tileX + "_" + top;
            AssetDatabase.CreateAsset(sprite, path);
        }
        var obj = new GameObject(name);
        obj.transform.SetParent(root, false);
        obj.transform.position = new Vector3(x, y, 0);
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = WorldDepthSorting.GetSortingOrder(y);
        obj.AddComponent<WorldDepthSorting>();
        var collider = obj.AddComponent<BoxCollider2D>();
        collider.size = tileX == 0 ? new Vector2(0.22f, 1) : new Vector2(1, 0.22f);
        collider.offset = tileX == 0 ? new Vector2(0, 0.5f) : new Vector2(0, 0.15f);
    }
}
