using System.Collections.Generic;
using UnityEngine;

// Person 2 owns placement UI/catalog. All construction payments go through DefenseBuildingSlot.TryBuild.
public class BuildingFeature : DefenseModule
{
    [Header("Feature Assets & Configurations")]
    [SerializeField] private BuildingCatalogData catalog;
    [SerializeField] private BuildingPanel panel;
    [SerializeField] private CastleVisualController castleVisual;
    [SerializeField] private GameObject castleVisualPrefab;
    [SerializeField] private TowerUpgradeBridge towerUpgradeBridge;

    private readonly List<BuildingSlot> activeAdapters = new List<BuildingSlot>();

    public BuildingCatalogData Catalog => catalog;
    public BuildingPanel Panel => panel;
    public CastleVisualController CastleVisual => castleVisual;
    public TowerUpgradeBridge TowerUpgradeBridge => towerUpgradeBridge;
    public IReadOnlyList<BuildingSlot> ActiveAdapters => activeAdapters;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        if (Context == null) return;

        // Ensure TowerUpgradeBridge is initialized
        if (towerUpgradeBridge == null)
        {
            towerUpgradeBridge = GetComponentInChildren<TowerUpgradeBridge>();
            if (towerUpgradeBridge == null)
            {
                towerUpgradeBridge = gameObject.AddComponent<TowerUpgradeBridge>();
            }
        }
        towerUpgradeBridge.Initialize(Context.Progress, Context.Inventory);

        // Connect Castle Visual if present
        if (castleVisual == null)
        {
            castleVisual = GetComponentInChildren<CastleVisualController>();
            if (castleVisual == null)
            {
                castleVisual = FindAnyObjectByType<CastleVisualController>();
            }
        }
        if (castleVisual == null && castleVisualPrefab != null && Context != null && Context.CastleTarget != null)
        {
            GameObject visualObj = Instantiate(castleVisualPrefab, Context.CastleTarget.position, Context.CastleTarget.rotation, Context.CastleTarget);
            castleVisual = visualObj.GetComponent<CastleVisualController>();
        }
        if (castleVisual != null)
        {
            castleVisual.Initialize(Context.Progress, Context.CastleHealth);
        }

        // Connect panel
        if (panel == null)
        {
            panel = GetComponentInChildren<BuildingPanel>(true);
        }
        if (panel != null && catalog != null)
        {
            panel.Catalog = catalog;
        }

