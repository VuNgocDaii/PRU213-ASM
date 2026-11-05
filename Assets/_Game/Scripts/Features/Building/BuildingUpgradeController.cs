using System;
using UnityEngine;

[DisallowMultipleComponent]
public class BuildingUpgradeController : MonoBehaviour
{
    [SerializeField] private BuildingController building;
    [SerializeField] private int[] upgradePrices = new int[0];

    public int MaxLevel => upgradePrices == null ? 1 : upgradePrices.Length + 1;
    public int NextPrice => building != null && building.CurrentLevel >= 1 && building.CurrentLevel < MaxLevel
        ? upgradePrices[building.CurrentLevel - 1] : 0;
    public bool IsMaxLevel => building != null && building.IsOccupied && building.CurrentLevel >= MaxLevel;

    public bool CanUpgrade(out string reason)
    {
        reason = null;
        if (building == null || building.Inventory == null) reason = "Chưa Initialize kho";
        else if (building.IsBusy) reason = "Đang xử lý";
        else if (!building.IsOccupied) reason = "Chưa có công trình";
        else if (MaxLevel > 3 || building.CurrentLevel < 1) reason = "Bảng cấp không hợp lệ (chỉ hỗ trợ 1–3)";
        else if (IsMaxLevel) reason = "Cấp tối đa";
        else if (NextPrice <= 0) reason = "Giá nâng cấp phải lớn hơn 0";
        else if (!BuildingController.ValidRoot(building.Instance) ||
            building.Instance.GetComponent<FeatureReferences>().Inventory != building.Inventory)
            reason = "Công trình thiếu hợp đồng nâng cấp hoặc sai kho";
        else if (building.Inventory.GetAmount(ItemType.Credits) < NextPrice) reason = "Không đủ Credits";
        return reason == null;
    }

    public bool TryUpgrade()
    {
        if (!Application.isPlaying) return false;
        if (!CanUpgrade(out string reason))
        {
            if (building != null && !building.IsBusy) building.ReportError(reason);
            return false;
        }
        if (!building.BeginTransaction()) return false;
        InventoryManager inventory = building.Inventory;
        int price = NextPrice;
        int previousLevel = building.CurrentLevel;
        UpgradeTarget target = building.Instance.GetComponent<UpgradeTarget>();
        bool paid = false;
        try
        {
            paid = inventory.TryRemove(ItemType.Credits, price);
            if (!paid) return false;
            target.ApplyLevel(previousLevel + 1);
            building.SetLevel(previousLevel + 1);
            return true;
        }
        catch (Exception exception)
        {
            // A throwing target may have changed its state before throwing. Restore the absolute level.
            if (paid)
            {
                try { target.ApplyLevel(previousLevel); }
                catch (Exception restoreError) { Debug.LogException(restoreError, this); }
                inventory.Add(ItemType.Credits, price);
            }
            building.ReportError("Nâng cấp thất bại, đã hoàn Credits: " + exception.Message);
            return false;
        }
        finally { building.EndTransaction(); }
    }
}
