using System;
using UnityEngine;
using UnityEngine.Events;

// Bridge for Towers UI: provides upgrade operations without requiring Towers to implement payment logic.
[DisallowMultipleComponent]
public class TowerUpgradeBridge : MonoBehaviour
{
    [SerializeField] private CastleProgress castleProgress;
    [SerializeField] private InventoryManager inventory;

    [Header("Events")]
    [SerializeField] private UnityEvent<GameObject, int> onTowerUpgraded;
    [SerializeField] private UnityEvent<string> onTowerUpgradeFailed;

    public void Initialize(CastleProgress progress, InventoryManager inv)
    {
        castleProgress = progress;
        inventory = inv;
    }

    public bool CanUpgrade(DefenseBuildingSlot slot, out string reason)
    {
        reason = null;
        if (slot == null) { reason = "Slot null"; return false; }
        if (slot.Occupant == null) { reason = "Không có tháp"; return false; }
        if (inventory == null) { reason = "Kho chưa khởi tạo"; return false; }

        BuildingUpgradeController upgradeController = slot.GetComponent<BuildingUpgradeController>();
        if (upgradeController != null)
        {
            return upgradeController.CanUpgrade(out reason);
        }

        UpgradeTarget target = slot.Occupant.GetComponent<UpgradeTarget>();
        if (target == null) { reason = "Thiếu UpgradeTarget"; return false; }

        // If no controller is attached, fallback to querying the slot's BuildingController
        BuildingController controller = slot.GetComponent<BuildingController>();
        if (controller != null && controller.IsBusy) { reason = "Đang xử lý"; return false; }

        return true;
    }

    public int GetNextUpgradePrice(DefenseBuildingSlot slot)
    {
        if (slot == null || slot.Occupant == null) return 0;
        BuildingUpgradeController upgradeController = slot.GetComponent<BuildingUpgradeController>();
        if (upgradeController != null) return upgradeController.NextPrice;
        return 0;
    }

    public bool TryUpgrade(DefenseBuildingSlot slot)
    {
        if (slot == null || slot.Occupant == null)
        {
            onTowerUpgradeFailed?.Invoke("Không có tháp trên ô");
            return false;
        }

        BuildingUpgradeController upgradeController = slot.GetComponent<BuildingUpgradeController>();
        if (upgradeController != null)
        {
            bool success = upgradeController.TryUpgrade();
            if (success)
            {
                onTowerUpgraded?.Invoke(slot.Occupant, upgradeController.Building.CurrentLevel);
            }
            return success;
        }

        onTowerUpgradeFailed?.Invoke("Ô tháp chưa gắn BuildingUpgradeController");
        return false;
    }
}
