using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

// Explicit, repeatable migration: legacy artwork stays in the scene but is disabled.
[InitializeOnLoad]
public static class PolishCowExpandedMapSetup
{
    private const string MainPath = "Assets/_Game/Scenes/Main.unity";
    private const string Data = "Assets/_Game/Data/Defense";
    private const string Art = "Assets/_Game/Art/SproutLands/";
    private const string Request = "Temp/PolishCowExpandedMap.request";
    static PolishCowExpandedMapSetup() { EditorApplication.update += Poll; }
    private static void Poll()
    {
        if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string action = File.ReadAllText(Request).Trim();
        File.Delete(Request);
        try
        {
            if (action == "verify") Verify();
            else if (action == "smoke") PolishCowDefenseSmokeTest.Run();
            else if (action == "polish") Polish();
            else if (action == "test-scenes") PrepareTestScenes();
            else Setup();
        }
        catch (Exception exception)
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/ExpandedMapSetup.txt", "FAIL: " + exception);
            Debug.LogException(exception);
        }
    }

    [MenuItem("Tools/PolishCow/Expanded Map/Build Sprout Lands Map and Foundation")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode before map setup.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != MainPath || scene.isDirty) throw new Exception("Open saved Main in Edit Mode before map setup. Unsaved changes are preserved.");
        if (Find(scene, "ExpandedFarm") != null)
        {
            Verify();
            Debug.Log("ExpandedFarm already exists; preserving map edits. Verification completed.");
            return;
        }
        var inventory = Collect<InventoryManager>(scene).Single();
        var player = Collect<PlayerMovement>(scene).Single();
        var camera = Collect<Camera>(scene).Single(c => c.CompareTag("MainCamera"));
        if (Find(scene, "World") == null) throw new Exception("Main is missing World.");
        Directory.CreateDirectory(Data + "/Sprites");
        Directory.CreateDirectory(Data + "/Tiles");
        AssetDatabase.Refresh();

        // Backup is outside Assets, so it cannot accidentally enter Build Settings.
        Directory.CreateDirectory("output/backups");
        string backup = "output/backups/Main-before-expanded-map.unity";
        if (!File.Exists(backup)) File.Copy(MainPath, backup);

        try
        {
        CastleDefinition castle = Config<CastleDefinition>("Castle");
        WaveDefinition[] waves = CreateWaveData();
        CreateTowerData();
        Transform systems = inventory.transform;
        CastleProgress progress = Add<CastleProgress>(systems.gameObject);
        Set(progress, "definition", castle);
        CastleHealth health = Add<CastleHealth>(systems.gameObject);
        Set(health, "progress", progress);
        GameManager game = Add<GameManager>(systems.gameObject);
        Set(game, "castleProgress", progress); Set(game, "castleHealth", health); Set(game, "inventory", inventory);
        SetArray(game, "playerControls", new UnityEngine.Object[] { player, player.GetComponent<PlayerInteraction>() });
        WaveTimer timer = Add<WaveTimer>(systems.gameObject);
        Set(timer, "game", game); SetArray(timer, "waves", waves);
        // No automatic combat until an actual WaveSpawner subscribes to OnWaveStarted.
        Set(timer, "startAutomatically", false);
        Transform map = Child(Find(scene, "World").transform, "ExpandedFarm");
        FeatureReferences references = Add<FeatureReferences>(map.gameObject);

        Tile[] grass = Tiles("Grass", "Tilesets/Grass.png");
        Tile[] dirt = Tiles("Path", "Tilesets/Tilled_Dirt.png");
        Tile water = TileAsset("Water", SpriteAsset("Water", "Tilesets/Water.png", 0, 0, 16, 16));
        Sprite marker = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Prototype/PrototypeMarker.asset");
        if (marker == null) throw new Exception("PrototypeMarker missing.");

        var waterMap = MakeTilemap(map, "Water", -20020);
        for (int y = -28; y < 28; y++) for (int x = -36; x < 36; x++) waterMap.SetTile(new Vector3Int(x, y, 0), water);
        var ground = MakeTilemap(map, "Ground", -20000);
        var roads = MakeTilemap(map, "Paths", -19990);
        for (int y = -24; y < 24; y++) for (int x = -32; x < 32; x++)
        {
            int col = x == -32 ? 0 : x == 31 ? 2 : 1;
            int row = y == 23 ? 0 : y == -24 ? 2 : 1;
            ground.SetTile(new Vector3Int(x, y, 0), grass[row * 3 + col]);
            if (IsRoad(x, y))
            {
                col = !IsRoad(x - 1, y) ? 0 : !IsRoad(x + 1, y) ? 2 : 1;
                row = !IsRoad(x, y + 1) ? 0 : !IsRoad(x, y - 1) ? 2 : 1;
                roads.SetTile(new Vector3Int(x, y, 0), dirt[row * 3 + col]);
            }
        }

        MapZone[] zones = {
            Zone(map, "CentralFarm", Vector2.zero, new Vector2(32,24), 1, progress, marker),
            Zone(map, "WestPasture", new Vector2(-24,0), new Vector2(16,24), 2, progress, marker),
            Zone(map, "EastWorkshop", new Vector2(24,0), new Vector2(16,24), 2, progress, marker),
            Zone(map, "NorthKeep", new Vector2(0,18), new Vector2(64,12), 3, progress, marker),
            Zone(map, "SouthFields", new Vector2(0,-18), new Vector2(64,12), 3, progress, marker)
        };
        MakeBoundaries(map);
        MakeArtwork(map, zones);
        MakeSlots(zones, references, progress, scene);
        MakeRoutes(zones);
        var follow = Add<ExpandedMapCamera>(camera.gameObject);
        Set(follow, "target", player.transform); SetArray(follow, "zones", zones);
        camera.orthographicSize = 7.5f;
        camera.backgroundColor = new Color(0.48f, 0.72f, 0.67f);
        camera.transform.position = new Vector3(0, -1, -10);

        // Deactivate only old visual terrain/boundaries; feature prefabs stay intact.
        Transform legacy = Find(scene, "World").transform.Find("PrototypeMap");
        if (legacy != null)
        {
            foreach (string name in new[] { "Grid", "Obstacles", "WorldArt" })
            {
                Transform part = legacy.Find(name);
                if (part != null) part.gameObject.SetActive(false);
            }
        }
        foreach (OrderController orders in Collect<OrderController>(scene)) Set(orders, "limitDemoOrders", false);
        var initializer = inventory.GetComponent<FeatureReferenceInitializer>();
        if (initializer == null) throw new Exception("Main is missing FeatureReferenceInitializer.");
        SetArray(initializer, "features", Collect<FeatureReferences>(scene).Cast<UnityEngine.Object>().ToArray());
        MakeHUD(scene, progress, health, game, timer, inventory);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Failed to save expanded Main.");
        Verify();
        Selection.activeGameObject = map.gameObject;
        SceneView.lastActiveSceneView?.Frame(new Bounds(Vector3.zero, new Vector3(68, 52, 1)), false);
        Debug.Log("Sprout Lands 64x48 map and defense foundation saved in Main. See Docs/DEFENSE_BASE.md.");
        }
        catch
        {
            // Setup began only from a saved scene; discard an incomplete migration.
            EditorSceneManager.OpenScene(MainPath, OpenSceneMode.Single);
            throw;
        }
    }

    private static bool IsRoad(int x, int y)
    {
        if (x < -31 || x > 30 || y < -23 || y > 22) return false;
        return y == -1 || y == 0 || x == -1 || x == 0 ||
            (y >= 0 && y <= 8 && (x == -4 || x == -3 || x == 2 || x == 3)) ||
            (y == -4 || y == -3) && x >= 0 && x <= 10 ||
            (y == 7 || y == 8) && x >= -12 && x <= 12;
    }
    private static MapZone Zone(Transform parent, string name, Vector2 center, Vector2 size, int level, CastleProgress progress, Sprite marker)
    {
        Transform root = Child(parent, name); root.localPosition = center;
        MapZone zone = root.gameObject.AddComponent<MapZone>();
        Set(zone, "progress", progress); Set(zone, "requiredLevel", level); Set(zone, "size", size);
        Transform content = Child(root, "Content");
        Transform locked = Child(root, "Locked");
        var overlay = locked.gameObject.AddComponent<SpriteRenderer>();
        overlay.sprite = marker; overlay.color = new Color(0.12f, 0.22f, 0.24f, 0.78f); overlay.sortingOrder = 9000;
        locked.localScale = size;
        locked.gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
        TextMeshPro label = Label(root, "UnlockLabel", "CASTLE LEVEL " + level, center, 3, 9010);
        label.transform.SetParent(locked, true);
        label.transform.localScale = new Vector3(1f / size.x, 1f / size.y, 1);
        Set(zone, "lockedOverlay", locked.gameObject); Set(zone, "unlockedContent", content.gameObject);
        locked.gameObject.SetActive(level > 1); content.gameObject.SetActive(level == 1);
        return zone;
    }
    private static void MakeBoundaries(Transform map)
    {
        Transform bounds = Child(map, "Boundaries");
        Wall(bounds, "West", new Vector2(-32.5f,0), new Vector2(1,50));
        Wall(bounds, "East", new Vector2(32.5f,0), new Vector2(1,50));
        Wall(bounds, "North", new Vector2(0,24.5f), new Vector2(66,1));
        Wall(bounds, "South", new Vector2(0,-24.5f), new Vector2(66,1));
    }
    private static void Wall(Transform parent, string name, Vector2 position, Vector2 size)
    { Transform wall = Child(parent, name); wall.localPosition = position; wall.gameObject.AddComponent<BoxCollider2D>().size = size; }

    private static void MakeArtwork(Transform map, MapZone[] zones)
    {
        Sprite tree = SpriteAsset("Tree", "Objects/Basic_Grass_Biom_things.png", 0, 0, 16, 32, true);
        Sprite shrub = SpriteAsset("Shrub", "Objects/Basic_Grass_Biom_things.png", 48, 16, 16, 16, true);
        Sprite flower = SpriteAsset("Flowers", "Objects/Basic_Grass_Biom_things.png", 80, 16, 16, 16, true);
        Sprite house = SpriteAsset("FarmsteadHouse", "Objects/Free_Chicken_House.png", 0, 0, 48, 48, true);
        Sprite fence = SpriteAsset("Fence", "Tilesets/Fences.png", 16, 0, 16, 16, true);
        Transform central = zones[0].transform.Find("Content");
        Decoration(central, "BarnHouse", house, new Vector2(-7,3.6f), 1.2f, true);
        Decoration(central, "WarehouseHouse", house, new Vector2(8,4), 1.2f, true);
        Decoration(central, "KeepHouse", house, new Vector2(0,8.7f), 1.3f, true);
        Label(central, "CastleLabel", "MILKWOOD KEEP", new Vector2(0,10), 3, 100);
        // Keep the original cow's roaming area clear and the south barn gate two units wide.
        for (int i = 0; i < 6; i++)
        {
            Decoration(central, "BarnBack" + i, fence, new Vector2(-9.5f + i, 5.4f), 1, false);
            if (i < 2 || i > 3) Decoration(central, "BarnFront" + i, fence, new Vector2(-9.5f + i, 1.2f), 1, false);
        }
        foreach (MapZone zone in zones)
        {
            Transform content = zone.transform.Find("Content");
            Bounds b = zone.WorldBounds;
            int count = zone.RequiredLevel == 1 ? 14 : 20;
            var random = new System.Random(820 + Array.IndexOf(zones, zone));
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(b.min.x + 1.5f, b.max.x - 1.5f, (float)random.NextDouble());
                float y = Mathf.Lerp(b.min.y + 1.5f, b.max.y - 1.5f, (float)random.NextDouble());
                // Central gameplay area, roads, build pads and spawn lanes stay unobstructed.
                if (Mathf.Abs(x) < 13 && y > -7 && y < 9 || Mathf.Abs(x) < 2 || Mathf.Abs(y) < 2) continue;
                Sprite sprite = i % 3 == 0 ? tree : i % 3 == 1 ? shrub : flower;
                Decoration(content, "Nature" + i, sprite, new Vector2(x,y), i % 3 == 0 ? 1.5f : 1, false);
            }
        }
        Label(zones[1].transform.Find("Content"), "Name", "WEST PASTURE", new Vector2(-24,9), 2.5f, 100);
        Label(zones[2].transform.Find("Content"), "Name", "EAST WORKSHOP", new Vector2(24,9), 2.5f, 100);
        Label(zones[3].transform.Find("Content"), "Name", "NORTH KEEP", new Vector2(0,21), 2.5f, 100);
        Label(zones[4].transform.Find("Content"), "Name", "SOUTH FIELDS", new Vector2(0,-21), 2.5f, 100);
    }
    private static void Decoration(Transform parent, string name, Sprite sprite, Vector2 worldPosition, float scale, bool solid)
    {
        Transform obj = Child(parent, name); obj.position = worldPosition; obj.localScale = Vector3.one * scale;
        var renderer = obj.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
        obj.gameObject.AddComponent<WorldDepthSorting>();
        renderer.sortingOrder = WorldDepthSorting.GetSortingOrder(worldPosition.y);
        if (solid) { var collider = obj.gameObject.AddComponent<BoxCollider2D>(); collider.size = new Vector2(2.3f,0.5f); collider.offset = new Vector2(0,0.25f); }
    }

    private static void MakeSlots(MapZone[] zones, FeatureReferences references, CastleProgress progress, Scene scene)
    {
        Sprite pad = SpriteAsset("BuildPadStone", "Tilesets/Tilled_Dirt.png", 0, 0, 48, 48);
        Vector2[][] towers = {
            new[] { new Vector2(-12,-4),new Vector2(-6,-8),new Vector2(6,-8),new Vector2(12,-4) },
            new[] { new Vector2(-26,-4),new Vector2(-20,4),new Vector2(20,4),new Vector2(26,-4) },
            new[] { new Vector2(-12,18),new Vector2(12,18),new Vector2(-12,-18),new Vector2(12,-18) }
        };
        for (int level = 1; level <= 3; level++)
            foreach (Vector2 position in towers[level - 1]) Slot(ZoneFor(zones,position), DefenseBuildingSlot.Kind.Tower, position, level, references, progress, pad);
        Vector2[] cows = { new Vector2(-7,3),new Vector2(-10,-5),new Vector2(-10,-9),new Vector2(-23,5),new Vector2(-23,-7),new Vector2(-20,18),new Vector2(-24,-18),new Vector2(-18,-18) };
        for (int i = 0; i < cows.Length; i++)
        {
            DefenseBuildingSlot slot = Slot(ZoneFor(zones,cows[i]), DefenseBuildingSlot.Kind.Cow, cows[i], i < 3 ? 1 : i < 5 ? 2 : 3, references, progress, pad);
            if (i == 0)
            {
                var cow = Collect<CowUpgrade>(scene).FirstOrDefault();
                if (cow != null) Set(slot, "initialOccupant", cow.gameObject);
            }
        }
        Vector2[] machines = { new Vector2(0,3),new Vector2(24,5),new Vector2(20,18),new Vector2(20,-18) };
        for (int i = 0; i < machines.Length; i++)
        {
            DefenseBuildingSlot slot = Slot(ZoneFor(zones,machines[i]), DefenseBuildingSlot.Kind.Machine, machines[i], i == 0 ? 1 : i == 1 ? 2 : 3, references, progress, pad);
            if (i == 0)
            {
                var machine = Collect<ProductionMachine>(scene).FirstOrDefault();
                if (machine != null) Set(slot, "initialOccupant", machine.gameObject);
            }
        }
    }
    private static MapZone ZoneFor(MapZone[] zones, Vector2 position) => zones.First(z => z.WorldBounds.Contains(new Vector3(position.x,position.y,0)));
    private static DefenseBuildingSlot Slot(MapZone zone, DefenseBuildingSlot.Kind kind, Vector2 position, int level, FeatureReferences references, CastleProgress progress, Sprite pad)
    {
        Transform root = Child(zone.transform.Find("Content"), kind + "Slot_" + position.x + "_" + position.y); root.position = position;
        DefenseBuildingSlot slot = root.gameObject.AddComponent<DefenseBuildingSlot>();
        Set(slot, "kind", (int)kind); Set(slot, "references", references); Set(slot, "progress", progress); Set(slot, "requiredLevel", level);
        var renderer = root.gameObject.AddComponent<SpriteRenderer>(); renderer.sprite = pad; renderer.sortingOrder = -19000;
        renderer.drawMode = SpriteDrawMode.Sliced; renderer.size = Vector2.one;
        renderer.color = kind == DefenseBuildingSlot.Kind.Tower ? new Color(0.9f,0.65f,0.38f,0.8f) : new Color(0.6f,0.85f,0.8f,0.65f);
        return slot;
    }
    private static void MakeRoutes(MapZone[] zones)
    {
        Route(zones[0], "CentralWest", new[] { new Vector2(-15, -0.5f),new Vector2(-2,-0.5f),new Vector2(-2,8),new Vector2(0,8) });
        Route(zones[0], "CentralEast", new[] { new Vector2(15,-0.5f),new Vector2(3,-0.5f),new Vector2(3,8),new Vector2(0,8) });
        Route(zones[1], "WestApproach", new[] { new Vector2(-31,-0.5f),new Vector2(-15,-0.5f),new Vector2(-2,-0.5f),new Vector2(-2,8),new Vector2(0,8) });
        Route(zones[2], "EastApproach", new[] { new Vector2(31,-0.5f),new Vector2(15,-0.5f),new Vector2(3,-0.5f),new Vector2(3,8),new Vector2(0,8) });
        Route(zones[3], "NorthApproach", new[] { new Vector2(-0.5f,23),new Vector2(-0.5f,15),new Vector2(0,8) });
        Route(zones[4], "SouthApproach", new[] { new Vector2(-0.5f,-23),new Vector2(-0.5f,-10),new Vector2(-0.5f,8),new Vector2(0,8) });
        // Explicit target anchor for CastleHealth/VFX integration by the combat module.
        Child(zones[0].transform.Find("Content"), "CastleTarget").position = new Vector3(0,8,0);
    }
    private static void Route(MapZone zone, string name, Vector2[] positions)
    {
        Transform root = Child(zone.transform.Find("Content"), name);
        var route = root.gameObject.AddComponent<EnemyRoute>();
        Transform[] points = positions.Select((position,index) => { Transform p = Child(root,"Waypoint" + index); p.position = position; return p; }).ToArray();
        SetArray(route,"waypoints",points);
    }

    private static void MakeHUD(Scene scene, CastleProgress progress, CastleHealth health, GameManager game, WaveTimer timer, InventoryManager inventory)
    {
        Canvas canvas = Collect<Canvas>(scene).First(c => c.renderMode == RenderMode.ScreenSpaceOverlay);
        GameObject panel = new GameObject("CastleHUD",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        panel.transform.SetParent(canvas.transform,false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1); rect.anchoredPosition = new Vector2(24,-120); rect.sizeDelta = new Vector2(510,112);
        panel.GetComponent<Image>().color = new Color(0.08f,0.16f,0.16f,0.88f); panel.GetComponent<Image>().raycastTarget = false;
        GameObject textObj = new GameObject("Status",typeof(RectTransform),typeof(CanvasRenderer),typeof(TextMeshProUGUI));
        textObj.transform.SetParent(panel.transform,false);
        var text = textObj.GetComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.fontSize = 21; text.color = new Color(1,0.95f,0.8f); text.raycastTarget = false;
        text.text = "CASTLE 1    HP 120/120\nEXP 0 / 495    |    Wave 0/10 - Preparing\nMedicine 0  |  Spoiled milk 0";
        text.rectTransform.anchorMin = Vector2.zero; text.rectTransform.anchorMax = Vector2.one; text.rectTransform.offsetMin = new Vector2(14,8); text.rectTransform.offsetMax = new Vector2(-14,-8);
        CastleHUD hud = panel.AddComponent<CastleHUD>();
        Set(hud,"progress",progress); Set(hud,"health",health); Set(hud,"game",game); Set(hud,"timer",timer); Set(hud,"inventory",inventory); Set(hud,"statusText",text);
    }

    private static WaveDefinition[] CreateWaveData()
    {
        int[] hp = {30,20,120,10,2000}, damage = {5,3,15,0,100}, credits = {5,6,15,3,300}, exp = {10,12,40,8,500};
        float[] speed = {1,2,0.6f,2.5f,0.5f};
        EnemyDefinition[] enemies = new EnemyDefinition[5];
        for (int i = 0; i < 5; i++)
        {
            bool exists = File.Exists(Data + "/Enemy" + i + ".asset");
            enemies[i] = Config<EnemyDefinition>("Enemy" + i);
            if (!exists) { enemies[i].kind = (EnemyDefinition.Kind)i; enemies[i].health = hp[i]; enemies[i].castleDamage = damage[i]; enemies[i].credits = credits[i]; enemies[i].experience = exp[i]; enemies[i].speed = speed[i]; EditorUtility.SetDirty(enemies[i]); }
        }
        int[,] counts = { {5,0,0,0,0},{8,0,0,0,0},{8,4,0,2,0},{10,6,2,3,0},{12,8,3,4,0},{14,10,5,5,0},{16,12,7,6,0},{18,14,8,7,0},{20,14,9,8,0},{20,16,12,6,1} };
        float[] preparation = {90,90,100,100,110,110,120,120,120,180};
        WaveDefinition[] waves = new WaveDefinition[10];
        for (int w = 0; w < 10; w++)
        {
            bool exists = File.Exists(Data + "/Wave" + (w+1).ToString("00") + ".asset");
            waves[w] = Config<WaveDefinition>("Wave" + (w+1).ToString("00"));
            if (exists) continue;
            waves[w].preparationSeconds = preparation[w]; waves[w].healthMultiplier = 1 + 0.08f * w; waves[w].completionCredits = 50*(w+1);
            waves[w].groups = Enumerable.Range(0,5).Where(i => counts[w,i] > 0).Select(i => new WaveDefinition.Group { enemy = enemies[i], count = counts[w,i], spawnInterval = 0.8f }).ToArray();
            EditorUtility.SetDirty(waves[w]);
        }
        return waves;
    }
    private static void CreateTowerData()
    {
        int[] prices = {80,150,120,200}, unlocks = {1,2,2,3};
        int[][] upgrades = {new[]{60,120},new[]{100,180},new[]{90,150},new[]{140,240}};
        int[][] damages = {new[]{8,12,18},new[]{25,38,55},new[]{5,8,12},new[]{40,60,90}};
        float[] shots = {1.2f,0.5f,1,0.4f}, ranges = {4,3.5f,3.5f,6};
        for (int i = 0; i < 4; i++)
        {
            if (File.Exists(Data + "/Tower" + i + ".asset")) continue;
            TowerDefinition tower = Config<TowerDefinition>("Tower" + i);
            tower.kind = (TowerDefinition.Kind)i; tower.buildPrice = prices[i]; tower.unlockCastleLevel = unlocks[i]; tower.upgradePrices = upgrades[i]; tower.damage = damages[i]; tower.shotsPerSecond = shots[i]; tower.range = ranges[i]; EditorUtility.SetDirty(tower);
        }
    }
    private static T Config<T>(string name) where T : ScriptableObject
    {
        string path = Data + "/" + name + ".asset";
        T config = AssetDatabase.LoadAssetAtPath<T>(path);
        if (config == null) { config = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(config,path); }
        return config;
    }
    private static Tile[] Tiles(string name, string texture)
    {
        Tile[] tiles = new Tile[9];
        for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
            tiles[y*3+x] = TileAsset(name + x + y, SpriteAsset(name + x + y,texture,x*16,y*16,16,16));
        return tiles;
    }
    private static Sprite SpriteAsset(string name, string texturePath, int x, int top, int width, int height, bool bottomPivot = false)
    {
        string path = Data + "/Sprites/" + name + ".asset";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null && Mathf.Approximately(sprite.pivot.y, bottomPivot ? 0 : height * 0.5f)) return sprite;
        string source = Art + texturePath;
        var importer = (TextureImporter)AssetImporter.GetAtPath(source);
        if (importer == null) throw new Exception("Missing texture " + source);
        importer.textureType = TextureImporterType.Sprite;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.alphaIsTransparency = true; importer.spritePixelsPerUnit = 16;
        importer.SaveAndReimport();
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(source);
        if (x + width > texture.width || top + height > texture.height) throw new Exception("Invalid sprite rect: " + source + " " + name);
        Sprite replacement = Sprite.Create(texture,new Rect(x,texture.height-top-height,width,height),new Vector2(0.5f,bottomPivot ? 0 : 0.5f),16,0,SpriteMeshType.FullRect);
        replacement.name = name;
        if (sprite != null) { EditorUtility.CopySerialized(replacement,sprite); UnityEngine.Object.DestroyImmediate(replacement); EditorUtility.SetDirty(sprite); }
        else { sprite = replacement; AssetDatabase.CreateAsset(sprite,path); }
        return sprite;
    }
    private static Tile TileAsset(string name, Sprite sprite)
    {
        string path = Data + "/Tiles/" + name + ".asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = sprite; tile.colliderType = Tile.ColliderType.None; AssetDatabase.CreateAsset(tile,path); }
        return tile;
    }
    private static Tilemap MakeTilemap(Transform parent, string name, int order)
    {
        Transform grid = Child(parent,"Grid"); Add<Grid>(grid.gameObject);
        Transform root = Child(grid,name); var map = Add<Tilemap>(root.gameObject); Add<TilemapRenderer>(root.gameObject).sortingOrder = order; return map;
    }
    private static TextMeshPro Label(Transform parent, string name, string text, Vector2 position, float size, int order)
    {
        Transform obj = Child(parent,name); obj.position = position;
        var label = obj.gameObject.AddComponent<TextMeshPro>(); label.text = text; label.font = TMP_Settings.defaultFontAsset; label.fontSize = size; label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(1,0.96f,0.8f); label.sortingOrder = order; label.rectTransform.sizeDelta = new Vector2(14,2); return label;
    }
    private static T Add<T>(GameObject target) where T : Component
    { T component = target.GetComponent<T>(); return component != null ? component : target.AddComponent<T>(); }
    private static Transform Child(Transform parent, string name)
    { Transform found = parent.Find(name); if (found != null) return found; var obj = new GameObject(name); obj.transform.SetParent(parent,false); return obj.transform; }
    private static GameObject Find(Scene scene, string name) => scene.GetRootGameObjects().FirstOrDefault(o => o.name == name);
    private static T[] Collect<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>(true)).ToArray();
    private static void Set(UnityEngine.Object target, string name, object value)
    {
        var data = new SerializedObject(target); var field = data.FindProperty(name);
        if (field == null) throw new Exception(target.name + " missing " + name);
        if (value is int integer) field.intValue = integer;
        else if (value is bool boolean) field.boolValue = boolean;
        else if (value is Vector2 vector) field.vector2Value = vector;
        else field.objectReferenceValue = value as UnityEngine.Object;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetArray(UnityEngine.Object target, string name, UnityEngine.Object[] values)
    {
        var data = new SerializedObject(target); var array = data.FindProperty(name); array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Tools/PolishCow/Expanded Map/Verify Map and Foundation")]
    public static void Verify()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != MainPath) throw new Exception("Open Main to verify the expanded map.");
        Require(Collect<MapZone>(scene).Length == 5,"Five map zones");
        Require(Collect<CastleProgress>(scene).Length == 1 && Collect<GameManager>(scene).Length == 1,"One shared castle/game foundation");
        Require(Collect<EnemyRoute>(scene).Length == 6,"Six waypoint routes");
        var slots = Collect<DefenseBuildingSlot>(scene);
        Require(slots.Count(s => s.SlotKind == DefenseBuildingSlot.Kind.Tower) == 12,"12 tower slots");
        Require(slots.Count(s => s.SlotKind == DefenseBuildingSlot.Kind.Cow) == 8,"8 cow slots");
        Require(slots.Count(s => s.SlotKind == DefenseBuildingSlot.Kind.Machine) == 4,"4 machine slots");
        Require(Collect<PlayerMovement>(scene).Length == 1 && Collect<InventoryManager>(scene).Length == 1,"One player and inventory");
        var waves = AssetDatabase.FindAssets("t:WaveDefinition",new[]{Data});
        Require(waves.Length == 10,"10 configured waves");
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/ExpandedMapSetup.txt","PASS: Sprout Lands map 64x48; 5 zones; 12/8/4 slots; 6 routes; 10 waves; one player/inventory/game foundation.\n");
        Debug.Log("PASS Expanded map foundation verification.");
    }
    private static void Require(bool condition, string message) { if (!condition) throw new Exception("Verify failed: " + message); }

    [MenuItem("Tools/PolishCow/Expanded Map/Refresh Keep Artwork")]
    public static void Polish()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != MainPath || scene.isDirty) throw new Exception("Open saved Main in Edit Mode.");
        Transform central = Find(scene,"World").transform.Find("ExpandedFarm/CentralFarm/Content");
        if (central == null) throw new Exception("Build the expanded map first.");
        Sprite house = SpriteAsset("FarmsteadHouse","Objects/Free_Chicken_House.png",0,0,48,48,true);
        if (central.Find("KeepHouse") == null) Decoration(central,"KeepHouse",house,new Vector2(0,8.7f),1.3f,true);
        foreach (string name in new[]{"BarnHouse","WarehouseHouse","KeepHouse"})
        {
            Transform building = central.Find(name);
            if (building != null) building.GetComponent<SpriteRenderer>().sprite = house;
        }
        Sprite pad = SpriteAsset("BuildPadStone","Tilesets/Tilled_Dirt.png",0,0,48,48);
        foreach (DefenseBuildingSlot slot in Collect<DefenseBuildingSlot>(scene))
        {
            var renderer = slot.GetComponent<SpriteRenderer>();
            if (renderer != null) { renderer.sprite = pad; renderer.drawMode = SpriteDrawMode.Sliced; renderer.size = Vector2.one; }
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    [MenuItem("Tools/PolishCow/Expanded Map/Prepare Defense Test Scenes")]
    public static void PrepareTestScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Exit Play Mode before preparing test scenes.");
        Scene original = SceneManager.GetActiveScene();
        if (original.isDirty) throw new Exception("Save the current scene first. Test setup preserves existing scenes.");
        CastleDefinition castle = AssetDatabase.LoadAssetAtPath<CastleDefinition>(Data + "/Castle.asset");
        if (castle == null) throw new Exception("Build the Main foundation first.");
        foreach (string sceneName in new[] { "CoreTest", "EnemyTest", "TowerTest" })
        {
            string path = "Assets/_Game/Scenes/Tests/" + sceneName + ".unity";
            if (File.Exists(path)) continue;
            Scene test = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(test);
            try
            {
                var systems = new GameObject("GameSystems");
                var inventory = systems.AddComponent<InventoryManager>(); Set(inventory,"credits",1000);
                var progress = systems.AddComponent<CastleProgress>(); Set(progress,"definition",castle);
                var health = systems.AddComponent<CastleHealth>(); Set(health,"progress",progress);
                var game = systems.AddComponent<GameManager>(); Set(game,"castleProgress",progress); Set(game,"castleHealth",health); Set(game,"inventory",inventory);
                var timer = systems.AddComponent<WaveTimer>(); Set(timer,"game",game);
                SetArray(timer,"waves",Enumerable.Range(1,10).Select(i => (UnityEngine.Object)AssetDatabase.LoadAssetAtPath<WaveDefinition>(Data + "/Wave" + i.ToString("00") + ".asset")).ToArray());
                var references = systems.AddComponent<FeatureReferences>();
                var initializer = systems.AddComponent<FeatureReferenceInitializer>(); Set(initializer,"inventory",inventory); SetArray(initializer,"features",new UnityEngine.Object[]{references});
                var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 10; camera.backgroundColor = new Color(0.48f,0.72f,0.67f); camera.transform.position = new Vector3(0,0,-10); cameraObject.AddComponent<AudioListener>();
                var canvasObject = new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
                canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
                MakeHUD(test,progress,health,game,timer,inventory);
                var work = new GameObject("FeatureWork");
                var ground = MakeTilemap(work.transform,"Ground",-20000);
                Tile grass = AssetDatabase.LoadAssetAtPath<Tile>(Data + "/Tiles/Grass11.asset");
                for (int y = -10; y < 10; y++) for (int x = -16; x < 16; x++) ground.SetTile(new Vector3Int(x,y,0),grass);
                var zone = Zone(work.transform,"TestZone",Vector2.zero,new Vector2(32,20),1,progress,AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Data/Prototype/PrototypeMarker.asset"));
                Sprite pad = AssetDatabase.LoadAssetAtPath<Sprite>(Data + "/Sprites/BuildPadStone.asset");
                for (int i = 0; i < 4; i++) Slot(zone,DefenseBuildingSlot.Kind.Tower,new Vector2(-6+i*4,-3),1,references,progress,pad);
                Route(zone,"TestRoute",new[]{new Vector2(-14,0),new Vector2(0,0),new Vector2(0,6)});
                Child(zone.transform.Find("Content"),"CastleTarget").position = new Vector3(0,6,0);
                Label(work.transform,"Instructions",sceneName + "  |  FeatureWork",new Vector2(0,8),3,100);
                if (!EditorSceneManager.SaveScene(test,path)) throw new Exception("Could not save " + sceneName);
            }
            finally { EditorSceneManager.CloseScene(test,true); SceneManager.SetActiveScene(original); }
        }
        Debug.Log("CoreTest, EnemyTest and TowerTest are ready; existing scenes were preserved.");
    }
}
