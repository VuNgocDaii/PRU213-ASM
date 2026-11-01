using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PolishCowPlayerSetup
{
    [MenuItem("Tools/PolishCow/Setup Caveman Player")]
    public static void SetupCavemanPlayer()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != "Assets/_Game/Scenes/Main.unity")
        {
            Debug.LogError("Hãy mở Main và thoát Play Mode trước khi thiết lập Caveman.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        GameObject player = FindPlayer(scene);
        if (player == null || !ConfigurePlayer(player))
        {
            return;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = player;
        Debug.Log("Đã thiết lập Caveman. Hãy kiểm tra và tự lưu Main bằng Ctrl+S.");
    }

    public static GameObject FindPlayer(Scene scene)
    {
        // Chỉ tìm trong scene được yêu cầu, kể cả đối tượng đang tắt.
        GameObject player = null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform objectTransform in root.GetComponentsInChildren<Transform>(true))
            {
                if (objectTransform.name == "Player")
                {
                    if (player != null)
                    {
                        Debug.LogError("Scene có nhiều đối tượng tên Player. Dừng để tránh sửa nhầm.");
                        return null;
                    }
                    player = objectTransform.gameObject;
                }
            }
        }
        if (player == null)
        {
            Debug.LogError("Không tìm thấy Player trong scene.");
        }
        return player;
    }

    public static bool ConfigurePlayer(GameObject player, bool keepExistingSprites = false)
    {
        SpriteRenderer spriteRenderer = player.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("Player cần có SpriteRenderer trên cùng GameObject.");
            return false;
        }
        Collider2D[] existingColliders = player.GetComponents<Collider2D>();
        if (existingColliders.Length > 1 ||
            (existingColliders.Length == 1 && !(existingColliders[0] is BoxCollider2D)))
        {
            Debug.LogError("Player cần tối đa một BoxCollider2D. Hãy kiểm tra collider hiện có.");
            return false;
        }

        PlayerMovement movement = player.GetComponent<PlayerMovement>();
        // Menu map giữ sprite đã gán. Chỉ chuẩn bị Caveman khi còn thiếu hướng.
        bool needsSprites = !keepExistingSprites || movement == null;
        if (!needsSprites)
        {
            SerializedObject existingMovement = new SerializedObject(movement);
            string[] directions = { "downSprite", "upSprite", "leftSprite", "rightSprite" };
            foreach (string direction in directions)
            {
                if (existingMovement.FindProperty(direction).objectReferenceValue == null)
                {
                    needsSprites = true;
                }
            }
        }
        Sprite[] sprites = needsSprites ? PrepareCavemanSprites() : null;
        if (needsSprites && sprites == null)
        {
            return false;
        }

        Undo.RecordObject(spriteRenderer, "Use Caveman Sprite");
        spriteRenderer.enabled = true;
        if (!keepExistingSprites || spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = sprites != null ? sprites[0] :
                new SerializedObject(movement).FindProperty("downSprite").objectReferenceValue as Sprite;
            spriteRenderer.flipX = false;
            spriteRenderer.flipY = false;
        }
        Undo.RecordObject(player.transform, "Set Player Scale");
        player.transform.localScale = Vector3.one;

        Rigidbody2D playerRigidbody = player.GetComponent<Rigidbody2D>();
        if (playerRigidbody == null)
        {
            playerRigidbody = Undo.AddComponent<Rigidbody2D>(player);
        }
        Undo.RecordObject(playerRigidbody, "Configure Player Rigidbody2D");
        playerRigidbody.bodyType = RigidbodyType2D.Dynamic;
        playerRigidbody.gravityScale = 0f;
        playerRigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        playerRigidbody.linearDamping = 0f;
        playerRigidbody.linearVelocity = Vector2.zero;
        playerRigidbody.angularVelocity = 0f;
        playerRigidbody.simulated = true;
        playerRigidbody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        BoxCollider2D feetCollider = player.GetComponent<BoxCollider2D>();
        if (feetCollider == null)
        {
            feetCollider = Undo.AddComponent<BoxCollider2D>(player);
        }
        // Collider nhỏ ở chân, không bao trùm cả đầu nhân vật.
        Undo.RecordObject(feetCollider, "Configure Player Feet Collider");
        feetCollider.size = new Vector2(0.5f, 0.25f);
        feetCollider.offset = new Vector2(0f, -0.25f);
        feetCollider.isTrigger = false;
        feetCollider.enabled = true;

        if (movement == null)
        {
            movement = Undo.AddComponent<PlayerMovement>(player);
        }
        Undo.RecordObject(movement, "Configure Player Movement");
        movement.enabled = true;
        SerializedObject movementData = new SerializedObject(movement);
        if (!keepExistingSprites || movementData.FindProperty("playerRigidbody").objectReferenceValue == null)
        {
            movementData.FindProperty("playerRigidbody").objectReferenceValue = playerRigidbody;
        }
        if (!keepExistingSprites || movementData.FindProperty("playerSpriteRenderer").objectReferenceValue == null)
        {
            movementData.FindProperty("playerSpriteRenderer").objectReferenceValue = spriteRenderer;
        }
        // Đã xem trực quan: 0 xuống, 1 lên, 2 trái, 3 phải.
        if (sprites != null)
        {
            string[] directions = { "downSprite", "upSprite", "leftSprite", "rightSprite" };
            for (int index = 0; index < directions.Length; index++)
            {
                SerializedProperty direction = movementData.FindProperty(directions[index]);
                if (!keepExistingSprites || direction.objectReferenceValue == null)
                {
                    direction.objectReferenceValue = sprites[index];
                }
            }
        }
        movementData.ApplyModifiedProperties();
        return true;
    }

    private static Sprite[] PrepareCavemanSprites()
    {
        string imagePath = "Assets/_Game/Art/Actor/Character/Caveman/SeparateAnim/Idle.png";
        TextureImporter importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("Không tìm thấy ảnh Caveman: " + imagePath);
            return null;
        }

        int width;
        int height;
        importer.GetSourceTextureWidthAndHeight(out width, out height);
        if (width != 64 || height != 16)
        {
            Debug.LogError("Ảnh Caveman đã thay đổi kích thước. Cần kiểm tra lại trước khi cắt.");
            return null;
        }

        SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null)
        {
            Debug.LogError("Không có Sprite Editor Data Provider. Kiểm tra package 2D Sprite.");
            return null;
        }
        provider.InitSpriteEditorDataProvider();
        ISpriteNameFileIdDataProvider nameProvider = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (nameProvider == null)
        {
            Debug.LogError("Không có Sprite Name/File ID Data Provider cho Caveman.");
            return null;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.mipmapEnabled = false;
        importer.spritePixelsPerUnit = 16f;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;

        // 4 frame 16x16 liền nhau, không có gutter; giữ nguyên ảnh nguồn.
        SpriteRect[] oldRects = provider.GetSpriteRects();
        SpriteRect[] rects = new SpriteRect[4];
        SpriteNameFileIdPair[] namePairs = new SpriteNameFileIdPair[4];
        for (int column = 0; column < 4; column++)
        {
            SpriteRect rect = new SpriteRect();
            rect.name = "CavemanIdle_" + column;
            rect.rect = new Rect(column * 16, 0, 16, 16);
            rect.alignment = SpriteAlignment.Center;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.spriteID = GUID.Generate();

            // Dùng lại ID nếu frame đã có, tránh đổi tham chiếu khi chạy lại.
            foreach (SpriteRect oldRect in oldRects)
            {
                if (oldRect.rect == rect.rect)
                {
                    rect.spriteID = oldRect.spriteID;
                    break;
                }
            }
            rects[column] = rect;
            namePairs[column] = new SpriteNameFileIdPair(rect.name, rect.spriteID);
        }

        provider.SetSpriteRects(rects);
        nameProvider.SetNameFileIdPairs(namePairs);
        provider.Apply();
        importer.SaveAndReimport();

        Sprite[] sprites = new Sprite[4];
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(imagePath);
        foreach (Object asset in assets)
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null)
            {
                for (int index = 0; index < sprites.Length; index++)
                {
                    if (sprite.name == "CavemanIdle_" + index)
                    {
                        sprites[index] = sprite;
                    }
                }
            }
        }

        for (int index = 0; index < sprites.Length; index++)
        {
            if (sprites[index] == null)
            {
                Debug.LogError("Không tải được frame CavemanIdle_" + index);
                return null;
            }
        }
        return sprites;
    }
}
