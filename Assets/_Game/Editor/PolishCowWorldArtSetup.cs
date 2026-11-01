using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class PolishCowWorldArtSetup
{
    private const string ArtFolder = "Assets/_Game/Art/Backgrounds/Tilesets/";
    private const string SpriteFolder = "Assets/_Game/Data/WorldSprites/";
    private const string BuildingFolder = "Assets/_Game/Prefabs/World/Buildings/";
    private const string EnvironmentFolder = "Assets/_Game/Prefabs/World/Environment/";

    [MenuItem("Tools/PolishCow/Setup World Art")]
    public static void SetupWorldArt()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            scene.path != "Assets/_Game/Scenes/Main.unity")
        {
            Debug.LogError("Hãy mở Main trong Edit Mode trước khi chạy Setup World Art.");
            return;
        }

        GameObject world = FindRoot(scene, "World");
        Transform map = world != null ? world.transform.Find("PrototypeMap") : null;
        GameObject player = PolishCowPlayerSetup.FindPlayer(scene);
        if (map == null || player == null)
        {
            Debug.LogError("Cần map mẫu và Player hiện có. Menu không tạo lại chúng.");
            return;
        }
        // Chạy lại không ghi đè vị trí, prefab, collider hoặc đường đã chỉnh.
        if (map.Find("WorldArt") != null)
        {
            Debug.Log("WorldArt đã có. Giữ nguyên các chỉnh sửa; không dựng lại hoặc tạo trùng.");
            return;
        }

        Transform ground = map.Find("Grid/Ground");
        Tilemap tilemap = ground != null ? ground.GetComponent<Tilemap>() : null;
        Tile grass = AssetDatabase.LoadAssetAtPath<Tile>(
            "Assets/_Game/Data/Prototype/PrototypeGrass.asset");
        Tile dirt = AssetDatabase.LoadAssetAtPath<Tile>(
            "Assets/_Game/Data/Prototype/PrototypeDirt.asset");
        if (tilemap == null || grass == null || dirt == null)
        {
            Debug.LogError("Thiếu nền hoặc tile cỏ/đất của map mẫu. Không thay đổi scene.");
            return;
        }

        // Chỉ ba sheet cần dùng. Không đổi PPU hoặc lát cắt cũ của sheet.
        if (!PrepareTexture("TilesetHouse.png", 528, 368) ||
            !PrepareTexture("TilesetNature.png", 384, 336) ||
            !PrepareTexture("TilesetElement.png", 256, 240))
        {
            return;
        }
        Directory.CreateDirectory(SpriteFolder);
        Directory.CreateDirectory(BuildingFolder);
        Directory.CreateDirectory(EnvironmentFolder);
        AssetDatabase.Refresh();

        // Rect tính từ góc trên trái của ảnh đã xem, không cắt đều 16x16.
        Sprite barnSprite = GetSprite("FarmHouse", "TilesetHouse.png", 0, 0, 64, 48);
        Sprite warehouseSprite = GetSprite("WarehouseHouse", "TilesetHouse.png", 192, 0, 64, 48);
        Sprite fenceSprite = GetSprite("Fence", "TilesetHouse.png", 160, 80, 16, 16);
        Sprite postSprite = GetSprite("FenceSide", "TilesetHouse.png", 192, 64, 8, 32);
        Sprite treeSprite = GetSprite("Tree", "TilesetNature.png", 48, 288, 48, 48);
        Sprite rockSprite = GetSprite("Rock", "TilesetNature.png", 240, 160, 32, 32);
        Sprite grassSprite = GetSprite("GrassTuft", "TilesetNature.png", 0, 160, 16, 16);
        Sprite crateSprite = GetSprite("Crate", "TilesetElement.png", 0, 0, 16, 16);
        Sprite barrelSprite = GetSprite("Barrel", "TilesetElement.png", 16, 0, 16, 16);
        Sprite benchSprite = GetSprite("Workbench", "TilesetElement.png", 176, 16, 48, 16);
        Sprite cartSprite = GetSprite("Handcart", "TilesetElement.png", 0, 48, 32, 32);
        Sprite signSprite = GetSprite("Sign", "TilesetElement.png", 32, 96, 16, 16);

        // Prefab có sẵn được giữ nguyên, kể cả đã được sửa thủ công.
        GameObject barn = GetPrefab(BuildingFolder, "FarmHouse", barnSprite,
            new Vector2(3.6f, 0.7f), new Vector2(0f, 0.4f));
        GameObject warehouse = GetPrefab(BuildingFolder, "Warehouse", warehouseSprite,
            new Vector2(3.6f, 0.7f), new Vector2(0f, 0.4f));
        GameObject fence = GetPrefab(EnvironmentFolder, "Fence", fenceSprite,
            new Vector2(1f, 0.25f), new Vector2(0f, 0.2f));
        GameObject side = GetPrefab(EnvironmentFolder, "FenceSide", postSprite,
            new Vector2(0.35f, 2f), new Vector2(0f, 1f));
        GameObject tree = GetPrefab(EnvironmentFolder, "Tree", treeSprite,
            new Vector2(0.5f, 0.35f), new Vector2(0f, 0.25f));
        GameObject rock = GetPrefab(EnvironmentFolder, "Rock", rockSprite,
            new Vector2(1.3f, 0.7f), new Vector2(0f, 0.45f));
        GameObject tuft = GetPrefab(EnvironmentFolder, "GrassTuft", grassSprite, Vector2.zero, Vector2.zero);
        GameObject crate = GetPrefab(EnvironmentFolder, "Crate", crateSprite,
            new Vector2(0.8f, 0.6f), new Vector2(0f, 0.35f));
        GameObject barrel = GetPrefab(EnvironmentFolder, "Barrel", barrelSprite,
            new Vector2(0.6f, 0.5f), new Vector2(0f, 0.3f));
        GameObject bench = GetPrefab(BuildingFolder, "ProductionWorkbench", benchSprite,
            new Vector2(2.7f, 0.5f), new Vector2(0f, 0.3f));
        GameObject cart = GetPrefab(EnvironmentFolder, "DeliveryCart", cartSprite,
            new Vector2(1.2f, 0.7f), new Vector2(0f, 0.4f));
        GameObject sign = GetPrefab(EnvironmentFolder, "Sign", signSprite,
            new Vector2(0.5f, 0.25f), new Vector2(0f, 0.15f));
        AssetDatabase.SaveAssets();

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Setup PolishCow World Art");
        GameObject art = new GameObject("WorldArt");
        art.transform.SetParent(map, false);
        Undo.RegisterCreatedObjectUndo(art, "Create World Art");
        Transform root = art.transform;

        ReplaceBoundary(map, root, fence, "Top", new Vector2(24f, 1f), true, 6f);
        ReplaceBoundary(map, root, fence, "Bottom", new Vector2(24f, 1f), true, -7f);
        ReplaceBoundary(map, root, side, "Left", new Vector2(1f, 12f), false, -11.5f);
        ReplaceBoundary(map, root, side, "Right", new Vector2(1f, 12f), false, 11.5f);

        // Chỉ thay hình tạm còn đúng cấu hình gốc. Đối tượng đã sửa được bỏ qua.
        if (ReplacePlaceholder(map, "CowBarn", new Vector2(-7f, 3f), new Vector2(6f, 4f)))
        {
            Place(barn, root, "CowBarnHouse", -7f, 2.2f);
            // Cổng là khoảng trống rộng 2 unit ở hàng rào phía dưới.
            for (int index = 0; index < 6; index++)
            {
                float x = -9.5f + index;
                Place(fence, root, "BarnFenceBack" + index, x, 5.4f);
                if (index < 2 || index > 3)
                {
                    Place(fence, root, "BarnFenceFront" + index, x, 1.2f);
                }
            }
            for (int index = 0; index < 2; index++)
            {
                Place(side, root, "BarnFenceLeft" + index, -10f, 1.3f + index * 2f);
                Place(side, root, "BarnFenceRight" + index, -4f, 1.3f + index * 2f);
            }
            Place(sign, root, "BarnGateSign", -8.6f, 0.8f);
            AddBarnPath(tilemap, grass, dirt);
        }
        if (ReplacePlaceholder(map, "Warehouse", new Vector2(7f, 3f), new Vector2(5f, 4f)))
        {
            Place(warehouse, root, "WarehouseHouse", 7f, 1.4f);
            Place(crate, root, "WarehouseCrate", 9.5f, 1.5f);
            Place(barrel, root, "WarehouseBarrel", 9.5f, 2.6f);
        }
        // Chưa có máy sữa/bãi đáp: giữ hai hình tạm và chỉ đặt đồ đánh dấu.
        Place(bench, root, "ProductionWorkbench", 0f, 1.1f);
        Place(barrel, root, "ProductionBarrel", 1.2f, 2f);
        Place(cart, root, "DeliveryHandcart", 7.3f, -4.8f);
        Place(crate, root, "DeliveryCrate", 9.2f, -4.9f);
        Place(sign, root, "DeliverySign", 5.5f, -4.5f);

        Place(tree, root, "TreeSouthWest", -9f, -5.4f);
        Place(tree, root, "TreeSouth", -5.5f, -5.5f);
        Place(tree, root, "TreeNorthEast", 10f, 3.2f);
        Place(rock, root, "RockSouthWest", -10f, -2.7f);
        Place(rock, root, "RockSouth", 3.8f, -5.5f);
        Place(tuft, root, "GrassWest", -6f, -3.4f);
        Place(tuft, root, "GrassSouth", -3f, -5f);
        Place(tuft, root, "GrassEast", 10.3f, -1.8f);

        // Giữ nguyên movement, sprite và tương tác; chỉ thêm thứ tự vẽ theo chân.
        if (player.GetComponent<WorldDepthSorting>() == null)
        {
            WorldDepthSorting sorting = Undo.AddComponent<WorldDepthSorting>(player);
            SerializedObject sortingData = new SerializedObject(sorting);
            sortingData.FindProperty("feetOffsetY").floatValue = -0.25f;
            sortingData.ApplyModifiedProperties();
            SpriteRenderer renderer = player.GetComponent<SpriteRenderer>();
            Undo.RecordObject(renderer, "Sort Player By Feet");
            renderer.sortingOrder = WorldDepthSorting.GetSortingOrder(player.transform.position.y - 0.25f);
        }
        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = art;
        SceneView.RepaintAll();
        Debug.Log("Đã dựng WorldArt và prefab. Máy sữa/bãi đáp vẫn là hình tạm. Kiểm tra Scene/Game rồi Ctrl+S lưu Main. Chạy lại menu sẽ giữ nguyên WorldArt.");
    }

    private static bool PrepareTexture(string fileName, int width, int height)
    {
        TextureImporter importer = AssetImporter.GetAtPath(ArtFolder + fileName) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Không tìm thấy " + fileName);
            return false;
        }
        int actualWidth;
        int actualHeight;
        importer.GetSourceTextureWidthAndHeight(out actualWidth, out actualHeight);
        if (actualWidth != width || actualHeight != height)
        {
            Debug.LogError("Kích thước " + fileName + " đã thay đổi. Cần xem lại ảnh trước khi cắt.");
            return false;
        }
        // Không đổi ảnh nguồn, spriteMode, PPU hoặc ID sprite cũ.
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
        return true;
    }

    private static Sprite GetSprite(string spriteName, string sheet, int x, int top, int width, int height)
    {
        string path = SpriteFolder + spriteName + ".asset";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
        {
            return sprite;
        }
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + sheet);
        Rect rect = new Rect(x, texture.height - top - height, width, height);
        // Sprite riêng PPU 16, pivot ở chân, dùng trực tiếp texture gốc.
        sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 16f, 0, SpriteMeshType.FullRect);
        sprite.name = spriteName;
        AssetDatabase.CreateAsset(sprite, path);
        return sprite;
    }

    private static GameObject GetPrefab(string folder, string objectName, Sprite sprite,
        Vector2 colliderSize, Vector2 colliderOffset)
    {
        string path = folder + objectName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null)
        {
            return prefab;
        }
        GameObject template = new GameObject(objectName);
        SpriteRenderer renderer = template.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = WorldDepthSorting.GetSortingOrder(0f);
        template.AddComponent<WorldDepthSorting>();
        if (colliderSize != Vector2.zero)
        {
            BoxCollider2D collider = template.AddComponent<BoxCollider2D>();
            collider.size = colliderSize;
            collider.offset = colliderOffset;
        }
        prefab = PrefabUtility.SaveAsPrefabAsset(template, path);
        Object.DestroyImmediate(template);
        return prefab;
    }

    private static void Place(GameObject prefab, Transform parent, string objectName, float x, float y)
    {
        if (parent.Find(objectName) != null)
        {
            return;
        }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = objectName;
        instance.transform.localPosition = new Vector3(x, y, 0f);
        SpriteRenderer renderer = instance.GetComponent<SpriteRenderer>();
        renderer.sortingOrder = WorldDepthSorting.GetSortingOrder(instance.transform.position.y);
        PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
        Undo.RegisterCreatedObjectUndo(instance, "Place World Art");
    }

    private static bool ReplacePlaceholder(Transform map, string areaName, Vector2 position, Vector2 size)
    {
        Transform area = map.Find("Areas/" + areaName);
        SpriteRenderer renderer = area != null ? area.GetComponent<SpriteRenderer>() : null;
        BoxCollider2D collider = area != null ? area.GetComponent<BoxCollider2D>() : null;
        Color originalColor = areaName == "CowBarn" ? new Color(0.8f, 0.57f, 0.28f) :
            new Color(0.45f, 0.7f, 0.65f);
        if (renderer == null || renderer.sprite == null ||
            AssetDatabase.GetAssetPath(renderer.sprite) != "Assets/_Game/Data/Prototype/PrototypeMarker.asset" ||
            renderer.size != size || area.localPosition != new Vector3(position.x, position.y, 0f) ||
            area.localScale != Vector3.one || area.localRotation != Quaternion.identity || !renderer.enabled ||
            renderer.color != originalColor || collider == null || collider.size != size ||
            collider.offset != Vector2.zero || !collider.enabled || collider.isTrigger)
        {
            Debug.LogWarning("Giữ nguyên " + areaName + " vì đã chỉnh hoặc không còn là hình tạm gốc.");
            return false;
        }
        // Không xóa đối tượng, component hoặc tham chiếu cũ.
        Undo.RecordObject(renderer, "Hide Original Placeholder");
        renderer.enabled = false;
        if (collider != null)
        {
            Undo.RecordObject(collider, "Disable Original Placeholder Collider");
            collider.enabled = false;
        }
        Transform label = area.Find("Label");
        if (label != null)
        {
            Undo.RecordObject(label.gameObject, "Hide Original Area Label");
            label.gameObject.SetActive(false);
        }
        return true;
    }

    private static void ReplaceBoundary(Transform map, Transform art, GameObject prefab,
        string objectName, Vector2 size, bool horizontal, float position)
    {
        Transform boundary = map.Find("Obstacles/" + objectName);
        SpriteRenderer renderer = boundary != null ? boundary.GetComponent<SpriteRenderer>() : null;
        Vector3 expectedPosition = horizontal ?
            new Vector3(0f, objectName == "Top" ? 6.5f : -6.5f, 0f) :
            new Vector3(position, 0f, 0f);
        if (renderer == null || renderer.sprite == null || renderer.size != size ||
            AssetDatabase.GetAssetPath(renderer.sprite) != "Assets/_Game/Data/Prototype/PrototypeMarker.asset" ||
            boundary.localPosition != expectedPosition || boundary.localScale != Vector3.one ||
            boundary.localRotation != Quaternion.identity ||
            renderer.color != new Color(0.35f, 0.23f, 0.14f))
        {
            Debug.LogWarning("Giữ nguyên biên " + objectName + " vì khác hình tạm gốc.");
            return;
        }
        Undo.RecordObject(renderer, "Replace Boundary Visual");
        renderer.enabled = false;
        // Giữ collider biên cũ. Hàng rào mới cũng có collider riêng.
        int count = horizontal ? 24 : 6;
        for (int index = 0; index < count; index++)
        {
            if (horizontal)
            {
                Place(prefab, art, "Boundary" + objectName + index, -11.5f + index, position);
            }
            else
            {
                Place(prefab, art, "Boundary" + objectName + index, position, -6f + index * 2f);
            }
        }
    }

    private static void AddBarnPath(Tilemap tilemap, Tile grass, Tile dirt)
    {
        Undo.RegisterCompleteObjectUndo(tilemap, "Connect Barn Gate To Path");
        for (int y = 1; y <= 2; y++)
        {
            for (int x = -8; x <= -7; x++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                // Chỉ đổi ô cỏ gốc, giữ các ô người dùng đã vẽ khác.
                if (tilemap.GetTile(cell) == grass)
                {
                    tilemap.SetTile(cell, dirt);
                }
            }
        }
    }

    private static GameObject FindRoot(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int index = 0; index < roots.Length; index++)
        {
            if (roots[index].name == objectName)
            {
                return roots[index];
            }
        }
        return null;
    }
}
