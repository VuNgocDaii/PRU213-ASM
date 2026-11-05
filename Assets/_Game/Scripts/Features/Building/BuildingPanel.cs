using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// One shared panel for all three slots, under the feature's own Canvas/PopupLayer.
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

    private BuildingSlot selectedSlot;
    private InventoryManager subscribedInventory;
    private BuildingController subscribedBuilding;
    private int displayedLevel;
    private bool displayedOccupied;
    public BuildingSlot SelectedSlot => selectedSlot;

    private void Awake()
    {
        if (actionButton != null) actionButton.onClick.AddListener(Confirm);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
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
        if (panelRoot != null) panelRoot.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (subscribedBuilding != null) subscribedBuilding.Changed -= Refresh;
        if (subscribedInventory != null) subscribedInventory.OnChanged -= Refresh;
        subscribedBuilding = null;
        subscribedInventory = null;
        selectedSlot = null;
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
        // Reject stale UI intents if another action changed the structure in the meantime.
        if (building.IsBusy || displayedOccupied != building.IsOccupied || displayedLevel != building.CurrentLevel) return;
        if (building.IsOccupied) selectedSlot.Upgrades?.TryUpgrade();
        else building.TryBuild();
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
            if (detailsText != null) detailsText.text = "Vị trí trống\nGiá xây: " + building.BuildPrice + " Credits";
            if (actionText != null) actionText.text = "Xây";
        }
        else if (upgrades != null)
        {
            canAct = upgrades.CanUpgrade(out reason);
            if (detailsText != null) detailsText.text = "Cấp " + building.CurrentLevel + " / " + upgrades.MaxLevel +
                (upgrades.IsMaxLevel ? "\nCấp tối đa" : "\nNâng lên cấp " + (building.CurrentLevel + 1) + ": " + upgrades.NextPrice + " Credits");
            if (actionText != null) actionText.text = upgrades.IsMaxLevel ? "Cấp tối đa" : "Nâng cấp";
        }
        else reason = "Chưa gán cấu hình nâng cấp";
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
    }
}
