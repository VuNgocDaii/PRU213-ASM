using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Shared panel for building construction, upgrades, and selling.
[DisallowMultipleComponent]
public class BuildingPanel : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text detailsText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text actionText;
    [SerializeField] private Button actionButton;
    [SerializeField] private Button closeButton;

    [Header("Sell & Catalog Enhancements")]
    [SerializeField] private Button sellButton;
    [SerializeField] private TMP_Text sellText;
    [SerializeField] private BuildingCatalogData catalog;

    private BuildingSlot selectedSlot;
    private InventoryManager subscribedInventory;
    private BuildingController subscribedBuilding;
    private int displayedLevel;
    private bool displayedOccupied;

    private List<BuildingCatalogData.Entry> availableEntries = new List<BuildingCatalogData.Entry>();
    private int currentCatalogIndex = 0;

    public BuildingSlot SelectedSlot => selectedSlot;
    public BuildingCatalogData Catalog { get => catalog; set => catalog = value; }

    private void Awake()
    {
        if (actionButton != null) actionButton.onClick.AddListener(Confirm);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (sellButton != null) sellButton.onClick.AddListener(Sell);
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    public void Open(BuildingSlot slot)
    {
        if (slot == null || slot.Building == null || slot.Building.IsBusy || !slot.IsPlayerInRange) return;
        Close();
        selectedSlot = slot;
        subscribedBuilding = slot.Building;
        subscribedInventory = subscribedBuilding.Inventory;
        subscribedBuilding.Changed += Refresh;
        if (subscribedInventory != null) subscribedInventory.OnChanged += Refresh;

        SetupCatalogForSlot();

        if (panelRoot != null) panelRoot.SetActive(true);
        Refresh();
    }

    private void SetupCatalogForSlot()
    {
        availableEntries.Clear();
        currentCatalogIndex = 0;
        if (catalog == null || selectedSlot == null) return;

        DefenseBuildingSlot.Kind kind = selectedSlot.DefenseSlot != null
            ? selectedSlot.DefenseSlot.SlotKind
            : DefenseBuildingSlot.Kind.Cow;

        int castleLevel = selectedSlot.Building.CastleProgress != null
            ? selectedSlot.Building.CastleProgress.CurrentLevel
            : 3;

        availableEntries = catalog.GetEntriesForKind(kind, castleLevel);
        if (availableEntries.Count > 0 && !selectedSlot.Building.IsOccupied)
        {
            ApplyCatalogEntry(availableEntries[0]);
        }
    }

    public void NextCatalogItem()
    {
        if (availableEntries.Count <= 1 || (selectedSlot != null && selectedSlot.Building.IsOccupied)) return;
        currentCatalogIndex = (currentCatalogIndex + 1) % availableEntries.Count;
        ApplyCatalogEntry(availableEntries[currentCatalogIndex]);
        Refresh();
    }

    public void PreviousCatalogItem()
    {
        if (availableEntries.Count <= 1 || (selectedSlot != null && selectedSlot.Building.IsOccupied)) return;
        currentCatalogIndex = (currentCatalogIndex - 1 + availableEntries.Count) % availableEntries.Count;
        ApplyCatalogEntry(availableEntries[currentCatalogIndex]);
        Refresh();
    }

    private void ApplyCatalogEntry(BuildingCatalogData.Entry entry)
    {
        if (entry == null || selectedSlot == null) return;
        selectedSlot.Building.SetBuildConfig(
            entry.prefab,
            entry.buildPrice,
            entry.displayName,
            entry.footprintSize,
            entry.unlockCastleLevel,
            entry.refundRatio
        );
        if (selectedSlot.Upgrades != null && entry.upgradePrices != null)
        {
            selectedSlot.Upgrades.SetUpgradePrices(entry.upgradePrices);
        }
    }

    public void Close()
    {
        if (subscribedBuilding != null) subscribedBuilding.Changed -= Refresh;
        if (subscribedInventory != null) subscribedInventory.OnChanged -= Refresh;
        subscribedBuilding = null;
        subscribedInventory = null;
        selectedSlot = null;
        availableEntries.Clear();
        currentCatalogIndex = 0;
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void Update()
    {
        if (selectedSlot == null) return;
        if (!selectedSlot.IsPlayerInRange || !selectedSlot.isActiveAndEnabled ||
            (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)) Close();
    }

    public void Confirm()
    {
        if (selectedSlot == null) return;
        if (!selectedSlot.IsPlayerInRange) { Close(); return; }
        BuildingController building = selectedSlot.Building;
        if (building.IsBusy || displayedOccupied != building.IsOccupied || displayedLevel != building.CurrentLevel) return;

        if (building.IsOccupied) selectedSlot.Upgrades?.TryUpgrade();
        else building.TryBuild();
        Refresh();
    }

    public void Sell()
    {
        if (selectedSlot == null || !selectedSlot.IsPlayerInRange) { Close(); return; }
        BuildingController building = selectedSlot.Building;
        if (building == null || building.IsBusy || !building.IsOccupied) return;

        building.TrySell(out int refunded);
        Refresh();
    }

    private void Refresh()
    {
        if (selectedSlot == null) return;
        BuildingController building = selectedSlot.Building;
        BuildingUpgradeController upgrades = selectedSlot.Upgrades;
        displayedLevel = building.CurrentLevel;
        displayedOccupied = building.IsOccupied;
        string reason = null;
        bool canAct = false;

        if (titleText != null) titleText.text = building.BuildingName;

        if (!building.IsOccupied)
        {
            canAct = building.CanBuild(out reason);
            if (detailsText != null)
            {
                string catalogNavHint = availableEntries.Count > 1
                    ? $"\n[Mục {currentCatalogIndex + 1}/{availableEntries.Count}]"
                    : "";
                detailsText.text = $"Vị trí trống\nGiá xây: {building.BuildPrice} Credits{catalogNavHint}";
            }
            if (actionText != null) actionText.text = "Xây";
            if (sellButton != null) sellButton.gameObject.SetActive(false);
        }
        else if (upgrades != null)
        {
            canAct = upgrades.CanUpgrade(out reason);
            int refund = building.CalculateRefund();
            if (detailsText != null)
            {
                detailsText.text = $"Cấp {building.CurrentLevel} / {upgrades.MaxLevel}" +
                    (upgrades.IsMaxLevel ? "\nCấp tối đa" : $"\nNâng lên cấp {building.CurrentLevel + 1}: {upgrades.NextPrice} Credits") +
                    $"\nGiá trị hoàn tiền khi bán: {refund} Credits";
            }
            if (actionText != null) actionText.text = upgrades.IsMaxLevel ? "Cấp tối đa" : "Nâng cấp";

            if (sellButton != null)
            {
                sellButton.gameObject.SetActive(true);
                sellButton.interactable = !building.IsBusy;
                if (sellText != null) sellText.text = $"Bán (+{refund})";
            }
        }
        else
        {
            reason = "Chưa gán cấu hình nâng cấp";
            if (sellButton != null) sellButton.gameObject.SetActive(false);
        }

        if (actionButton != null) actionButton.interactable = canAct;
        if (statusText != null)
        {
            int credits = building.Inventory != null ? building.Inventory.GetAmount(ItemType.Credits) : 0;
            statusText.text = "Bạn có: " + credits + " Credits\n" + (building.LastError ?? reason ?? "Chọn nút để xác nhận");
        }
    }

    private void OnDisable() { Close(); }
    private void OnDestroy()
    {
        Close();
        if (actionButton != null) actionButton.onClick.RemoveListener(Confirm);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (sellButton != null) sellButton.onClick.RemoveListener(Sell);
    }
}
