using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Compose the saved member workspaces without changing their source scenes or prefabs.
public static class PolishCowAllFeaturesSetup
{
    private const string MainPath = "Assets/_Game/Scenes/Main.unity";

    [MenuItem("Tools/PolishCow/Integrate All Features Into Main")]
    public static void Integrate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Exit Play Mode first.");
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).isDirty)
                throw new InvalidOperationException("Save open scene changes before integration.");
        PolishCowTowerLevelSetup.Configure();
        var main = SceneManager.GetSceneByPath(MainPath);
        if (!main.isLoaded) main = EditorSceneManager.OpenScene(MainPath);
        SceneManager.SetActiveScene(main);
        var inventory = All<InventoryManager>(main).Single();
        var context = All<DefenseSceneContext>(main).Single();
        var towers = All<TowersFeature>(main).Single();
        var building = All<BuildingFeature>(main).Single();
        var catalog = AssetDatabase.LoadAssetAtPath<BuildingCatalogData>("Assets/_Game/Data/Features/Building/BuildingCatalog.asset");
        if (catalog == null) throw new InvalidOperationException("Missing building catalog.");
        Set(building, "catalog", catalog);
        foreach (var entry in catalog.Entries)
        {
            if (entry.slotKind == DefenseBuildingSlot.Kind.Tower)
            {
                string kind = entry.id.Substring("tower_".Length);
                kind = char.ToUpperInvariant(kind[0]) + kind.Substring(1);
                entry.prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Features/Tower/Tower_" + kind + ".prefab");
                var definition = entry.prefab != null ? entry.prefab.GetComponent<TowerController>()?.Definition : null;
                if (definition == null) throw new InvalidOperationException("Missing tower configuration: " + entry.id);
                entry.buildPrice = definition.buildPrice;
                entry.unlockCastleLevel = definition.unlockCastleLevel;
                entry.upgradePrices = definition.upgradePrices;
                entry.refundRatio = .6f;
            }
            else entry.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(entry.slotKind == DefenseBuildingSlot.Kind.Cow
                ? "Assets/_Game/Prefabs/Features/Cows/CowBarn.prefab"
                : "Assets/_Game/Prefabs/Features/Production/ProductionMachine.prefab");
        }
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssetIfDirty(catalog);
        var enemies = All<EnemiesFeature>(main).Single();
        if (enemies.GetComponent<WaveSpawner>() == null) enemies.gameObject.AddComponent<WaveSpawner>();

        if (!main.GetRootGameObjects().Any(o => o.name == "DefenseCanvas"))
        {
            const string workspacePath = "Assets/_Game/Scenes/Workspaces/TowersWorkspace.unity";
            var workspace = SceneManager.GetSceneByPath(workspacePath);
            bool opened = !workspace.isLoaded;
            if (opened) workspace = EditorSceneManager.OpenScene(workspacePath, OpenSceneMode.Additive);
            try
            {
                var source = All<DefenseShopUI>(workspace).Single().transform.root.gameObject;
                var copy = UnityEngine.Object.Instantiate(source);
                copy.name = "DefenseCanvas";
                SceneManager.MoveGameObjectToScene(copy, main);
                foreach (Transform child in copy.transform.Cast<Transform>().ToArray())
                    if (child.name != "ShopPanel" && child.name != "TowerActionPanel" && child.name != "BattleUIPanels")
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
            finally
            {
                if (opened) EditorSceneManager.CloseScene(workspace, true);
                SceneManager.SetActiveScene(main);
            }
        }
        var shop = All<DefenseShopUI>(main).Single();
        var actions = All<TowerActionMenuUI>(main).Single();
        var battle = All<BattleUI>(main).Single();
        battle.gameObject.SetActive(true);
        Set(towers, "shopUI", shop);
        Set(towers, "actionMenuUI", actions);
        Set(towers, "battleUI", battle);
        var playerData = new SerializedObject(All<PlayerInteraction>(main).Single());
        var hint = playerData.FindProperty("interactionHint").objectReferenceValue as TMP_Text;
        if (hint != null && hint.transform.parent.TryGetComponent<InteractionHintCard>(out var card))
        {
            Layout(card.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(0, 145), new Vector2(740, 66));
            PrefabUtility.RecordPrefabInstancePropertyModifications(card.GetComponent<RectTransform>());
        }
        var defenseCanvas = shop.GetComponentInParent<Canvas>();
        defenseCanvas.sortingOrder = 10;
        var scaler = defenseCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
        Layout(shop.GetComponent<RectTransform>(), new Vector2(.5f, 0), new Vector2(0, 18), new Vector2(640, 96));
        var shopData = new SerializedObject(shop);
        var entries = shopData.FindProperty("entries");
        for (int i = 0; i < entries.arraySize; i++)
        {
            var entry = entries.GetArrayElementAtIndex(i);
            var cost = entry.FindPropertyRelative("costText").objectReferenceValue as TMP_Text;
            var locked = entry.FindPropertyRelative("lockReasonText").objectReferenceValue as TMP_Text;
            foreach (var label in new[] { cost, locked })
            {
                if (label == null) continue;
                label.enableAutoSizing = true;
                label.fontSizeMin = 12;
                label.fontSizeMax = 18;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                label.rectTransform.anchorMin = Vector2.zero;
                label.rectTransform.anchorMax = Vector2.one;
                label.rectTransform.offsetMin = new Vector2(6, 6);
                label.rectTransform.offsetMax = new Vector2(-6, -6);
            }
            if (locked != null) locked.color = new Color(1, .82f, .42f);
            var overlay = entry.FindPropertyRelative("lockOverlay").objectReferenceValue as GameObject;
            if (overlay != null && overlay.TryGetComponent<Image>(out var image))
                image.color = new Color(.1f, .14f, .12f, .96f);
        }
        PolishCowUIPolish.CompactTowerShop(main);
        Layout(actions.GetComponent<RectTransform>(), new Vector2(.5f, .5f), Vector2.zero, new Vector2(400, 380));
        var actionData = new SerializedObject(actions);
        var towerTitle = (TMP_Text)actionData.FindProperty("txtTitle").objectReferenceValue;
        StyleText(towerTitle, new Vector2(0, 130), new Vector2(310, 48), 26);
        var statsTransform = actions.transform.Find("TowerStats");
        if (statsTransform == null)
        {
            var statsObject = new GameObject("TowerStats", typeof(RectTransform), typeof(TextMeshProUGUI));
            statsObject.transform.SetParent(actions.transform, false);
            statsTransform = statsObject.transform;
        }
        var statsText = statsTransform.GetComponent<TextMeshProUGUI>();
        statsText.font = towerTitle.font;
        StyleText(statsText, new Vector2(0, 55), new Vector2(350, 80), 23);
        Set(actions, "txtStats", statsText);
        StyleButton((Button)actionData.FindProperty("btnUpgrade").objectReferenceValue, new Vector2(0, -35), new Vector2(300, 52));
        StyleButton((Button)actionData.FindProperty("btnSell").objectReferenceValue, new Vector2(0, -105), new Vector2(300, 52));
        actions.GetComponent<Image>().color = new Color(.04f, .1f, .08f, .98f);
        ConfigureBattlePanels(battle);
        // The parent is only a container. It must not intercept world clicks.
        var backdrop = battle.GetComponent<Image>();
        if (backdrop != null) { backdrop.color = Color.clear; backdrop.raycastTarget = false; }
        var close = actions.transform.Find("btn_Close")?.GetComponent<Button>();
        if (close != null)
        {
            Layout(close.GetComponent<RectTransform>(), new Vector2(.5f, .5f), new Vector2(175, 160), new Vector2(32, 32));
            while (close.onClick.GetPersistentEventCount() > 0)
                UnityEditor.Events.UnityEventTools.RemovePersistentListener(close.onClick, 0);
            UnityEditor.Events.UnityEventTools.AddPersistentListener(close.onClick, actions.CloseMenu);
        }
        if (!All<BuildingPanel>(main).Any())
            PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Features/Building/BuildingCanvas.prefab"), main);
        var buildingPanel = All<BuildingPanel>(main).Single();
        Set(building, "panel", buildingPanel);
        ConfigureBuildingPanel(buildingPanel);

        string[] names = { "CowShop", "MedicineShop", "MilkShop" };
        Vector3[] positions = { new Vector3(-5,-6), new Vector3(-2,-6), new Vector3(1,-6) };
        for (int i = 0; i < names.Length; i++)
        {
            if (All<Transform>(main).Any(t => t.name == names[i])) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Modules/Economy/" + names[i] + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing shop " + names[i]);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, main);
            instance.name = names[i];
            instance.transform.position = positions[i];
        }
        Set(All<CowShop>(main).Single(), "individualCowPrefab",
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Game/Prefabs/Features/Cows/IndividualCow.prefab"));
        var cowShop = All<CowShop>(main).Single();
        Sprite cowShopSprite = AssetDatabase.LoadAllAssetsAtPath("Assets/_Game/Art/Features/Economy/shop-hoang/cowShop.png")
            .OfType<Sprite>().FirstOrDefault();
        if (cowShopSprite == null) throw new InvalidOperationException("Missing cowShop sprite.");
        var cowShopRenderer = cowShop.GetComponent<SpriteRenderer>();
        cowShopRenderer.sprite = cowShopSprite;
        cowShopRenderer.color = Color.white;
        float shopScale = 2.1f / cowShopSprite.bounds.size.x;
        cowShop.transform.localScale = new Vector3(shopScale, shopScale, 1);
        var cowShopCollider = cowShop.GetComponent<BoxCollider2D>();
        cowShopCollider.size = new Vector2(1.2f / shopScale, .65f / shopScale);
        cowShopCollider.offset = new Vector2(0, -.35f / shopScale);
        PrefabUtility.RecordPrefabInstancePropertyModifications(cowShopRenderer);
        PrefabUtility.RecordPrefabInstancePropertyModifications(cowShop.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(cowShopCollider);
        foreach (var cow in All<CowController>(main))
        {
            var health = cow.GetComponent<CowHealth>();
            if (health == null) health = cow.gameObject.AddComponent<CowHealth>();
            Set(cow, "cowHealth", health);
            var interaction = cow.GetComponent<CowInteraction>();
            if (interaction != null) Set(interaction, "cowHealth", health);
        }
        var slots = All<DefenseBuildingSlot>(main);
        foreach (var slot in slots)
        {
            var references = slot.GetComponent<FeatureReferences>();
            if (references == null) references = slot.gameObject.AddComponent<FeatureReferences>();
            Set(slot, "references", references);
            Set(slot, "defenseContext", context);
            Set(slot, "progress", context.Progress);
            GameObject occupant = slot.SlotKind == DefenseBuildingSlot.Kind.Cow
                ? All<CowUpgrade>(main).Where(c => Vector2.Distance(c.transform.position, slot.transform.position) < .1f).Select(c => c.gameObject).FirstOrDefault()
                : slot.SlotKind == DefenseBuildingSlot.Kind.Machine
                    ? All<ProductionMachine>(main).Where(m => Vector2.Distance(m.transform.position, slot.transform.position) < .1f).Select(m => m.gameObject).FirstOrDefault()
                    : null;
            if (occupant != null) Set(slot, "initialOccupant", occupant);
        }
        PolishCowFixedBuildLayout.ApplyToScene(main);
        SetArray(context, "slots", slots);
        var initializer = inventory.GetComponent<FeatureReferenceInitializer>();
        Set(initializer, "inventory", inventory);
        SetArray(initializer, "features", All<FeatureReferences>(main));
        var installer = All<DefenseModuleInstaller>(main).Single();
        Set(installer, "context", context);
        SetArray(installer, "featureRoots", All<DefenseModule>(main).Select(m => m.transform).ToArray());
        foreach (var sample in All<InventoryInteractionSample>(main)) sample.gameObject.SetActive(false);
        SetArray(context, "routes", All<EnemyRoute>(main));
        var waves = Enumerable.Range(1, 10).Select(i => AssetDatabase.LoadAssetAtPath<WaveDefinition>(
            "Assets/_Game/Data/Defense/Wave" + i.ToString("00") + ".asset")).ToArray();
        if (waves.Any(w => w == null || w.groups.Any(g => g.enemy == null || g.enemy.prefab == null)))
            throw new InvalidOperationException("Missing wave/enemy prefab configuration.");
        SetArray(context, "waves", waves);
        SetArray(context.Timer, "waves", waves);
        var timerData = new SerializedObject(context.Timer);
        timerData.FindProperty("startAutomatically").boolValue = true;
        timerData.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(context.Timer);
        AddShopSign(cowShop.transform, "MUA BO", cowShopSprite.bounds.extents.y + .25f);
        AddShopSign(All<MedicineShop>(main).Single().transform, "THUOC", 1.5f);
        AddShopSign(All<MilkShop>(main).Single().transform, "BAN SUA", 1.5f);
        // Initial capital covers a cow, a first tower and feed before the first wave.
        var inventoryData = new SerializedObject(inventory);
        inventoryData.FindProperty("credits").intValue = 300;
        inventoryData.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(main);
        if (!EditorSceneManager.SaveScene(main)) throw new InvalidOperationException("Could not save Main.");
        Debug.Log("Main ready: CowShop art, shared economy, building, towers, enemies and ten automatic waves.");
    }

    private static void AddShopSign(Transform parent, string title, float height)
    {
        var sign = parent.Find("ShopSign");
        if (sign == null)
        {
            var go = new GameObject("ShopSign", typeof(TextMeshPro));
            go.transform.SetParent(parent, false);
            sign = go.transform;
        }
        var text = sign.GetComponent<TextMeshPro>();
        text.font = TMP_Settings.defaultFontAsset;
        text.fontSharedMaterial = text.font.material;
        text.text = title + "  [E]";
        text.fontSize = 2.2f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(1, .93f, .65f);
        text.rectTransform.sizeDelta = new Vector2(4, .65f);
        sign.localPosition = new Vector3(0, height, -1);
        text.GetComponent<MeshRenderer>().sortingOrder = 2000;
    }

    private static void ConfigureBuildingPanel(BuildingPanel panel)
    {
        var data = new SerializedObject(panel);
        var popup = (GameObject)data.FindProperty("panelRoot").objectReferenceValue;
        popup.GetComponent<RectTransform>().sizeDelta = new Vector2(540, 460);
        var font = ((TMP_Text)data.FindProperty("titleText").objectReferenceValue).font;
        var sell = PanelButton(popup.transform, font, "Sell", "Bán công trình", new Vector2(0, -195), new Vector2(240, 42));
        var previous = PanelButton(popup.transform, font, "Previous", "<", new Vector2(-220, 175), new Vector2(50, 40));
        var next = PanelButton(popup.transform, font, "Next", ">", new Vector2(220, 175), new Vector2(50, 40));
        Set(panel, "sellButton", sell);
        Set(panel, "sellText", sell.GetComponentInChildren<TMP_Text>());
        while (previous.onClick.GetPersistentEventCount() > 0) UnityEditor.Events.UnityEventTools.RemovePersistentListener(previous.onClick, 0);
        while (next.onClick.GetPersistentEventCount() > 0) UnityEditor.Events.UnityEventTools.RemovePersistentListener(next.onClick, 0);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(previous.onClick, panel.PreviousCatalogItem);
        UnityEditor.Events.UnityEventTools.AddPersistentListener(next.onClick, panel.NextCatalogItem);
        popup.SetActive(false);
        PrefabUtility.RecordPrefabInstancePropertyModifications(popup.GetComponent<RectTransform>());
    }

    private static void ConfigureBattlePanels(BattleUI battle)
    {
        var data = new SerializedObject(battle);
        foreach (string field in new[] { "gameOverPanel", "pausePanel" })
        {
            var panel = (GameObject)data.FindProperty(field).objectReferenceValue;
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(.025f, .06f, .045f, .95f);
        }
        StyleText((TMP_Text)data.FindProperty("txtGameOverTitle").objectReferenceValue, new Vector2(0, 60), new Vector2(800, 170), 38);
        StyleButton((Button)data.FindProperty("btnRestart").objectReferenceValue, new Vector2(0, -95), new Vector2(240, 56));
        StyleButton((Button)data.FindProperty("btnResume").objectReferenceValue, new Vector2(0, -65), new Vector2(240, 56));
        var pause = (GameObject)data.FindProperty("pausePanel").objectReferenceValue;
        foreach (TMP_Text text in pause.GetComponentsInChildren<TMP_Text>(true))
            if (text.GetComponentInParent<Button>() == null)
                StyleText(text, new Vector2(0, 60), new Vector2(700, 110), 36);
        var banner = (GameObject)data.FindProperty("levelUpBanner").objectReferenceValue;
        Layout(banner.GetComponent<RectTransform>(), new Vector2(.5f, 1), new Vector2(0, -20), new Vector2(720, 100));
        banner.GetComponent<Image>().color = new Color(.04f, .12f, .08f, .98f);
        var bannerText = (TMP_Text)data.FindProperty("txtLevelUp").objectReferenceValue;
        StyleText(bannerText, new Vector2(0, -50), new Vector2(680, 88), 25);
        bannerText.rectTransform.anchorMin = bannerText.rectTransform.anchorMax = new Vector2(.5f, 1);
    }

    private static void StyleText(TMP_Text text, Vector2 position, Vector2 size, float fontSize)
    {
        Layout(text.rectTransform, new Vector2(.5f, .5f), position, size);
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * .75f;
        text.fontSizeMax = fontSize;
        text.color = new Color(.94f, .96f, .9f);
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
    }

    private static void StyleButton(Button button, Vector2 position, Vector2 size)
    {
        Layout(button.GetComponent<RectTransform>(), new Vector2(.5f, .5f), position, size);
        button.GetComponent<Image>().color = new Color(.16f, .45f, .32f);
        var label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null) StyleText(label, Vector2.zero, size - new Vector2(16, 8), 24);
    }

    private static Button PanelButton(Transform parent, TMP_FontAsset font, string name, string label, Vector2 position, Vector2 size)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<Button>();
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Layout(go.GetComponent<RectTransform>(), new Vector2(.5f, .5f), position, size);
        go.GetComponent<Image>().color = new Color(.2f, .4f, .3f);
        var textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(go.transform, false);
        var text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = label;
        text.fontSize = 22;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;
        return go.GetComponent<Button>();
    }

    private static T[] All<T>(Scene scene) where T : Component =>
        scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<T>(true)).ToArray();

    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(target);
        var property = data.FindProperty(field) ?? throw new InvalidOperationException("Missing field " + field);
        property.objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void SetArray(UnityEngine.Object target, string field, UnityEngine.Object[] values)
    {
        var data = new SerializedObject(target);
        var array = data.FindProperty(field);
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        data.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }

    private static void Layout(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