        // Bridge all DefenseBuildingSlots from Context
        AttachSlotAdapters();
    }

    protected override void OnConnected()
    {
        base.OnConnected();
        if (Context != null && Context.Progress != null)
        {
            Context.Progress.OnCastleLevelUp += OnCastleLevelUp;
        }
    }

    protected override void OnDisconnected()
    {
        base.OnDisconnected();
        if (Context != null && Context.Progress != null)
        {
            Context.Progress.OnCastleLevelUp -= OnCastleLevelUp;
        }
    }

    private void OnCastleLevelUp(int newLevel)
    {
        if (castleVisual != null)
        {
            castleVisual.ApplyCastleLevel(newLevel);
        }
    }

    private void AttachSlotAdapters()
    {
        activeAdapters.Clear();
        if (Context == null || Context.Slots == null) return;

        Transform playerTransform = null;
        var player = FindAnyObjectByType<PlayerInteraction>();
        if (player != null) playerTransform = player.transform;

        foreach (DefenseBuildingSlot defSlot in Context.Slots)
        {
            if (defSlot == null) continue;

            BuildingSlot adapter = defSlot.GetComponent<BuildingSlot>();
            BuildingController controller = defSlot.GetComponent<BuildingController>();
            BuildingUpgradeController upgrader = defSlot.GetComponent<BuildingUpgradeController>();

            if (controller == null)
            {
                controller = defSlot.gameObject.AddComponent<BuildingController>();
            }

            // Wire FeatureReferences so Inventory is accessible
            FeatureReferences slotRefs = defSlot.GetComponent<FeatureReferences>();
            if (slotRefs != null)
            {
                controller.SetFeatureReferences(slotRefs);
            }

            // Provide default catalog entry configuration according to slot kind
            BuildingCatalogData.Entry defaultEntry = GetDefaultEntryForKind(defSlot.SlotKind);
            if (defaultEntry != null)
            {
                controller.SetBuildConfig(
                    defaultEntry.prefab,
                    defaultEntry.buildPrice,
                    defaultEntry.displayName,
                    defaultEntry.footprintSize,
                    defaultEntry.unlockCastleLevel,
                    defaultEntry.refundRatio
                );
            }

            // Wire DefenseBuildingSlot AFTER SetBuildConfig so buildPrice is correct
            // when BindDefenseSlot picks up existing occupants
            controller.BindDefenseSlot(defSlot, Context.Progress);

            if (upgrader == null)
            {
                upgrader = defSlot.gameObject.AddComponent<BuildingUpgradeController>();
            }

            int[] upgradePrices = defaultEntry?.upgradePrices ?? GetDefaultUpgradePrices(defSlot.SlotKind);
            bool isTower = defSlot.SlotKind == DefenseBuildingSlot.Kind.Tower;
            upgrader.Configure(controller, upgradePrices, Context.Progress, isTower);

            if (adapter == null)
            {
                adapter = defSlot.gameObject.AddComponent<BuildingSlot>();
            }

            // Ensure collider exists for player interaction [E]
            var col = defSlot.GetComponent<Collider2D>();
            if (col == null)
            {
                var circle = defSlot.gameObject.AddComponent<CircleCollider2D>();
                circle.isTrigger = true;
                circle.radius = 1.2f;
            }

            adapter.BindDefenseSlot(defSlot, Context, panel, playerTransform);
            activeAdapters.Add(adapter);
        }
    }

    private BuildingCatalogData.Entry GetDefaultEntryForKind(DefenseBuildingSlot.Kind kind)
    {
        if (catalog == null) return null;
        var entries = catalog.GetEntriesForKind(kind, 3);
        return entries.Count > 0 ? entries[0] : null;
    }

    private int[] GetDefaultUpgradePrices(DefenseBuildingSlot.Kind kind)
    {
        switch (kind)
        {
            case DefenseBuildingSlot.Kind.Tower:
                return new[] { 60, 120 }; // Arrow tower default
            case DefenseBuildingSlot.Kind.Cow:
                return new[] { 120, 280 }; // Incremental step prices from Excel
            case DefenseBuildingSlot.Kind.Machine:
                return new[] { 150, 250 }; // Incremental step prices from Excel
            default:
                return new int[0];
        }
    }

    public bool TryBuildOnSlot(DefenseBuildingSlot slot, string catalogId)
    {
        if (slot == null || catalog == null) return false;
        BuildingCatalogData.Entry entry = catalog.FindById(catalogId);
        if (entry == null) return false;

        BuildingController controller = slot.GetComponent<BuildingController>();
        if (controller == null) return false;

        controller.SetBuildConfig(
            entry.prefab,
            entry.buildPrice,
            entry.displayName,
            entry.footprintSize,
            entry.unlockCastleLevel,
            entry.refundRatio
        );

        return controller.TryBuild();
    }

    public bool TryUpgradeOnSlot(DefenseBuildingSlot slot)
    {
        if (slot == null) return false;
        BuildingUpgradeController upgrader = slot.GetComponent<BuildingUpgradeController>();
        if (upgrader == null) return false;
        return upgrader.TryUpgrade();
    }

    public bool TrySellOnSlot(DefenseBuildingSlot slot, out int refundAmount)
    {
        refundAmount = 0;
        if (slot == null) return false;
        BuildingController controller = slot.GetComponent<BuildingController>();
        if (controller == null) return false;
        return controller.TrySell(out refundAmount);
    }
}
