using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public class PolishCowPrototypeSetup
{
    [MenuItem("Tools/PolishCow/Setup Prototype Map and Player")]
    public static void SetupPrototype()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogError("Hãy thoát Play Mode trước khi dựng map.");
            return;
        }
        if (UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.2d.tilemap/package.json") == null ||
            UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.inputsystem/package.json") == null)
        {
            Debug.LogError("Cần package 2D Tilemap Editor và Input System. Menu không cài package hoặc đổi cấu hình.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        string scenePath = "Assets/_Game/Scenes/Main.unity";
        bool creatingMain = !File.Exists(scenePath);
        if (creatingMain)
        {
            // Chỉ dùng scene builder khi chưa có Main.
            PolishCowSceneBuilder.CreateMainScene();
            if (!File.Exists(scenePath) || SceneManager.GetActiveScene().path != scenePath)
            {
                return;
            }
        }
        else
        {
            Scene loadedMain = SceneManager.GetSceneByPath(scenePath);
            if (loadedMain.isLoaded)
            {
                SceneManager.SetActiveScene(loadedMain);
            }
            else
            {
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject world = null;
        Camera mainCamera = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == "World")
            {
                world = root;
            }
            foreach (Camera sceneCamera in root.GetComponentsInChildren<Camera>(true))
            {
                if (sceneCamera.CompareTag("MainCamera"))
                {
                    mainCamera = sceneCamera;
                }
            }
        }
        if (world == null || mainCamera == null)
        {
            Debug.LogError("Main cần có World và camera mang tag MainCamera.");
            return;
        }

        Sprite grassSprite = null;
        Sprite dirtSprite = null;
        string floorPath = "Assets/_Game/Art/Backgrounds/Tilesets/TilesetFloor.png";
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(floorPath))
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null)
            {
                if (sprite.name == "Floor_R12_C11")
                {
                    grassSprite = sprite;
                }
                else if (sprite.name == "Floor_R08_C12")
                {
                    dirtSprite = sprite;
                }
            }
        }
        if (grassSprite == null || dirtSprite == null)
        {
            Debug.LogError("Chưa có hai tile nền đã cắt. Hãy chạy Prepare Player Cow and Floor Sprites trước.");
            return;
        }
        if (grassSprite.pixelsPerUnit != 16f || dirtSprite.pixelsPerUnit != 16f)
        {
            Debug.LogError("Tile nền cần PPU 16 để mỗi tile chiếm đúng 1 unit.");
            return;
        }

        // Tìm cả Player đang tắt; chỉ tạo mới nếu scene chưa có Player.
        GameObject player = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name != "Player")
                {
                    continue;
                }
                if (player != null)
                {
                    Debug.LogError("Main có nhiều Player. Hãy kiểm tra Hierarchy trước khi chạy lại menu.");
                    return;
                }
                player = child.gameObject;
            }
        }
        if (player == null)
        {
            player = new GameObject("Player");
            player.transform.SetParent(world.transform, false);
            player.AddComponent<SpriteRenderer>();
            Undo.RegisterCreatedObjectUndo(player, "Create Initial Player");
        }
        if (!PolishCowPlayerSetup.ConfigurePlayer(player, true))
        {
            return;
        }

        // Các asset tạm được lưu qua AssetDatabase, không ghi YAML.
        string assetFolder = "Assets/_Game/Data/Prototype";
        Directory.CreateDirectory(assetFolder);
        AssetDatabase.Refresh();
        Tile grassTile = CreateTile(assetFolder + "/PrototypeGrass.asset", grassSprite);
        Tile dirtTile = CreateTile(assetFolder + "/PrototypeDirt.asset", dirtSprite);
        Sprite markerSprite = CreateMarkerSprite(assetFolder + "/PrototypeMarker.asset");
        if (markerSprite == null)
        {
            return;
        }
        AssetDatabase.SaveAssets();

        GameObject map = GetOrCreateChild(world.transform, "PrototypeMap");
        EnableParents(world);
        EnableParents(player);
        Undo.RecordObject(map.transform, "Reset Prototype Map");
        map.transform.localPosition = Vector3.zero;
        map.transform.localRotation = Quaternion.identity;
        map.transform.localScale = Vector3.one;

        CreateGround(map.transform, grassTile, dirtTile);
        CreateAreas(map.transform, markerSprite);
        CreateBoundaries(map.transform, markerSprite);

        // Vị trí xuất hiện nằm trên khoảng trống giữa các khu vực.
        Undo.RecordObject(player.transform, "Move Player To Spawn");
        player.transform.position = map.transform.TransformPoint(new Vector3(-2f, -2f, 0f));
        SpriteRenderer playerRenderer = player.GetComponent<SpriteRenderer>();
        Undo.RecordObject(playerRenderer, "Set Player Sorting");
        playerRenderer.sortingOrder = 10;

        Undo.RecordObject(mainCamera, "Frame Prototype Map");
        Undo.RecordObject(mainCamera.transform, "Center Prototype Camera");
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 8f;
        mainCamera.transform.position = map.transform.TransformPoint(new Vector3(0f, 0f, -10f));
        mainCamera.transform.rotation = Quaternion.identity;
        EnableParents(mainCamera.gameObject);
        mainCamera.enabled = true;

        // Lưu đối tượng thật vào Main, không phụ thuộc Play Mode.
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = map;
        if (!EditorSceneManager.SaveScene(scene))
        {
            Debug.LogError("Không lưu được Main. Map vẫn ở trong scene; hãy kiểm tra Console rồi Ctrl+S.");
            return;
        }
        SceneView.RepaintAll();
        EditorApplication.QueuePlayerLoopUpdate();
        Debug.Log("Đã tạo/cập nhật map và Player trong Edit Mode, rồi lưu Main.unity. Chọn PrototypeMap và nhấn F trong Scene view; chọn Game view 16:9 để xem map.");
    }

    private static void CreateGround(Transform map, Tile grassTile, Tile dirtTile)
    {
        GameObject gridObject = GetOrCreateChild(map, "Grid");
        Grid grid = gridObject.GetComponent<Grid>();
        if (grid == null)
        {
            grid = Undo.AddComponent<Grid>(gridObject);
        }
        Undo.RecordObject(grid, "Configure Prototype Grid");
        grid.cellSize = Vector3.one;
        grid.cellGap = Vector3.zero;

        GameObject ground = GetOrCreateChild(gridObject.transform, "Ground");
        Tilemap tilemap = ground.GetComponent<Tilemap>();
        if (tilemap == null)
        {
            tilemap = Undo.AddComponent<Tilemap>(ground);
        }
        TilemapRenderer renderer = ground.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<TilemapRenderer>(ground);
        }
        Undo.RecordObject(renderer, "Set Ground Sorting");
        renderer.enabled = true;
        renderer.sortingOrder = -100;
        Undo.RegisterCompleteObjectUndo(tilemap, "Paint Prototype Ground");
        tilemap.ClearAllTiles();

        // 24x14 ô, tọa độ x [-12,11], y [-7,6]. Nền không có collider.
        for (int y = -7; y < 7; y++)
        {
            for (int x = -12; x < 12; x++)
            {
                Tile selectedTile = grassTile;
                // Đường chính rộng 2 ô; nhánh trên nằm giữa các công trình.
                if (y == -1 || y == 0 ||
                    (y > 0 && (x == -4 || x == -3 || x == 2 || x == 3)) ||
                    (y < 0 && (x == -1 || x == 0)) ||
                    (y == -3 && x > 0))
                {
                    selectedTile = dirtTile;
                }
                tilemap.SetTile(new Vector3Int(x, y, 0), selectedTile);
            }
        }
    }

    private static void CreateAreas(Transform map, Sprite markerSprite)
    {
        GameObject areas = GetOrCreateChild(map, "Areas");
        GameObject slots = GetOrCreateChild(map, "BuildSlots");
        CreateArea(areas.transform, slots.transform, "CowBarn", "Cow Barn",
            new Vector2(-7f, 3f), new Vector2(6f, 4f), new Color(0.8f, 0.57f, 0.28f), markerSprite);
        CreateArea(areas.transform, slots.transform, "Production", "Production",
            new Vector2(0f, 3f), new Vector2(4f, 3f), new Color(0.35f, 0.6f, 0.8f), markerSprite);
        CreateArea(areas.transform, slots.transform, "Warehouse", "Warehouse",
            new Vector2(7f, 3f), new Vector2(5f, 4f), new Color(0.45f, 0.7f, 0.65f), markerSprite);
        CreateArea(areas.transform, slots.transform, "Delivery", "Delivery",
            new Vector2(8f, -4.5f), new Vector2(4f, 2f), new Color(0.85f, 0.6f, 0.25f), markerSprite);
    }

    private static void CreateArea(Transform areas, Transform slots, string areaName,
        string labelText, Vector2 position, Vector2 size, Color color, Sprite markerSprite)
    {
        // Công trình tạm là vật cản; bãi giao hàng là vùng trống có màu.
        bool solid = areaName != "Delivery";
        GameObject area = CreateBlock(areas, areaName, position, size, color, markerSprite, solid);
        GameObject slot = GetOrCreateChild(slots, areaName + "Slot");
        Undo.RecordObject(slot.transform, "Position Build Slot");
        slot.transform.localPosition = new Vector3(position.x, position.y, 0f);

        GameObject labelObject = GetOrCreateChild(area.transform, "Label");
        TextMeshPro label = labelObject.GetComponent<TextMeshPro>();
        if (label == null)
        {
            label = Undo.AddComponent<TextMeshPro>(labelObject);
        }
        Undo.RecordObject(label, "Label Prototype Area");
        label.enabled = true;
        label.text = labelText;
        label.fontSize = 3f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.sortingOrder = 20;
        Undo.RecordObject(label.rectTransform, "Position Area Label");
        label.rectTransform.sizeDelta = new Vector2(size.x, 1f);
        label.transform.localPosition = new Vector3(0f, 0f, -0.1f);
    }

    private static void CreateBoundaries(Transform map, Sprite markerSprite)
    {
        Transform obstacles = GetOrCreateChild(map, "Obstacles").transform;
        Color fenceColor = new Color(0.35f, 0.23f, 0.14f);
        CreateBlock(obstacles, "Top", new Vector2(0f, 6.5f), new Vector2(24f, 1f), fenceColor, markerSprite, true);
        CreateBlock(obstacles, "Bottom", new Vector2(0f, -6.5f), new Vector2(24f, 1f), fenceColor, markerSprite, true);
        CreateBlock(obstacles, "Left", new Vector2(-11.5f, 0f), new Vector2(1f, 12f), fenceColor, markerSprite, true);
        CreateBlock(obstacles, "Right", new Vector2(11.5f, 0f), new Vector2(1f, 12f), fenceColor, markerSprite, true);
    }

    private static GameObject CreateBlock(Transform parent, string objectName, Vector2 position,
        Vector2 size, Color color, Sprite markerSprite, bool solid)
    {
        GameObject block = GetOrCreateChild(parent, objectName);
        Undo.RecordObject(block.transform, "Position Prototype Block");
        block.transform.localPosition = new Vector3(position.x, position.y, 0f);
        SpriteRenderer renderer = block.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = Undo.AddComponent<SpriteRenderer>(block);
        }
        Undo.RecordObject(renderer, "Configure Prototype Block");
        renderer.enabled = true;
        renderer.sprite = markerSprite;
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = size;
        renderer.color = color;
        renderer.sortingOrder = 0;

        BoxCollider2D collider = block.GetComponent<BoxCollider2D>();
        if (solid)
        {
            if (collider == null)
            {
                collider = Undo.AddComponent<BoxCollider2D>(block);
            }
            Undo.RecordObject(collider, "Configure Prototype Obstacle");
            collider.size = size;
            collider.offset = Vector2.zero;
            collider.isTrigger = false;
            collider.enabled = true;
        }
        else if (collider != null)
        {
            // Bãi giao hàng cần đi vào được.
            Undo.DestroyObjectImmediate(collider);
        }
        return block;
    }

    private static GameObject GetOrCreateChild(Transform parent, string objectName)
    {
        // Tìm con trực tiếp để chạy lại menu không tạo đối tượng trùng.
        Transform child = parent.Find(objectName);
        if (child != null)
        {
            Undo.RecordObject(child.gameObject, "Enable Prototype Object");
            child.gameObject.SetActive(true);
            return child.gameObject;
        }
        GameObject childObject = new GameObject(objectName);
        childObject.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(childObject, "Create Prototype Object");
        return childObject;
    }

    private static void EnableParents(GameObject target)
    {
        // Một đối tượng đang bật vẫn bị ẩn nếu cha của nó đang tắt.
        for (Transform current = target.transform; current != null; current = current.parent)
        {
            if (!current.gameObject.activeSelf)
            {
                Undo.RecordObject(current.gameObject, "Enable Prototype Parent");
                current.gameObject.SetActive(true);
            }
        }
    }

    private static Tile CreateTile(string assetPath, Sprite sprite)
    {
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, assetPath);
        }
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static Sprite CreateMarkerSprite(string assetPath)
    {
        if (File.Exists(assetPath))
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                Sprite existingSprite = asset as Sprite;
                if (existingSprite != null)
                {
                    return existingSprite;
                }
            }
            Debug.LogError("Asset hình tạm đã có nhưng không chứa Sprite: " + assetPath);
            return null;
        }
        // Hình vuông trắng để tô màu công trình tạm, không cần thêm ảnh ngoài.
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "PrototypeMarker";
        texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        AssetDatabase.CreateAsset(texture, assetPath);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1),
            new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
        sprite.name = "PrototypeMarkerSprite";
        AssetDatabase.AddObjectToAsset(sprite, texture);
        return sprite;
    }
}
